// ScrabbleBoardAI.cs
// Lightweight board representation used by the AI engine.
// Designed to wrap (or mirror) the existing game board without modifying it.

using System;
using System.Collections.Generic;

namespace Scrabble.Core.AI
{
    // -- Tile letter-scores (standard English Scrabble) ------------------------

    public static class TileValues
    {
        private static readonly int[] _values = new int[27]; // index 0='A'..25='Z', 26=blank

        static TileValues()
        {
            // Standard distribution
            var map = new Dictionary<char, int>
            {
                ['A'] = 1,  ['B'] = 3,  ['C'] = 3,  ['D'] = 2,  ['E'] = 1,
                ['F'] = 4,  ['G'] = 2,  ['H'] = 4,  ['I'] = 1,  ['J'] = 8,
                ['K'] = 5,  ['L'] = 1,  ['M'] = 3,  ['N'] = 1,  ['O'] = 1,
                ['P'] = 3,  ['Q'] = 10, ['R'] = 1,  ['S'] = 1,  ['T'] = 1,
                ['U'] = 1,  ['V'] = 4,  ['W'] = 4,  ['X'] = 8,  ['Y'] = 4,
                ['Z'] = 10
            };
            foreach (var kv in map)
                _values[kv.Key - 'A'] = kv.Value;
            _values[26] = 0; // blank
        }

        public static int Of(char letter) =>
            letter == '?' ? 0 : _values[Math.Max(0, Math.Min(25, letter - 'A'))];
    }

    // -- Premium square types --------------------------------------------------

    public enum Premium { None, DoubleLetter, TripleLetter, DoubleWord, TripleWord }

    // -- Board cell ------------------------------------------------------------

    public sealed class BoardCell
    {
        public char Letter { get; set; }        // '\0' = empty
        public bool IsBlank { get; set; }       // tile is a blank (worth 0)
        public Premium Premium { get; set; }
        public bool IsOccupied => Letter != '\0';
    }

    // -- Rack -----------------------------------------------------------------

    public sealed class Rack
    {
        // counts[0..25] = regular letters, counts[26] = blanks
        private readonly int[] _counts = new int[27];

        public Rack(IEnumerable<char> tiles)
        {
            foreach (char t in tiles)
            {
                if (t == '?' || t == '\0' || t == ' ')
                {
                    _counts[26]++;
                }
                else
                {
                    _counts[t - 'A']++;
                }
            }
        }

        public int CountOf(char letter) => _counts[letter - 'A'];
        public int Blanks => _counts[26];
        public int Total => Sum();

        private int Sum() { int s = 0; foreach (var n in _counts) s += n; return s; }

        public bool Take(char letter)
        {
            int idx = letter - 'A';
            if (_counts[idx] > 0) { _counts[idx]--; return true; }
            if (_counts[26] > 0) { _counts[26]--; return true; } // use blank
            return false;
        }

        public bool TakeExact(char letter)
        {
            int idx = letter - 'A';
            if (_counts[idx] > 0) { _counts[idx]--; return true; }
            return false;
        }

        public bool TakeBlank()
        {
            if (_counts[26] > 0) { _counts[26]--; return true; }
            return false;
        }

        public void Return(char letter) => _counts[letter - 'A']++;
        public void ReturnBlank() => _counts[26]++;

        public Rack Clone()
        {
            var r = new Rack(Array.Empty<char>());
            Array.Copy(_counts, r._counts, 27);
            return r;
        }

        public IEnumerable<char> Letters()
        {
            for (int i = 0; i < 26; i++)
                for (int j = 0; j < _counts[i]; j++)
                    yield return (char)('A' + i);
            for (int j = 0; j < _counts[26]; j++)
                yield return '?';
        }
    }

    // -- Board -----------------------------------------------------------------

    public sealed class AiBoard
    {
        public const int Size = 15;
        public const int Center = 7;

        private readonly BoardCell[,] _cells = new BoardCell[Size, Size];

        public AiBoard()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    _cells[r, c] = new BoardCell { Premium = GetPremium(r, c) };
        }

        public BoardCell this[int row, int col] => _cells[row, col];

        public bool IsEmpty => !AnyOccupied();

