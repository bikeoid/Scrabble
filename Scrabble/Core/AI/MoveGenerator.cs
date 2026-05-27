// MoveGenerator.cs
// Implements the Appel & Jacobson (1988) DAWG-based move generation algorithm.
//
// Key ideas:
//  1. Reduce to 1-D: generate "across" moves; transpose board for "down" moves.
//  2. Anchor squares: empty squares adjacent to occupied squares.
//  3. Cross-check sets: per-empty-square bit masks of letters that form legal
//     perpendicular words.
//  4. Backtrack left-parts then extend right through the DAWG.

using System;
using System.Collections.Generic;

namespace Scrabble.Core.AI
{
    public sealed class MoveGenerator
    {
        private readonly Dawg _dawg;
        private MoveValidator _validator = null;

        public MoveGenerator(Dawg dawg) { 
            _dawg = dawg; 
            _validator = new MoveValidator(dawg);
        }

        // -- Public entry-point ------------------------------------------------

        /// <summary>
        /// Generate every legal move for the given rack on the given board.
        /// Returns moves sorted descending by score.
        /// </summary>
        public List<ScrabbleMove> GenerateAll(AiBoard board, Rack rack)
        {
            var moves = new List<ScrabbleMove>();

            GenerateDirection(board, rack, horizontal: true,  moves);
            GenerateDirection(board, rack, horizontal: false, moves);

            moves.Sort((a, b) => b.Score.CompareTo(a.Score));
            return moves;
        }

        // -- Direction driver --------------------------------------------------

        private void GenerateDirection(
            AiBoard board, Rack rack, bool horizontal, List<ScrabbleMove> results)
        {
            var crossChecks = board.ComputeCrossChecks(_dawg, horizontal);
            var crossSums   = board.ComputeCrossSums(horizontal);
            var anchors     = board.GetAnchors();

            foreach (var (anchorRow, anchorCol) in anchors)
            {
                int row = anchorRow, col = anchorCol;

                // How many free squares are to the left/above of this anchor
                // (the max left-part length)?
                int limit = horizontal
                    ? LeftFreeCount(board, row, col, horizontal)
                    : LeftFreeCount(board, row, col, horizontal);

                var placements = new List<TilePlacement>();

                // Case A: the square immediately to the left/above is occupied -> prefix already on board
                bool prefixOnBoard = horizontal
                    ? (col > 0 && board[row, col - 1].IsOccupied)
                    : (row > 0 && board[row - 1, col].IsOccupied);

                if (prefixOnBoard)
                {
                    // Walk back to the start of the existing prefix
                    string prefix = GetExistingPrefix(board, row, col, horizontal);
                    var node = _dawg.Traverse(prefix);
                    if (node is not null)
                    {
                        int anchorR = horizontal ? row : row;
                        int anchorC = horizontal ? col : col;
                        ExtendRight(board, rack, node, anchorR, anchorC, anchorR, anchorC,
                                    horizontal, crossChecks, crossSums, placements, results, prefix);
                    }
                }
                else
                {
                    // Case B: left-part comes from the rack (0..limit tiles)
                    LeftPart(board, rack, _dawg.Root, string.Empty,
                             row, col, row, col, horizontal,
                             crossChecks, crossSums, limit, placements, results);
                }
            }
        }

        // -- Left-part generation (backtracking) -------------------------------

        private void LeftPart(
            AiBoard board, Rack rack, DawgNode node,
            string partialWord,
            int anchorRow, int anchorCol,
            int currentRow, int currentCol,
            bool horizontal,
            int[,] crossChecks, int[,] crossSums,
            int limit,
            List<TilePlacement> placements,
            List<ScrabbleMove> results)
        {
            // Always try to extend right from current partial word (even empty string)
            ExtendRight(board, rack, node, anchorRow, anchorCol, anchorRow, anchorCol,
                        horizontal, crossChecks, crossSums, placements, results, partialWord);

            if (limit == 0) return;

            foreach (var (letter, child) in node.Children())
            {
                bool usedBlank = false;

                if (rack.CountOf(letter) > 0)
                    rack.TakeExact(letter);
                else if (rack.Blanks > 0)
                { rack.TakeBlank(); usedBlank = true; }
                else
                    continue;

                int nextRow = horizontal ? anchorRow  : anchorRow  - (partialWord.Length + 1);
                int nextCol = horizontal ? anchorCol - (partialWord.Length + 1) : anchorCol;

                placements.Add(new TilePlacement
                {
                    Row = nextRow, Col = nextCol, Letter = letter, IsBlank = usedBlank
                });

                LeftPart(board, rack, child, partialWord + letter,
                         anchorRow, anchorCol, nextRow, nextCol,
                         horizontal, crossChecks, crossSums,
                         limit - 1, placements, results);

                placements.RemoveAt(placements.Count - 1);

                if (usedBlank) rack.ReturnBlank();
                else rack.Return(letter);
            }
        }

        // -- Right extension ---------------------------------------------------

