// MoveValidator.cs
// Validates a proposed move against the complete official Scrabble rule set.
//
// Use this to validate both human-submitted moves (from the UI) and
// computer-generated moves before they are committed to the game state.
//
// All rules checked
// ------------------
// R01  At least one tile must be placed.
// R02  All placed tiles must be within the 15×15 board boundary.
// R03  No tile may be placed on an already-occupied square.
// R04  Every tile letter must be a valid A-Z letter (or a blank whose face is A-Z).
// R05  All placements must lie on a single row OR a single column (not diagonal/scattered).
// R06  The placement must be contiguous: no gaps between newly placed tiles unless
//      an existing board tile fills every gap.
// R07  On the first move the word must cover the centre square (7,7).
// R08  On subsequent moves at least one new tile must be adjacent to an existing tile
//      (connectivity – the board must remain one connected group).
// R09  The main word formed (reading across or down through all new and bridging tiles)
//      must be a valid dictionary word.
// R10  Every perpendicular cross-word formed by the new tiles must also be a valid
//      dictionary word.
// R11  The player's rack must contain all tiles being placed (accounting for blanks).
//      (Only checked when a rack is supplied; omit rack for server-side board validation.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scrabble.Core.AI
{
    // -- Validation result -----------------------------------------------------

    /// <summary>
    /// The outcome of a validation attempt, including a human-readable reason
    /// for any failure and the list of words that were verified (useful for UI feedback).
    /// </summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }

        /// <summary>Short machine-readable code for the failing rule, e.g. "R09_INVALID_MAIN_WORD".</summary>
        public string ErrorCode { get; init; } = string.Empty;

        /// <summary>Human-readable explanation suitable for displaying in the UI.</summary>
        public string ErrorMessage { get; init; } = string.Empty;

        /// <summary>
        /// All dictionary words that were verified as part of this move
        /// (main word + any cross-words).  Populated even on success; empty on structural failures.
        /// </summary>
        public IReadOnlyList<string> WordsFormed { get; init; } = Array.Empty<string>();

        /// <summary>The computed score (0 if validation failed).</summary>
        public int Score { get; init; }

        // -- Factory helpers ----------------------------------------------------

        public static ValidationResult Ok(IReadOnlyList<string> words, int score) =>
            new() { IsValid = true, WordsFormed = words, Score = score };

        public static ValidationResult Fail(string code, string message,
            IReadOnlyList<string>? wordsFormed = null) =>
            new() { IsValid = false, ErrorCode = code, ErrorMessage = message,
                    WordsFormed = wordsFormed ?? Array.Empty<string>() };
    }

    // -- Validator -------------------------------------------------------------

    /// <summary>
    /// Validates a proposed placement against all Scrabble rules and the dictionary.
    /// Thread-safe; create once and reuse.
    /// </summary>
    public sealed class MoveValidator
    {
        private readonly Dawg _dawg;

        public MoveValidator(Dawg dawg) => _dawg = dawg;

        // -- Primary entry-point -----------------------------------------------

        /// <summary>
        /// Validate a list of tile placements on the given board.
        /// </summary>
        /// <param name="board">Current board state (before the move).</param>
        /// <param name="placements">Tiles the player wants to place.</param>
        /// <param name="rack">
        ///   Optional: the player's current rack.  When non-null, R11 (rack ownership)
        ///   is enforced.  Pass null to skip that check (e.g. server replay validation).
        /// </param>
        public ValidationResult Validate(
            AiBoard board,
            IReadOnlyList<TilePlacement> placements,
            Rack? rack = null)
        {
            // -- R01 At least one tile -----------------------------------------
            if (placements.Count == 0)
                return ValidationResult.Fail("R01_NO_TILES", "You must place at least one tile.");

            // -- R02 In bounds -------------------------------------------------
            foreach (var p in placements)
            {
                if (!board.InBounds(p.Row, p.Col))
                    return ValidationResult.Fail("R02_OUT_OF_BOUNDS",
                        $"Tile '{p.Letter}' at ({p.Row},{p.Col}) is outside the 15×15 board.");
            }

            // -- R03 No overlap with existing tiles ----------------------------
            foreach (var p in placements)
            {
                if (board[p.Row, p.Col].IsOccupied)
                    return ValidationResult.Fail("R03_SQUARE_OCCUPIED",
                        $"Square ({p.Row},{p.Col}) is already occupied by '{board[p.Row, p.Col].Letter}'.");
            }

            // -- R04 Valid letters ---------------------------------------------
            foreach (var p in placements)
            {
                if (p.Letter < 'A' || p.Letter > 'Z')
                    return ValidationResult.Fail("R04_INVALID_LETTER",
                        $"'{p.Letter}' is not a valid Scrabble letter.");
            }

            // -- R05 All on one row or one column ------------------------------
            bool allSameRow = placements.Select(p => p.Row).Distinct().Count() == 1;
            bool allSameCol = placements.Select(p => p.Col).Distinct().Count() == 1;

            if (!allSameRow && !allSameCol)
                return ValidationResult.Fail("R05_NOT_LINEAR",
                    "All tiles must be placed in the same row or the same column.");

            // Determine direction (horizontal wins when a single tile is placed)
            bool horizontal = allSameRow;

            // -- R06 Contiguous (no empty gap between new tiles) ---------------
            var contiguityError = CheckContiguity(board, placements, horizontal);
            if (contiguityError is not null)
                return contiguityError;

            // -- Temporarily place tiles so we can read formed words -----------
            ApplyPlacements(board, placements);
            try
            {
                // -- R07 First move covers centre ------------------------------
                bool boardWasEmpty = !placements.Any(p =>
                    // The board was empty before we placed — detect by checking whether
                    // any tile OTHER than those we just placed is now occupied.
                    HasExistingTile(board, placements));

                if (boardWasEmpty)
                {
                    if (!placements.Any(p => p.Row == AiBoard.Center && p.Col == AiBoard.Center))
                        return ValidationResult.Fail("R07_MUST_COVER_CENTRE",
                            "The first move must cover the centre square (row 7, col 7).");

                    if (placements.Count < 2)
                        return ValidationResult.Fail("R07_FIRST_MOVE_MIN_LENGTH",
                            "The first move must form a word of at least 2 letters.");
                }

                // -- R08 Connected to existing tiles ---------------------------
                if (!boardWasEmpty)
                {
                    bool connected = placements.Any(p => IsAdjacentToExisting(board, p, placements));
                    if (!connected)
                        return ValidationResult.Fail("R08_NOT_CONNECTED",
                            "At least one new tile must be adjacent to a tile already on the board.");
                }

                // -- Collect all words formed ----------------------------------
                var wordsFormed = new List<string>();

                // Main word
                string mainWord = ReadWord(board, placements[0].Row, placements[0].Col, horizontal);
                wordsFormed.Add(mainWord);

                // Cross-words (one per newly placed tile)
                foreach (var p in placements)
                {
                    string cross = ReadWord(board, p.Row, p.Col, !horizontal);
                    if (cross.Length >= 2)
                        wordsFormed.Add(cross);
                }

                // De-duplicate (edge case: single-tile move might produce same word both ways)
                wordsFormed = wordsFormed.Distinct().ToList();

                // -- R09 Main word is valid -------------------------------------
                if (mainWord.Length < 2)
                    return ValidationResult.Fail("R09_WORD_TOO_SHORT",
                        $"The main word '{mainWord}' is too short (minimum 2 letters).");

                if (!_dawg.Contains(mainWord))
                    return ValidationResult.Fail("R09_INVALID_MAIN_WORD",
                        $"'{mainWord}' is not a valid Scrabble word.",
                        wordsFormed);

                // -- R10 All cross-words are valid -----------------------------
                foreach (var p in placements)
                {
                    string cross = ReadWord(board, p.Row, p.Col, !horizontal);
                    if (cross.Length < 2) continue; // single letter → no cross-word formed

                    if (!_dawg.Contains(cross))
                        return ValidationResult.Fail("R10_INVALID_CROSS_WORD",
                            $"'{cross}' (formed crossing at row {p.Row}, col {p.Col}) is not a valid Scrabble word.",
                            wordsFormed);
                }

                // -- R11 Rack ownership (optional) -----------------------------
                if (rack is not null)
                {
                    var rackCopy = rack.Clone();
                    foreach (var p in placements)
                    {
                        bool taken = p.IsBlank
                            ? rackCopy.TakeBlank()
                            : rackCopy.TakeExact(p.Letter) || rackCopy.TakeBlank();

                        if (!taken)
                            return ValidationResult.Fail("R11_TILE_NOT_IN_RACK",
                                $"Your rack does not contain the tile '{(p.IsBlank ? "blank" : p.Letter.ToString())}'.");
                    }
                }

                // -- All checks passed -----------------------------------------
                int score = MoveScorer.Score(board, placements.ToList(), horizontal);
                return ValidationResult.Ok(wordsFormed, score);
            }
            finally
            {
                // Always undo the temporary placement
                UndoPlacements(board, placements);
            }
        }

        // -- Convenience overloads ---------------------------------------------

        /// <summary>
        /// Validate a <see cref="ScrabbleMove"/> produced by the AI engine.
        /// The move is already fully formed, so validation is essentially a
        /// double-check before committing to the game state.
        /// </summary>
        public ValidationResult Validate(AiBoard board, ScrabbleMove move, Rack? rack = null) =>
            Validate(board, move.Placements, rack);

        /// <summary>
        /// Quick check: is a single word playable in the dictionary?
        /// (Does not check board placement rules.)
        /// </summary>
        public bool IsValidWord(string word) =>
            word.Length >= 2 && _dawg.Contains(word.ToUpperInvariant());

        // -- Rule helpers ------------------------------------------------------

        /// <summary>R06: verify no gaps exist between the outermost new tiles.</summary>
        private static ValidationResult? CheckContiguity(
            AiBoard board,
            IReadOnlyList<TilePlacement> placements,
            bool horizontal)
        {
            if (placements.Count == 1) return null; // single tile is always contiguous

            int fixedDim = horizontal ? placements[0].Row : placements[0].Col;

            // Sort along the variable axis
            var sorted = horizontal
                ? placements.OrderBy(p => p.Col).ToList()
                : placements.OrderBy(p => p.Row).ToList();

            int start = horizontal ? sorted[0].Col  : sorted[0].Row;
            int end   = horizontal ? sorted[^1].Col : sorted[^1].Row;

            var newPositions = new HashSet<int>(sorted.Select(p => horizontal ? p.Col : p.Row));

            for (int i = start; i <= end; i++)
            {
                if (newPositions.Contains(i)) continue; // new tile

                int r = horizontal ? fixedDim : i;
                int c = horizontal ? i : fixedDim;

                if (!board[r, c].IsOccupied)
                    return ValidationResult.Fail("R06_GAP_IN_PLACEMENT",
                        $"There is an empty gap at ({r},{c}) between the tiles you placed.");
            }
            return null;
        }

        /// <summary>
        /// R08: does this new tile touch at least one square that was occupied
        ///      BEFORE the current move (i.e. not by another tile from the same placement)?
        /// </summary>
        private static bool IsAdjacentToExisting(
            AiBoard board,
            TilePlacement p,
            IReadOnlyList<TilePlacement> placements)
        {
            var newPositions = new HashSet<(int, int)>(placements.Select(x => (x.Row, x.Col)));

            foreach (var (dr, dc) in new[] { (-1,0),(1,0),(0,-1),(0,1) })
            {
                int nr = p.Row + dr, nc = p.Col + dc;
                if (!board.InBounds(nr, nc)) continue;
                if (board[nr, nc].IsOccupied && !newPositions.Contains((nr, nc)))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// After temporarily placing tiles, was the board originally empty?
        /// True when every occupied cell belongs to a new placement.
        /// </summary>
        private static bool HasExistingTile(
            AiBoard board,
            IReadOnlyList<TilePlacement> placements)
        {
            var newPos = new HashSet<(int,int)>(placements.Select(p => (p.Row, p.Col)));
            for (int r = 0; r < AiBoard.Size; r++)
            for (int c = 0; c < AiBoard.Size; c++)
            {
                if (board[r, c].IsOccupied && !newPos.Contains((r, c)))
                    return true; // there is at least one pre-existing tile
            }
            return false;
        }

        // -- Word reading ------------------------------------------------------

        /// <summary>
        /// Read the full word that passes through (startRow, startCol) in the given
        /// direction, from the board as it currently is (with new tiles already applied).
        /// </summary>
        private static string ReadWord(AiBoard board, int startRow, int startCol, bool horizontal)
        {
            // Walk backwards to the beginning of the word
            int r = startRow, c = startCol;
            if (horizontal) { while (c > 0 && board[r, c - 1].IsOccupied) c--; }
            else             { while (r > 0 && board[r - 1, c].IsOccupied) r--; }

            // Walk forward, collecting letters
            var sb = new StringBuilder();
            while (board.InBounds(r, c) && board[r, c].IsOccupied)
            {
                sb.Append(board[r, c].Letter);
                if (horizontal) c++; else r++;
            }
            return sb.ToString();
        }

        // -- Temporary board mutation ------------------------------------------

        private static void ApplyPlacements(AiBoard board, IReadOnlyList<TilePlacement> placements)
        {
            foreach (var p in placements)
                board.PlaceLetter(p.Row, p.Col, p.Letter, p.IsBlank);
        }

        private static void UndoPlacements(AiBoard board, IReadOnlyList<TilePlacement> placements)
        {
            foreach (var p in placements)
                board.ClearCell(p.Row, p.Col);
        }
    }
}
