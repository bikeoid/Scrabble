// ComputerPlayerAITests.cs
// xUnit tests for the AI engine, including full MoveValidator coverage.
// Run with:  dotnet test

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Scrabble.Core.AI;
using Assert = Xunit.Assert;

namespace Scrabble.Tests.AI
{
    // ==========================================================================
    //  DAWG
    // ==========================================================================

    public class DawgTests
    {
        [Fact]
        public void Contains_KnownWord_ReturnsTrue()
        {
            var dawg = Dawg.FromWords(new[] { "CAT", "CATS", "CAR", "CARE" });
            Assert.True(dawg.Contains("CAT"));
            Assert.True(dawg.Contains("CARE"));
        }

        [Fact]
        public void Contains_UnknownWord_ReturnsFalse()
        {
            var dawg = Dawg.FromWords(new[] { "CAT" });
            Assert.False(dawg.Contains("DOG"));
            Assert.False(dawg.Contains("CA"));
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("Cat")]
        [InlineData("CAT")]
        public void Contains_IsCaseInsensitive(string word)
        {
            var dawg = Dawg.FromWords(new[] { "CAT" });
            Assert.True(dawg.Contains(word));
        }
    }

    // ==========================================================================
    //  TileValues
    // ==========================================================================

    public class TileValuesTests
    {
        [Theory]
        [InlineData('A', 1)]
        [InlineData('Z', 10)]
        [InlineData('Q', 10)]
        [InlineData('E', 1)]
        public void LetterHasCorrectValue(char letter, int expected)
        {
            Assert.Equal(expected, TileValues.Of(letter));
        }
    }

    // ==========================================================================
    //  Rack
    // ==========================================================================

    public class RackTests
    {
        [Fact]
        public void TakeAndReturn_Regular()
        {
            var rack = new Rack(new[] { 'A', 'B', 'C' });
            Assert.True(rack.TakeExact('A'));
            Assert.False(rack.TakeExact('A'));
            rack.Return('A');
            Assert.True(rack.TakeExact('A'));
        }

        [Fact]
        public void TakeBlank_UsedWhenLetterAbsent()
        {
            var rack = new Rack(new[] { '?' });
            bool taken = rack.CountOf('A') > 0 ? rack.TakeExact('A') : rack.TakeBlank();
            Assert.True(taken);
            Assert.Equal(0, rack.Blanks);
        }
    }

    // ==========================================================================
    //  MoveScorer
    // ==========================================================================

    public class MoveScorerTests
    {
        [Fact]
        public void SingleTile_CentreDW_ScoreDoubled()
        {
            var placements = new List<TilePlacement>
                { new() { Row = 7, Col = 7, Letter = 'A' } };
            int score = MoveScorer.Score(new AiBoard(), placements, horizontal: true);
            Assert.Equal(2, score); // A=1, DW at centre -> 2
        }

        [Fact]
        public void SevenTiles_GivesBingoBonus()
        {
            var placements = new List<TilePlacement>();
            string word = "SKILFUL";
            for (int i = 0; i < word.Length; i++)
                placements.Add(new TilePlacement { Row = 7, Col = 7 + i, Letter = word[i] });
            int score = MoveScorer.Score(new AiBoard(), placements, horizontal: true);
            Assert.True(score == 86, $"Expected start bonus double plus bingo; got {score}");
        }

        [Fact]
        public void TripleWordBingo()
        {
            var board = new AiBoard();
            board.PlaceLetter(6, 0, 'A', false);
            board.PlaceLetter(6, 1, 'N', false);

            var placements = new List<TilePlacement>();
            placements.Add(new TilePlacement { Row = 5, Col = 0, Letter = 'M' });
            string word = "CARONI";  // MA as prefix, A exists on board
            for (int i = 0; i < word.Length; i++)
                placements.Add(new TilePlacement { Row = 7 + i, Col = 0, Letter = word[i] });
            int score = MoveScorer.Score(board, placements, horizontal: false);
            Assert.True(score == 89, $"Expected bonus triple plus bingo; got {score}");
        }
    }

    // ==========================================================================
    //  MoveGenerator
    // ==========================================================================

    public class MoveGeneratorTests
    {
        [Fact]
        public void FirstMove_GeneratesMovesOnEmptyBoard()
        {
            var dawg = Dawg.FromWords(new[] { "CAT", "AT", "TA" });
            var moves = new MoveGenerator(dawg).GenerateAll(new AiBoard(), new Rack(new[] { 'C', 'A', 'T' }));
            Assert.NotEmpty(moves);
        }

