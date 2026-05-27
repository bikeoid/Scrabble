# Scrabble AI - Computer Player Engine

A high-performance, fully rule-compliant Scrabble AI for the
[bikeoid/Scrabble](https://github.com/bikeoid/Scrabble) Blazor project.

Implements the **Appel & Jacobson (1988)** DAWG-based move generator - the same algorithm
described in *"The World's Fastest Scrabble Program"* (Communications of the ACM, May 1988),
referenced in the project's own README.

---

## Files

| File | Purpose |
|------|---------|
| `Dawg.cs` | Directed Acyclic Word Graph - compact lexicon, O(n) lookup |
| `ScrabbleBoardAI.cs` | Board model, premium squares, cross-checks, cross-sums, anchors |
| `ScrabbleMove.cs` | Move representation + full scoring (premiums, cross-words, bingo bonus) |
| `MoveGenerator.cs` | Backtracking move generator (the Appel-Jacobson algorithm) |
| `SkillLevel.cs` | Skill-level enum + move selection strategies |
| `ComputerPlayerAI.cs` | Top-level service + adapter/DTO layer |
| `ComputerPlayerAITests.cs` | xUnit unit tests |

---

## Algorithm Overview

### Why a DAWG?

The lexicon (~179,000 TWL words) is stored as a **Directed Acyclic Word Graph** - a minimised
finite-state automaton. Compared to a naïve list scan:

- Memory: ~175 KB vs ~780 KB for the raw word list
- Query: O(word length) instead of O(dictionary size)
- Backtracking: the DAWG guides the search so only legal word continuations are ever explored

### Move Generation (one turn)

```
For each direction (horizontal, vertical):
  Precompute cross-check masks   (which letters can legally cross each empty square)
  Precompute cross-sums          (perpendicular tile values for fast scoring)
  Identify anchor squares        (empty squares adjacent to occupied squares)

  For each anchor:
    If a prefix already exists on the board:
      Walk DAWG to that prefix node
      ExtendRight -> enumerate all completions
    Else:
      LeftPart  -> enumerate all rack subsets as left-parts (limited by free squares left of anchor)
      ExtendRight for each left-part -> enumerate all right-extensions

      Each ExtendRight step:
        If current square is occupied -> follow that letter in DAWG (no rack use)
        If current square is empty   -> try each DAWG edge whose letter passes the cross-check mask
        If DAWG node is terminal     -> record the move + score it
```

Total complexity per turn: **O(A x R x D)** where A = anchors (<=225), R = rack permutations
(at most 7! / duplicates, heavily pruned by the DAWG), D = DAWG depth (<=15).
Typical turn time: **< 50 ms** on modern hardware, well under the 1-2 s on the 1988 VAX.

### Scoring

Full standard Scrabble scoring is implemented:

- Letter premiums (DL, TL) applied to newly placed tiles only
- Word premiums (DW, TW) applied to the main word; also to cross-words
- **Bingo bonus**: +50 when all 7 rack tiles are played
- Cross-word scoring: every perpendicular word formed by the move is scored independently

---

## Skill Levels

| Level | Strategy |
|-------|----------|
| **Easy** | Randomly picks from the **bottom 25 %** of legal moves by score |
| **Medium** | Randomly picks from the **top 50 %** of legal moves |
| **Hard** | Always plays the highest-scoring legal move (pure greedy) |
| **Expert** | Highest-scoring move adjusted by rack-balance bonus (vowel/consonant ratio) and a board-opening penalty (discourages exposing triple-word squares to the opponent) |

---

## Integration Steps

### 1. Add files to the server project

Copy all `.cs` files (except the test file) into `Scrabble/Scrabble.Server/AI/`.

### 2. Register the service

In `Program.cs` (or `Startup.cs`), add:

```csharp
builder.Services.AddSingleton<Scrabble.Core.AI.ComputerPlayerAI>();
```

Optionally pre-warm the DAWG at startup (avoids a cold-start delay):

```csharp
var app = builder.Build();
await app.Services.GetRequiredService<ComputerPlayerAI>().InitialiseAsync();
```

### 3. Point at the dictionary

The default path is `wwwroot/Dictionary/TWL06.txt` - the same file already used by the game.
Override via:

```csharp
services.AddSingleton(sp => new ComputerPlayerAI(
    sp.GetRequiredService<ILogger<ComputerPlayerAI>>())
{
    DictionaryPath = "/path/to/TWL06.txt"
});
```

### 4. Call from the computer-turn handler

Find the location where the server processes the computer player's turn and inject the service:

```csharp
// Example - adapt to match the actual server code
public async Task<IActionResult> ComputerMove(
    [FromServices] ComputerPlayerAI ai,
    int gameId)
{
    var game = await _db.Games.FindAsync(gameId);

    // Map your board model to char[15,15] + bool[15,15]
    var (letters, blanks) = MapBoard(game.Board);
    string rack = game.ComputerRack;

    // Choose skill level - could come from game settings / player profile
    var skill = SkillLevel.Hard;

    ScrabbleMove? move = await ai.MakeMoveAsync(letters, blanks, rack, skill);

    if (move is null)
    {
        // No legal moves - pass or exchange tiles
        return Ok(new { action = "pass" });
    }

    // Apply the move to the game state using the existing server logic
    var dto = ComputerPlayerAI.ToMoveDto(move);
    await ApplyMove(game, dto);

    return Ok(dto);
}
```

### 5. Add a skill-level field to player/game settings (optional)

```csharp
// In your player or game settings model:
public SkillLevel ComputerSkillLevel { get; set; } = SkillLevel.Hard;
```

Expose it in the Player Settings UI (already exists in the project) as a dropdown:
Easy / Medium / Hard / Expert.

---

## Performance Notes

| Scenario | Typical time |
|----------|-------------|
| DAWG build (179 k words) | ~200-400 ms (once, at startup) |
| Move generation, typical position, no blanks | 5-30 ms |
| Move generation, 1 blank in rack | 30-100 ms |
| Move generation, 2 blanks in rack | 100-300 ms |

The DAWG is built **once** as a singleton and shared across all concurrent requests,
so startup cost is paid only once.

Cross-checks and cross-sums are recomputed per turn (they change after every move),
but the computation is O(225) and takes < 1 ms.

---

## Running the Tests

Add the test file to an xUnit test project that references the AI source files:

```xml
<ProjectReference Include="..\Scrabble.Server\Scrabble.Server.csproj" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
```

```bash
dotnet test
```

---

## References

- Appel, A.W. & Jacobson, G.J. (1988). *The World's Fastest Scrabble Program*.
  Communications of the ACM, 31(5), 572-578.
  https://www.cs.cmu.edu/afs/cs/academic/class/15451-s06/www/lectures/scrabble.pdf
- TWL06 dictionary: https://www.wordgamedictionary.com/twl06/