        private void ExtendRight(
            AiBoard board, Rack rack, DawgNode node,
            int anchorRow, int anchorCol,
            int row, int col,
            bool horizontal,
            int[,] crossChecks, int[,] crossSums,
            List<TilePlacement> placements,
            List<ScrabbleMove> results,
            string word)
        {
            if (!board.InBounds(row, col))
            {
                // Off the board -- record if terminal
                if (node.IsTerminal && placements.Count > 0)
                {
                    var validationResult = _validator.Validate(board, placements, null);
                    if (!validationResult.IsValid)
                    {
                        // This can happen if the last placed tile forms an invalid perpendicular word.
                        // In that case, we shouldn't record this move.
                        return;
                    }
                    RecordMove(board, placements, word, anchorRow, anchorCol, horizontal, results);
                }
                return;
            }

            var cell = board[row, col];

            if (cell.IsOccupied)
            {
                // Use the letter already on the board
                char existing = cell.Letter;
                var child = node.GetChild(existing);
                if (child is not null)
                {
                    int nr = horizontal ? row : row + 1;
                    int nc = horizontal ? col + 1 : col;
                    ExtendRight(board, rack, child, anchorRow, anchorCol, nr, nc,
                                horizontal, crossChecks, crossSums, placements, results,
                                word + existing);
                }
                return;
            }

            // Empty square -- try placing a tile
            // Cross-check mask for this square
            int ccMask = crossChecks[row, col];

            // Record if we can stop here (node is terminal and we've played ≥1 new tile)
            if (node.IsTerminal && placements.Count > 0)
            {
                var validationResult = _validator.Validate(board, placements, null);
                if (!validationResult.IsValid)
                {
                    // This can happen if the last placed tile forms an invalid perpendicular word.
                    // In that case, we shouldn't record this move.
                    return;
                }
                RecordMove(board, placements, word, anchorRow, anchorCol, horizontal, results);
            }

            foreach (var (letter, child) in node.Children())
            {
                // Check cross-check
                if ((ccMask & (1 << (letter - 'A'))) == 0) continue;

                // Try regular tile first, then blank
                bool usedBlank = false;
                if (rack.CountOf(letter) > 0)
                    rack.TakeExact(letter);
                else if (rack.Blanks > 0)
                { rack.TakeBlank(); usedBlank = true; }
                else
                    continue;

                placements.Add(new TilePlacement
                    { Row = row, Col = col, Letter = letter, IsBlank = usedBlank });

                int nr2 = horizontal ? row : row + 1;
                int nc2 = horizontal ? col + 1 : col;
                ExtendRight(board, rack, child, anchorRow, anchorCol, nr2, nc2,
                            horizontal, crossChecks, crossSums, placements, results,
                            word + letter);

                placements.RemoveAt(placements.Count - 1);

                if (usedBlank) rack.ReturnBlank();
                else rack.Return(letter);
            }
        }

        // -- Helper: record a completed move -----------------------------------

        private static void RecordMove(
            AiBoard board,
            List<TilePlacement> placements,
            string word,
            int anchorRow, int anchorCol,
            bool horizontal,
            List<ScrabbleMove> results)
        {
            // Deep-copy placements
            var pCopy = new List<TilePlacement>(placements.Count);
            foreach (var p in placements) pCopy.Add(p);

            // Determine true start position
            int startRow = pCopy[0].Row, startCol = pCopy[0].Col;
            if (horizontal)
            {
                // The word may start left of the first placement (existing tiles)
                int c = pCopy[0].Col;
                while (c > 0 && board[pCopy[0].Row, c - 1].IsOccupied) c--;
                startCol = c;
                startRow = pCopy[0].Row;
            }
            else
            {
                int r = pCopy[0].Row;
                while (r > 0 && board[r - 1, pCopy[0].Col].IsOccupied) r--;
                startRow = r;
                startCol = pCopy[0].Col;
            }

            int score = MoveScorer.Score(board, pCopy, horizontal);


            results.Add(new ScrabbleMove
            {
                Word         = word,
                StartRow     = startRow,
                StartCol     = startCol,
                IsHorizontal = horizontal,
                Placements   = pCopy,
                Score        = score
            });
        }

        // -- Helpers -----------------------------------------------------------

        /// <summary>Count free (non-anchor) squares to the left/above of an anchor.</summary>
        private static int LeftFreeCount(AiBoard board, int row, int col, bool horizontal)
        {
            int count = 0;
            if (horizontal)
            {
                int c = col - 1;
                while (c >= 0 && !board[row, c].IsOccupied && !IsAnchor(board, row, c))
                { count++; c--; }
            }
            else
            {
                int r = row - 1;
                while (r >= 0 && !board[r, col].IsOccupied && !IsAnchor(board, r, col))
                { count++; r--; }
            }
            return count;
        }

        private static bool IsAnchor(AiBoard board, int row, int col)
        {
            if (board[row, col].IsOccupied) return false;
            // Adjacent to any occupied square?
            int s = AiBoard.Size;
            if (row > 0   && board[row-1, col].IsOccupied) return true;
            if (row < s-1 && board[row+1, col].IsOccupied) return true;
            if (col > 0   && board[row, col-1].IsOccupied) return true;
            if (col < s-1 && board[row, col+1].IsOccupied) return true;
            return false;
        }

        private static string GetExistingPrefix(AiBoard board, int row, int col, bool horizontal)
        {
            var sb = new System.Text.StringBuilder();
            if (horizontal)
            {
                int c = col - 1;
                while (c >= 0 && board[row, c].IsOccupied) c--;
                c++;
                while (c < col) { sb.Append(board[row, c].Letter); c++; }
            }
            else
            {
                int r = row - 1;
                while (r >= 0 && board[r, col].IsOccupied) r--;
                r++;
                while (r < row) { sb.Append(board[r, col].Letter); r++; }
            }
            return sb.ToString();
        }
    }
}