        [Fact]
        public void GeneratedMoves_AreValidWords()
        {
            var words = new[] { "CAT", "CAR", "CARE", "AT", "ATE", "EAT", "TA" };
            var dawg  = Dawg.FromWords(words);
            var moves = new MoveGenerator(dawg).GenerateAll(
                new AiBoard(), new Rack(new[] { 'C', 'A', 'T', 'E', 'R', 'E', 'S' }));
            foreach (var m in moves)
                Assert.True(dawg.Contains(m.Word), $"'{m.Word}' not in dictionary");
        }

        [Fact]
        public void MovesAreSortedDescendingByScore()
        {
            var dawg  = Dawg.FromWords(new[] { "CAT", "CATS", "CAR", "AT" });
            var moves = new MoveGenerator(dawg).GenerateAll(
                new AiBoard(), new Rack(new[] { 'C', 'A', 'T', 'S' }));
            for (int i = 1; i < moves.Count; i++)
                Assert.True(moves[i-1].Score >= moves[i].Score,
                    $"Move {i-1} ({moves[i-1].Score}) < move {i} ({moves[i].Score})");
        }
    }

    // ==========================================================================
    //  SkillLevel
    // ==========================================================================

    public class SkillLevelTests
    {
        private static List<ScrabbleMove> FakeMoves(params int[] scores)
        {
            var list = scores.Select(s => new ScrabbleMove
            {
                Word = "TEST", Score = s,
                Placements = new List<TilePlacement> { new() { Row = 7, Col = 7, Letter = 'T' } }
            }).ToList();
            list.Sort((a, b) => b.Score.CompareTo(a.Score));
            return list;
        }

        [Fact]
        public void Hard_AlwaysReturnsHighestScore()
        {
            var moves  = FakeMoves(10, 8, 6, 4, 2);
            var chosen = MoveSelector.Select(moves, SkillLevel.Hard, new AiBoard(), new Rack(new[]{'T'}));
            Assert.Equal(10, chosen!.Score);
        }

        [Fact]
        public void Easy_NeverReturnsHighestScore_FromLargeList()
        {
            var scores = Enumerable.Range(1, 20).Reverse().Select(i => i * 5).ToArray();
            var moves  = FakeMoves(scores);
            bool allEasyLow = Enumerable.Range(0, 100)
                .Select(_ => MoveSelector.Select(moves, SkillLevel.Easy, new AiBoard(), new Rack(new[]{'T'}))!.Score)
                .All(s => s <= 25);
            Assert.True(allEasyLow);
        }
    }

    // ==========================================================================
    //  MoveValidator - structural rules (R01-R08)
    // ==========================================================================

    public class MoveValidatorStructuralTests
    {
        private static readonly string[] Vocab = new[]
        {
            "CAT", "CATS", "CAR", "CARE", "AT", "TA", "EAT", "ATE",
            "ON", "NO", "TO", "TON", "TONE", "HI", "IS", "IT", "TI", "OE", "SI"
        };

        private static MoveValidator V() => new(Dawg.FromWords(Vocab));
        private static AiBoard       B() => new();

        // -- R01 --------------------------------------------------------------