        private bool AnyOccupied()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (_cells[r, c].IsOccupied) return false;
            return true;
        }

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Size && col >= 0 && col < Size;

        // -- Standard 15x15 premium layout ------------------------------------
        private static Premium GetPremium(int r, int c)
        {
            // Normalise to top-left quadrant for symmetry
            int row = Math.Min(r, 14 - r);
            int col = Math.Min(c, 14 - c);

            // Triple-word squares
            if (row == 0 && (col == 0 || col == 7)) return Premium.TripleWord;
            if (row == 7 && col == 0) return Premium.TripleWord;

            // Double-word squares (and center)
            if (row == col && row <= 4) return Premium.DoubleWord;
            if (row == 7 && col == 7) return Premium.DoubleWord; // center

            // Triple-letter
            if (row == 1 && col == 5) return Premium.TripleLetter;
            if (row == 5 && (col == 1 || col == 5)) return Premium.TripleLetter;

            // Double-letter
            if (row == 0 && col == 3) return Premium.DoubleLetter;
            if (row == 2 && col == 6) return Premium.DoubleLetter;
            if (row == 3 && (col == 0 || col == 7)) return Premium.DoubleLetter;
            if (row == 6 && (col == 2 || col == 6)) return Premium.DoubleLetter;
            if (row == 7 && col == 3) return Premium.DoubleLetter;

            return Premium.None;
        }

        // -- Mutation helpers used by scoring ---------------------------------

        public void PlaceLetter(int row, int col, char letter, bool isBlank = false)
        {
            _cells[row, col].Letter = letter;
            _cells[row, col].IsBlank = isBlank;
        }

        public void ClearCell(int row, int col)
        {
            _cells[row, col].Letter = '\0';
            _cells[row, col].IsBlank = false;
        }

        // -- Cross-check computation -------------------------------------------

        /// <summary>
        /// For every empty square, compute which letters can be placed there
        /// without creating an illegal perpendicular word (cross-check sets).
        /// Returns a 15x15 array of 26-bit masks (bit i set -> letter 'A'+i allowed).
        /// A mask of 0x3FFFFFF means "all letters allowed" (no perpendicular constraint).
        /// </summary>
        public int[,] ComputeCrossChecks(Dawg dawg, bool horizontal)
        {
            // When horizontal=true  we check vertical words (down-direction constraint).
            // When horizontal=false we check horizontal words (across-direction constraint).
            var result = new int[Size, Size];

            for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (_cells[r, c].IsOccupied) { result[r, c] = 0; continue; }

                // Gather prefix (above/left) and suffix (below/right) in the perpendicular direction
                var prefix = new System.Text.StringBuilder();
                var suffix = new System.Text.StringBuilder();

                if (horizontal)
                {
                    // We're generating horizontal words -> cross-check in vertical direction
                    for (int pr = r - 1; pr >= 0 && _cells[pr, c].IsOccupied; pr--)
                        prefix.Insert(0, _cells[pr, c].Letter);
                    for (int sr = r + 1; sr < Size && _cells[sr, c].IsOccupied; sr++)
                        suffix.Append(_cells[sr, c].Letter);
                }
                else
                {
                    // We're generating vertical words -> cross-check in horizontal direction
                    for (int pc = c - 1; pc >= 0 && _cells[r, pc].IsOccupied; pc--)
                        prefix.Insert(0, _cells[r, pc].Letter);
                    for (int sc = c + 1; sc < Size && _cells[r, sc].IsOccupied; sc++)
                        suffix.Append(_cells[r, sc].Letter);
                }

                if (prefix.Length == 0 && suffix.Length == 0)
                {
                    result[r, c] = 0x3FFFFFF; // unconstrained
                    continue;
                }

                int mask = 0;
                for (int li = 0; li < 26; li++)
                {
                    char letter = (char)('A' + li);
                    string candidate = prefix + letter.ToString() + suffix;
                    if (dawg.Contains(candidate))
                        mask |= (1 << li);
                }
                result[r, c] = mask;
            }
            return result;
        }

        // -- Cross-sums (for fast perpendicular scoring) -----------------------

        /// <summary>
        /// For each empty square, compute the total tile-value of tiles directly
        /// above+below (horizontal mode) or left+right (vertical mode).
        /// </summary>
        public int[,] ComputeCrossSums(bool horizontal)
        {
            var result = new int[Size, Size];
            for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (_cells[r, c].IsOccupied) continue;
                int sum = 0;
                if (horizontal)
                {
                    for (int pr = r - 1; pr >= 0 && _cells[pr, c].IsOccupied; pr--)
                        sum += _cells[pr, c].IsBlank ? 0 : TileValues.Of(_cells[pr, c].Letter);
                    for (int sr = r + 1; sr < Size && _cells[sr, c].IsOccupied; sr++)
                        sum += _cells[sr, c].IsBlank ? 0 : TileValues.Of(_cells[sr, c].Letter);
                }
                else
                {
                    for (int pc = c - 1; pc >= 0 && _cells[r, pc].IsOccupied; pc--)
                        sum += _cells[r, pc].IsBlank ? 0 : TileValues.Of(_cells[r, pc].Letter);
                    for (int sc = c + 1; sc < Size && _cells[r, sc].IsOccupied; sc++)
                        sum += _cells[r, sc].IsBlank ? 0 : TileValues.Of(_cells[r, sc].Letter);
                }
                result[r, c] = sum;
            }
            return result;
        }

        // -- Anchor squares ----------------------------------------------------

        /// <summary>
        /// Returns all empty squares that are adjacent to at least one occupied square
        /// (or the center square if the board is empty).
        /// </summary>
        public List<(int Row, int Col)> GetAnchors()
        {
            var anchors = new List<(int, int)>();
            bool anyOccupied = false;

            for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (_cells[r, c].IsOccupied) { anyOccupied = true; continue; }
                if (HasOccupiedNeighbour(r, c))
                    anchors.Add((r, c));
            }

            if (!anyOccupied)
                anchors.Add((Center, Center));

            return anchors;
        }

        private bool HasOccupiedNeighbour(int r, int c)
        {
            if (r > 0      && _cells[r - 1, c].IsOccupied) return true;
            if (r < Size-1 && _cells[r + 1, c].IsOccupied) return true;
            if (c > 0      && _cells[r, c - 1].IsOccupied) return true;
            if (c < Size-1 && _cells[r, c + 1].IsOccupied) return true;
            return false;
        }
    }
}
