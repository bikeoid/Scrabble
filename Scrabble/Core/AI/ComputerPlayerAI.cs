// ComputerPlayerAI.cs
//
// HOW TO INTEGRATE
// -----------------
// 1. Add all files in this folder to the Scrabble.Core project.
// 2. Register the service in Program.cs:
//       builder.Services.AddSingleton<ComputerPlayerAI>();
// 3. Where the server processes a computer player's turn (look for wherever the
//    existing "Computer" logic runs), inject ComputerPlayerAI and call MakeMoveAsync.
// 4. Configure skill level per game/player in appsettings.json or via the
//    player settings UI (add a "SkillLevel" field to the player model).
//
// DICTIONARY
// ----------
// The DAWG is built once from the existing dictionary file already used by the
// game (TWL06 / SOWPODS).  Point DictionaryPath at the same file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Scrabble.Core.AI
{
    /// <summary>
    /// Adapter: maps the game's own board/rack types into AiBoard/Rack, generates
    /// moves, validates them, and returns the chosen move in a format the game server can apply.
    /// </summary>
    public sealed class ComputerPlayerAI
    {
        // -- Configuration -----------------------------------------------------

        /// <summary>
        /// Path to the plain-text word list (one word per line, any case).
        /// Defaults to the TWL06 file shipped with the project.
        /// </summary>
        public string DictionaryPath { get; set; } =
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "TWL06a.txt");  // Server side path only

        // -- Internal state ----------------------------------------------------

        private Dawg?          _dawg;
        private MoveGenerator? _generator;
        private MoveValidator? _validator;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly ILogger<ComputerPlayerAI> _logger;

        public ComputerPlayerAI(ILogger<ComputerPlayerAI> logger) => _logger = logger;

        public List<string> TwoLetterWords => _dawg is not null ? _dawg.TwoLetterWords : new List<string>();

        // -- Initialisation ----------------------------------------------------

        /// <summary>
        /// Eagerly load the DAWG.  Call once at startup (e.g. from Program.cs
        /// after building the host) to avoid a cold-start delay on the first move.
        /// </summary>
        public async Task InitialiseAsync(MemoryStream memoryStream = null)
        {
            await _initLock.WaitAsync();
            try
            {
                if (_dawg is not null) return;
                if (memoryStream == null)
                {
                    _logger.LogInformation("Building DAWG from {Path}", DictionaryPath);
                    _dawg = await Task.Run(() => Dawg.FromFile(DictionaryPath));
                } else
                {
                    _logger.LogInformation("Building DAWG from memory stream");
                    _dawg = await Task.Run(() => Dawg.FromMemoryStreamAsync(memoryStream));
                }
                _generator = new MoveGenerator(_dawg);
                _validator = new MoveValidator(_dawg);
                _logger.LogInformation("DAWG ready.");
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task EnsureInitialisedAsync()
        {
            if (_generator is null) await InitialiseAsync();
        }

        // -- Public API --------------------------------------------------------

        /// <summary>
        /// Compute the computer's move.
        /// </summary>
        /// <param name="boardLetters">
        ///   15x15 array of characters.  '\0' (or ' ') = empty; uppercase letter = placed tile.
        /// </param>
        /// <param name="boardBlanks">
        ///   15x15 bool array; true = the tile at that square was played as a blank.
        /// </param>
        /// <param name="rackTiles">
        ///   Up to 7 characters from the computer's rack.
        ///   Use '?' to represent blank tiles.
        /// </param>
        /// <param name="skill">Difficulty level.</param>
        /// <returns>
        ///   The chosen <see cref="ScrabbleMove"/>, or null if the computer should pass/exchange.
        /// </returns>
        public async Task<ScrabbleMove?> MakeMoveAsync(
            char[,]  boardLetters,
            bool[,]  boardBlanks,
            char[]   rackTiles,
            SkillLevel skill = SkillLevel.Hard)
        {
            await EnsureInitialisedAsync();

            // Build AI board from game state
            var board = BuildBoard(boardLetters, boardBlanks);
            var rack  = new Rack(rackTiles);

            // Generate all legal moves
            var moves = _generator!.GenerateAll(board, rack);

            _logger.LogDebug("Generated {Count} legal moves at skill {Skill}", moves.Count, skill);

            if (moves.Count == 0)
            {
                _logger.LogInformation("No legal moves found; computer will pass.");
                return null;
            }

            // Select move according to skill level
            var chosen = MoveSelector.Select(moves, skill, board, rack);

            _logger.LogInformation("Computer plays: {Move}", chosen);
            return chosen;
        }

        // -- Convenience overload using the game's existing string representation --

        /// <summary>
        /// Overload that accepts the rack as a string (e.g. "AEILNST" or "AEI?NST").
        /// </summary>
        public Task<ScrabbleMove?> MakeMoveAsync(
            char[,]  boardLetters,
            bool[,]  boardBlanks,
            string   rack,
            SkillLevel skill = SkillLevel.Hard)
            => MakeMoveAsync(boardLetters, boardBlanks, rack.ToUpperInvariant().ToCharArray(), skill);

        // -- Board adapter -----------------------------------------------------

        private static AiBoard BuildBoard(char[,] letters, bool[,] blanks)
        {
            var board = new AiBoard();
            for (int r = 0; r < AiBoard.Size; r++)
            for (int c = 0; c < AiBoard.Size; c++)
            {
                char ch = letters[r, c];
                if (ch != '\0' && ch != ' ')
                    board.PlaceLetter(r, c, char.ToUpperInvariant(ch), blanks[r, c]);
            }
            return board;
        }

        // -- Validation API ----------------------------------------------------

        /// <summary>
        /// Validate a human (or externally supplied) move before committing it.
        /// Returns a <see cref="ValidationResult"/> with a detailed error when invalid.
        /// </summary>
        /// <param name="boardLetters">Current board state (before the move).</param>
        /// <param name="boardBlanks">Blank flags for each board position.</param>
        /// <param name="placements">The tiles the player wants to place.</param>
        /// <param name="rackTiles">
        ///   Optional player rack.  When supplied, R11 (rack ownership) is also enforced.
        /// </param>
        public async Task<ValidationResult> ValidateMoveAsync(
            char[,]              boardLetters,
            bool[,]              boardBlanks,
            IReadOnlyList<TilePlacement> placements,
            char[]?              rackTiles = null)
        {
            await EnsureInitialisedAsync();

            var board = BuildBoard(boardLetters, boardBlanks);
            var rack  = rackTiles is null ? null : new Rack(rackTiles);

            return _validator!.Validate(board, placements, rack);
        }

        /// <summary>
        /// Check whether a single word appears in the dictionary.
        /// Useful for the "Check Word" feature already present in the UI.
        /// </summary>
        public async Task<bool> IsValidWordAsync(string word)
        {
            await EnsureInitialisedAsync();
            return _validator!.IsValidWord(word);
        }

        // -- Integration helpers -----------------------------------------------

        /// <summary>
        /// Converts a <see cref="ScrabbleMove"/> back to the format expected by the
        /// game server.  Extend this method to map into whatever DTO the server uses.
        /// </summary>
        public static MoveDto ToMoveDto(ScrabbleMove move)
        {
            var tiles = move.Placements.Select(p => new PlacedTileDto
            {
                Row     = p.Row,
                Col     = p.Col,
                Letter  = p.Letter,
                IsBlank = p.IsBlank
            }).ToList();

            return new MoveDto
            {
                Word         = move.Word,
                StartRow     = move.StartRow,
                StartCol     = move.StartCol,
                IsHorizontal = move.IsHorizontal,
                Score        = move.Score,
                Tiles        = tiles
            };
        }
    }

    // -- Lightweight DTOs for server integration -------------------------------
    // Replace / extend these to match the actual server models.

    public sealed class PlacedTileDto
    {
        public int  Row     { get; init; }
        public int  Col     { get; init; }
        public char Letter  { get; init; }
        public bool IsBlank { get; init; }
    }

    public sealed class MoveDto
    {
        public string              Word         { get; init; } = string.Empty;
        public int                 StartRow     { get; init; }
        public int                 StartCol     { get; init; }
        public bool                IsHorizontal { get; init; }
        public int                 Score        { get; init; }
        public List<PlacedTileDto> Tiles        { get; init; } = new();
    }
}