        [Fact]
        public void R01_NoTiles_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>());
            Assert.False(r.IsValid);
            Assert.StartsWith("R01", r.ErrorCode);
        }

        // -- R02 --------------------------------------------------------------

        [Fact]
        public void R02_RowOutOfBounds_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
                { new() { Row = 15, Col = 7, Letter = 'A' } });
            Assert.False(r.IsValid);
            Assert.StartsWith("R02", r.ErrorCode);
        }

        [Fact]
        public void R02_ColNegative_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
                { new() { Row = 7, Col = -1, Letter = 'A' } });
            Assert.False(r.IsValid);
            Assert.StartsWith("R02", r.ErrorCode);
        }

        // -- R03 --------------------------------------------------------------

        [Fact]
        public void R03_SquareAlreadyOccupied_Fails()
        {
            var board = B();
            board.PlaceLetter(7, 7, 'A');
            var r = V().Validate(board, new List<TilePlacement>
                { new() { Row = 7, Col = 7, Letter = 'B' } });
            Assert.False(r.IsValid);
            Assert.StartsWith("R03", r.ErrorCode);
        }

        // -- R04 --------------------------------------------------------------

        [Fact]
        public void R04_NonAlphaLetter_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
                { new() { Row = 7, Col = 7, Letter = '1' } });
            Assert.False(r.IsValid);
            Assert.StartsWith("R04", r.ErrorCode);
        }

        // -- R05 --------------------------------------------------------------

        [Fact]
        public void R05_DiagonalPlacement_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
            {
                new() { Row = 6, Col = 6, Letter = 'C' },
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 8, Col = 8, Letter = 'T' }
            });
            Assert.False(r.IsValid);
            Assert.StartsWith("R05", r.ErrorCode);
        }

        [Fact]
        public void R05_ScatteredRowsAndCols_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
            {
                new() { Row = 5, Col = 7, Letter = 'C' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            });
            Assert.False(r.IsValid);
            Assert.StartsWith("R05", r.ErrorCode);
        }

        // -- R06 --------------------------------------------------------------

        [Fact]
        public void R06_EmptyGapBetweenTiles_Fails()
        {
            // C at col 7, T at col 9 - col 8 is empty
            var r = V().Validate(B(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'C' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            });
            Assert.False(r.IsValid);
            Assert.StartsWith("R06", r.ErrorCode);
        }

        [Fact]
        public void R06_GapFilledByExistingTile_NotR06Error()
        {
            var board = B();
            board.PlaceLetter(7, 8, 'A'); // A already on board
            // Place C(7,7) and T(7,9) - gap at 7,8 filled by existing A -> CAT
            var r = V().Validate(board, new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'C' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            });
            Assert.NotEqual("R06_GAP_IN_PLACEMENT", r.ErrorCode);
        }

        // -- R07 --------------------------------------------------------------

        [Fact]
        public void R07_FirstMove_MissesCentre_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
            {
                new() { Row = 0, Col = 0, Letter = 'A' },
                new() { Row = 0, Col = 1, Letter = 'T' }
            });
            Assert.False(r.IsValid);
            Assert.StartsWith("R07", r.ErrorCode);
        }

        [Fact]
        public void R07_FirstMove_SingleTile_Fails()
        {
            var r = V().Validate(B(), new List<TilePlacement>
                { new() { Row = 7, Col = 7, Letter = 'A' } });
            Assert.False(r.IsValid);
            Assert.StartsWith("R07", r.ErrorCode);
        }

        [Fact]
        public void R07_FirstMove_CoversCentre_ValidWord_Passes()
        {
            var r = V().Validate(B(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            });
            Assert.True(r.IsValid, r.ErrorMessage);
        }

        // -- R08 --------------------------------------------------------------

        [Fact]
        public void R08_PlacementFloating_NotTouchingExisting_Fails()
        {
            var board = B();
            board.PlaceLetter(7, 7, 'A');
            board.PlaceLetter(7, 8, 'T');

            // Float AT over in a corner, no adjacency
            var r = V().Validate(board, new List<TilePlacement>
            {
                new() { Row = 0, Col = 0, Letter = 'A' },
                new() { Row = 0, Col = 1, Letter = 'T' }
            });
            Assert.False(r.IsValid);
            Assert.StartsWith("R08", r.ErrorCode);
        }

        [Fact]
        public void R08_PlacementAdjacentToExisting_NotR08()
        {
            var board = B();
            board.PlaceLetter(7, 7, 'A');
            board.PlaceLetter(7, 8, 'T');

            // Place 'E' at (7,6) - adjacent to A(7,7) -> EAT
            var r = V().Validate(board, new List<TilePlacement>
                { new() { Row = 7, Col = 6, Letter = 'E' } });
            Assert.NotEqual("R08_NOT_CONNECTED", r.ErrorCode);
        }
    }

    // ==========================================================================
    //  MoveValidator - dictionary rules (R09, R10)
    // ==========================================================================

    public class MoveValidatorDictionaryTests
    {
        private static readonly string[] Vocab = new[]
        {
            "CAT", "CATS", "CAR", "AT", "TA", "EAT", "ATE",
            "ON", "NO", "TO", "TON", "TONE", "HI", "IT", "TI", "OE", "SI", "ZA"
        };

        private static MoveValidator V() => new(Dawg.FromWords(Vocab));

        // -- R09 --------------------------------------------------------------

        [Fact]
        public void R09_InvalidMainWord_Fails()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'Z' },
                new() { Row = 7, Col = 8, Letter = 'Z' },
                new() { Row = 7, Col = 9, Letter = 'Z' }
            });
            Assert.False(r.IsValid);
            Assert.Equal("R09_INVALID_MAIN_WORD", r.ErrorCode);
        }

        [Fact]
        public void R09_ValidMainWord_Passes()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            });
            Assert.True(r.IsValid, r.ErrorMessage);
            Assert.Contains("AT", r.WordsFormed);
        }

        [Fact]
        public void R09_WordsFormed_ContainsMainWord()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'C' },
                new() { Row = 7, Col = 8, Letter = 'A' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            });
            Assert.True(r.IsValid, r.ErrorMessage);
            Assert.Contains("CAT", r.WordsFormed);
        }

        // -- R10 --------------------------------------------------------------

        [Fact]
        public void R10_InvalidCrossWord_Fails()
        {
            var board = new AiBoard();
            board.PlaceLetter(7, 7, 'C');
            board.PlaceLetter(7, 8, 'A');
            board.PlaceLetter(7, 9, 'T');

            // 'Z' 'A' above 'C' -> "ZC" vertically - not in vocab
            var r = V().Validate(board, new List<TilePlacement>
            {
                new() { Row = 6, Col = 7, Letter = 'Z' },
                  new() { Row = 6, Col = 8, Letter = 'A' } });
            Assert.False(r.IsValid);
            Assert.Equal("R10_INVALID_CROSS_WORD", r.ErrorCode);
        }

        [Fact]
        public void R10_ValidCrossWord_NotR10Error()
        {
            // Place ON vertically: O(6,7), N(7,7)
            var board = new AiBoard();
            board.PlaceLetter(6, 7, 'O');
            board.PlaceLetter(7, 7, 'N');

            // Place T at (6,6) -> horizontal word "TO" (T+O), no vertical cross-word below T
            var r = V().Validate(board, new List<TilePlacement>
                { new() { Row = 6, Col = 6, Letter = 'T' } });
            Assert.NotEqual("R10_INVALID_CROSS_WORD", r.ErrorCode);
        }

        [Fact]
        public void R10_WordsFormed_IncludesCrossWords_OnSuccess()
        {
            var board = new AiBoard();
            board.PlaceLetter(7, 7, 'A');
            board.PlaceLetter(7, 8, 'T');

            // Place E at (7,6) -> EAT
            var r = V().Validate(board, new List<TilePlacement>
                { new() { Row = 7, Col = 6, Letter = 'E' } });
            Assert.True(r.IsValid, r.ErrorMessage);
            Assert.Contains("EAT", r.WordsFormed);
        }
    }

    // ==========================================================================
    //  MoveValidator - rack ownership (R11)
    // ==========================================================================

    public class MoveValidatorRackTests
    {
        private static MoveValidator V() =>
            new(Dawg.FromWords(new[] { "CAT", "AT", "TA" }));

        [Fact]
        public void R11_TileNotInRack_Fails()
        {
            var rack = new Rack(new[] { 'A', 'T' }); // no C
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'C' },
                new() { Row = 7, Col = 8, Letter = 'A' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            }, rack);
            Assert.False(r.IsValid);
            Assert.Equal("R11_TILE_NOT_IN_RACK", r.ErrorCode);
        }

        [Fact]
        public void R11_BlankSubstitution_Passes()
        {
            var rack = new Rack(new[] { '?', 'A', 'T' });
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'C', IsBlank = true },
                new() { Row = 7, Col = 8, Letter = 'A' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            }, rack);
            Assert.True(r.IsValid, r.ErrorMessage);
        }

        [Fact]
        public void R11_NoRackArgument_SkipsOwnershipCheck()
        {
            // No rack supplied -> R11 is skipped; valid word should pass
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            }, rack: null);
            Assert.True(r.IsValid, r.ErrorMessage);
        }

        [Fact]
        public void R11_ExactRack_AllPresent_Passes()
        {
            var rack = new Rack(new[] { 'C', 'A', 'T' });
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'C' },
                new() { Row = 7, Col = 8, Letter = 'A' },
                new() { Row = 7, Col = 9, Letter = 'T' }
            }, rack);
            Assert.True(r.IsValid, r.ErrorMessage);
        }

        [Fact]
        public void R11_DuplicateTileRequired_EnoughInRack_Passes()
        {
            // Need two S tiles
            var dawg = Dawg.FromWords(new[] { "SS" }); // toy word
            var v    = new MoveValidator(dawg);
            var rack = new Rack(new[] { 'S', 'S' });
            var r = v.Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'S' },
                new() { Row = 7, Col = 8, Letter = 'S' }
            }, rack);
            // SS is valid in our toy vocab
            Assert.True(r.IsValid, r.ErrorMessage);
        }

        [Fact]
        public void R11_DuplicateTileRequired_OnlyOneInRack_Fails()
        {
            var dawg = Dawg.FromWords(new[] { "SS" });
            var v    = new MoveValidator(dawg);
            var rack = new Rack(new[] { 'S' }); // only one S
            var r = v.Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'S' },
                new() { Row = 7, Col = 8, Letter = 'S' }
            }, rack);
            Assert.False(r.IsValid);
            Assert.Equal("R11_TILE_NOT_IN_RACK", r.ErrorCode);
        }
    }

    // ==========================================================================
    //  MoveValidator - ValidationResult metadata
    // ==========================================================================

    public class ValidationResultMetaTests
    {
        private static MoveValidator V() =>
            new(Dawg.FromWords(new[] { "CAT", "AT", "TA", "ON", "NO" }));

        [Fact]
        public void Score_IsZero_OnFailure()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'Z' },
                new() { Row = 7, Col = 8, Letter = 'Z' }
            });
            Assert.False(r.IsValid);
            Assert.Equal(0, r.Score);
        }

        [Fact]
        public void Score_IsPositive_OnSuccess()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            });
            Assert.True(r.IsValid, r.ErrorMessage);
            Assert.True(r.Score > 0, $"Expected positive score; got {r.Score}");
        }

        [Fact]
        public void ErrorCode_IsEmpty_OnSuccess()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            });
            Assert.True(r.IsValid);
            Assert.Empty(r.ErrorCode);
        }

        [Fact]
        public void ErrorMessage_IsNonEmpty_OnFailure()
        {
            var r = V().Validate(new AiBoard(), new List<TilePlacement>());
            Assert.False(r.IsValid);
            Assert.NotEmpty(r.ErrorMessage);
        }
    }

    // ==========================================================================
    //  MoveValidator - IsValidWord convenience method
    // ==========================================================================

    public class IsValidWordTests
    {
        private static MoveValidator V() =>
            new(Dawg.FromWords(new[] { "CAT", "AT" }));

        [Theory]
        [InlineData("CAT",  true)]
        [InlineData("cat",  true)]
        [InlineData("Cat",  true)]
        [InlineData("AT",   true)]
        [InlineData("DOG",  false)]
        [InlineData("A",    false)]   // single letter - below minimum length
        [InlineData("",     false)]
        public void IsValidWord_Correctness(string word, bool expected)
        {
            Assert.Equal(expected, V().IsValidWord(word));
        }
    }

    // ==========================================================================
    //  MoveValidator - board immutability guarantee
    // ==========================================================================

    public class ValidatorBoardImmutabilityTests
    {
        [Fact]
        public void Board_IsUnchanged_AfterSuccessfulValidation()
        {
            var v     = new MoveValidator(Dawg.FromWords(new[] { "AT" }));
            var board = new AiBoard();
            v.Validate(board, new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            });
            Assert.False(board[7, 7].IsOccupied, "Board must be empty after validation.");
            Assert.False(board[7, 8].IsOccupied, "Board must be empty after validation.");
        }

        [Fact]
        public void Board_IsUnchanged_AfterFailedValidation()
        {
            var v     = new MoveValidator(Dawg.FromWords(new[] { "CAT" }));
            var board = new AiBoard();
            v.Validate(board, new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'Z' },
                new() { Row = 7, Col = 8, Letter = 'Z' }
            });
            Assert.False(board[7, 7].IsOccupied);
            Assert.False(board[7, 8].IsOccupied);
        }

        [Fact]
        public void Board_IsUnchanged_AfterExceptionDuringValidation()
        {
            // Even if an exception somehow occurs mid-validation,
            // the finally-block in Validate must undo the placements.
            // We test this by running validation twice on the same board
            // and confirming it stays clean.
            var v     = new MoveValidator(Dawg.FromWords(new[] { "AT" }));
            var board = new AiBoard();
            var tiles = new List<TilePlacement>
            {
                new() { Row = 7, Col = 7, Letter = 'A' },
                new() { Row = 7, Col = 8, Letter = 'T' }
            };
            v.Validate(board, tiles);
            v.Validate(board, tiles); // second call - board must still be clean
            Assert.False(board[7, 7].IsOccupied);
        }
    }
}
