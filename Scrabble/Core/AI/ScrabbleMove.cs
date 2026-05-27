// ScrabbleMove.cs
// Represents a candidate move and computes its score.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Scrabble.Core.AI
{
    /// <summary>
    /// A single tile placement within a move.
    /// </summary>
    public sealed class TilePlacement
    {
        public int Row { get; init; }
        public int Col { get; init; }
        public char Letter { get; init; }   // The letter being played (the face letter)
        public bool IsBlank { get; init; }  // True if this tile is a blank
    }

    /// <summary>
    /// A complete candidate move: a word, its position/direction, which tiles are placed,
    /// and the total score.
    /// </summary>
    public sealed class ScrabbleMove : IComparable<ScrabbleMove>
    {
        public string Word { get; init; } = string.Empty;
        public int StartRow { get; init; }
        public int StartCol { get; init; }
        public bool IsHorizontal { get; init; }
        public List<TilePlacement> Placements { get; init; } = new();
        public int Score { get; set; }

        public int CompareTo(ScrabbleMove? other) =>
            other is null ? 1 : Score.CompareTo(other.Score);

        public override string ToString() =>
            $"{Word} at ({StartRow},{StartCol}) {(IsHorizontal ? "->" : "↓")} [{Score}pts]";
    }

    /// <summary>
    /// Computes the Scrabble score for a move, including premium squares and
    /// cross-word bonuses.  Premiums only apply to newly placed tiles.
    /// </summary>
    public static class MoveScorer
    {
        private const int BingoBonus = 50;
        private const int RackSize   = 7;

        /// <summary>
        /// Score a move on the given board.  Tiles in <paramref name="placements"/> are
        /// the newly-placed tiles; existing board tiles are read from <paramref name="board"/>.
        /// </summary>
        public static int Score(
            AiBoard board,
            List<TilePlacement> placements,
            bool horizontal)
        {
            // Build a lookup for new tiles by position
            var newTiles = new Dictionary<(int, int), TilePlacement>();
            foreach (var p in placements)
                newTiles[(p.Row, p.Col)] = p;

            // -- Main word score ---------------------------------------------

            int mainWordScore = 0;
            int mainWordMult  = 1;

            // Walk the full extent of the word on the board + new placements
            int minR, maxR, minC, maxC;
            if (horizontal)
            {
                int row = placements[0].Row;
                minR = maxR = row;
                // Find leftmost occupied or new-tile column
                minC = placements.Min(p => p.Col);
                while (minC > 0 && board[row, minC - 1].IsOccupied) minC--;
                maxC = placements.Max(p => p.Col);
                while (maxC < AiBoard.Size - 1 && board[row, maxC + 1].IsOccupied) maxC++;
            }
            else
            {
                int col = placements[0].Col;
                minC = maxC = col;
                minR = placements.Min(p => p.Row);
                while (minR > 0 && board[minR - 1, col].IsOccupied) minR--;
                maxR = placements.Max(p => p.Row);
                while (maxR < AiBoard.Size - 1 && board[maxR + 1, col].IsOccupied) maxR++;
            }

            for (int r = minR; r <= maxR; r++)
            for (int c = minC; c <= maxC; c++)
            {
                bool isNew = newTiles.TryGetValue((r, c), out var np);
                char letter = isNew ? np!.Letter : board[r, c].Letter;
                bool isBlank = isNew ? np!.IsBlank : board[r, c].IsBlank;
                int tileVal = isBlank ? 0 : TileValues.Of(letter);

                if (isNew)
                {
                    var prem = board[r, c].Premium;
                    if (prem == Premium.DoubleLetter) tileVal *= 2;
                    else if (prem == Premium.TripleLetter) tileVal *= 3;
                    else if (prem == Premium.DoubleWord) mainWordMult *= 2;
                    else if (prem == Premium.TripleWord) mainWordMult *= 3;
                }

                mainWordScore += tileVal;
            }

            int totalScore = mainWordScore * mainWordMult;

            // -- Cross-word scores --------------------------------------------
            // Each newly-placed tile that is adjacent to perpendicular existing tiles
            // forms a cross-word; score it independently.

            foreach (var p in placements)
            {
                int crossScore = 0;
                int crossMult  = 1;
                bool hasCross  = false;

                var prem = board[p.Row, p.Col].Premium;
                if (prem == Premium.DoubleWord) crossMult *= 2;
                else if (prem == Premium.TripleWord) crossMult *= 3;

                int tileVal = p.IsBlank ? 0 : TileValues.Of(p.Letter);
                if (prem == Premium.DoubleLetter) tileVal *= 2;
                else if (prem == Premium.TripleLetter) tileVal *= 3;

                if (horizontal)
                {
                    // Check vertical cross-word
                    int pr = p.Row - 1;
                    while (pr >= 0 && board[pr, p.Col].IsOccupied)
                    {
                        crossScore += board[pr, p.Col].IsBlank ? 0 : TileValues.Of(board[pr, p.Col].Letter);
                        hasCross = true;
                        pr--;
                    }
                    int sr = p.Row + 1;
                    while (sr < AiBoard.Size && board[sr, p.Col].IsOccupied)
                    {
                        crossScore += board[sr, p.Col].IsBlank ? 0 : TileValues.Of(board[sr, p.Col].Letter);
                        hasCross = true;
                        sr++;
                    }
                }
                else
                {
                    // Check horizontal cross-word
                    int pc = p.Col - 1;
                    while (pc >= 0 && board[p.Row, pc].IsOccupied)
                    {
                        crossScore += board[p.Row, pc].IsBlank ? 0 : TileValues.Of(board[p.Row, pc].Letter);
                        hasCross = true;
                        pc--;
                    }
                    int sc = p.Col + 1;
                    while (sc < AiBoard.Size && board[p.Row, sc].IsOccupied)
                    {
                        crossScore += board[p.Row, sc].IsBlank ? 0 : TileValues.Of(board[p.Row, sc].Letter);
                        hasCross = true;
                        sc++;
                    }
                }

                if (hasCross)
                    totalScore += (crossScore + tileVal) * crossMult;
            }

            // -- Bingo bonus --------------------------------------------------
            if (placements.Count == RackSize)
                totalScore += BingoBonus;

            return totalScore;
        }
    }
}
