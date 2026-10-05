// SkillLevel.cs
// Defines skill levels and the move-selection strategy for each.
//
// Easy   -- plays a randomly chosen move from the bottom 60-80% of scored moves
//           favouring 3 letter words and falling back to 2 letter words
//           (or passes if no move scores above 0).
// Medium -- plays a randomly chosen move from the top 25-50% of scored moves.
// Tricky -- plays a randomly chosen move from the top 10-20% of scored moves.
// Hard   -- always plays the highest-scoring move (pure greedy, Appel & Jacobson style).
// Expert -- highest-scoring move PLUS a strategic rack-management bonus that rewards
//           retaining high-synergy tiles and avoids opening triple-word lines.

// for Easy -> Tricky could perhaps make the choice of word depend
// on the number of points e.g. based on the top scoring word choose a word between 25-50% off that?
// wonder how much difference that might make. probably quite a lot esp if there was a particularly
// high scoring word in the #1 slot

using Scrabble.Core.Squares;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace Scrabble.Core.AI
{
    public enum SkillLevel
    {
        Easy   = 0,
        Medium = 1,
        Hard   = 2,
        Expert = 3,
        Tricky = 4
    }

    // Enum used to display choices to user when selecting computer opposition
    // note the differences in numeric values between this Enum and the SkillLevel Enum above
    // because can't change the numbering in the SkillLevel enum without "breaking" in-flight
    // and historical games
    public enum ProficiencyName
    {
        Beginner = 0,
        Intermediate = 1,
        Experienced = 2,
        Advanced = 3,
        Expert = 4
    }

    // since ProficiencyName enum won't display the names in the order they appear in the enum
    // declaration but instead are displayed according to integer value of the entry,
    // this mapping is required to convert gui value to server side skill level
    public static class SkillNameMap
    {
        public static int toSkillLevel(ProficiencyName pn)
        {
            return pn switch
            {
                ProficiencyName.Beginner => (int)SkillLevel.Easy,
                ProficiencyName.Intermediate => (int)SkillLevel.Medium,
                ProficiencyName.Experienced => (int)SkillLevel.Tricky,
                ProficiencyName.Advanced => (int)SkillLevel.Hard,
                ProficiencyName.Expert => (int)SkillLevel.Expert,
                _ => 0
            };
        }

        public static ProficiencyName toProficiencyName(SkillLevel sl)
        {
            return sl switch
            {
                SkillLevel.Easy => ProficiencyName.Beginner,
                SkillLevel.Medium => ProficiencyName.Intermediate,
                SkillLevel.Tricky => ProficiencyName.Experienced,
                SkillLevel.Hard => ProficiencyName.Advanced,
                SkillLevel.Expert => ProficiencyName.Expert,
                _ => 0
            };
        }
    }

    public static class MoveSelector
    {
        private static readonly Random _rng = new();

        // ---- Public API ----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Given a sorted (descending) list of legal moves, pick one according
        /// to the requested skill level.  Returns null if the list is empty.
        /// </summary>
        public static ScrabbleMove? Select(
            List<ScrabbleMove> moves,
            SkillLevel skill,
            AiBoard board,
            Rack rack)
        {
            // check if at least one move can be made
            if (moves.Count == 0) return null;

            if (skill == SkillLevel.Hard) return moves[0];
            if (skill == SkillLevel.Expert) return SelectExpert(moves, board, rack);

            // for any other skill level, we will randomly select a move from
            // a subset of the list of possible moves

            // split the list into 2 letter words and 3+ letter words
            List<ScrabbleMove> twos = new List<ScrabbleMove>();
            List<ScrabbleMove> threeplus = new List<ScrabbleMove>();
            foreach (var move in moves)
            {
                if (move.Word.Length == 2)
                    twos.Add(move);
                else
                    threeplus.Add(move);
            }

            // grab some stats for the move lists (for deciding on a better word selection strategy)
            double avgScoreAll = 0;
            int minScoreAll = 0;
            int maxScoreAll = 0;
            double avgScore2LW = 0;
            int minScore2LW = 0;
            int maxScore2LW = 0;
            double avgScore3PLW = 0;
            int minScore3PLW = 0;
            int maxScore3PLW = 0;

            if (moves.Count > 0)
            {
                avgScoreAll = moves.Average(m => m.Score);
                minScoreAll = moves.Min(m => m.Score);
                maxScoreAll = moves.Max(m => m.Score);
            }
            if (twos.Count > 0)
            {
                avgScore2LW = twos.Average(m => m.Score);
                minScore2LW = twos.Min(m => m.Score);
                maxScore2LW = twos.Max(m => m.Score);
            }
            if (threeplus.Count > 0)
            {
                avgScore3PLW = threeplus.Average(m => m.Score);
                minScore3PLW = threeplus.Min(m => m.Score);
                maxScore3PLW = threeplus.Max(m => m.Score);
            }

            int i = 0;
            /*
            Console.WriteLine("-- ALL --");
            foreach (var move in moves)
            {
                i++;
                Console.WriteLine(i + ". " + move.PrintMe());
            }
            */
            Console.WriteLine("-- 2LW --");
            i = 0;
            foreach (var move in twos)
            {
                i++;
                Console.WriteLine(i + ". " + move.PrintMe());
            }
            Console.WriteLine("-- 3+LW --");
            i = 0;
            foreach (var move in threeplus)
            {
                i++;
                Console.WriteLine(i + ". " + move.PrintMe());
            }

            Console.WriteLine("Available move count is " + moves.Count +
                                " [" + minScoreAll + " - " + maxScoreAll + ", " + Math.Round(avgScoreAll, 2) + "]");
            Console.WriteLine(" 2 letter words : " + twos.Count + 
                                " [" + minScore2LW + " - " + maxScore2LW + ", " + Math.Round(avgScore2LW, 2) + "]");
            Console.WriteLine(" 3+ letter words : " + threeplus.Count +
                                " [" + minScore3PLW + " - " + maxScore3PLW + ", " + Math.Round(avgScore3PLW, 2) + "]");

            // set up the primary and fallback lists
            List<ScrabbleMove> primary = threeplus;
            List<ScrabbleMove> fallback = twos;

            // if the threeplus list is too small then revert to using the
            // full list of moves and set the fallback list to empty
            // (because it's possible the small list of 3+LW has weak/low
            // scoring options)
            if (threeplus.Count < 10)
            {
                Console.WriteLine("Insufficient 3+LW, revert to using full word list...");
                primary = moves;
                fallback = new List<ScrabbleMove>();  // empty list
            }

            return skill switch
            {
                SkillLevel.Easy   => SelectEasy(primary, fallback),
                SkillLevel.Medium => SelectMedium(primary, fallback),
                SkillLevel.Tricky => SelectTricky(primary, fallback),
                 _                 => moves[0]
            };
        }

        // ---- Easy: random from 60-80% ----------------------------------------------------------------
        private static ScrabbleMove? SelectEasy(List<ScrabbleMove> primary, List<ScrabbleMove> fallback)
        {
            // initially try to select from 3+ letter words because using 2 letter words
            // means the board can get a bit congested. fall back to 2 letter words if
            // there are no 3 letter options

            // note that a 3 letter word may involve only placing 1 tile 
            // if it is being played against 2 already placed tiles
            // so maybe we need to check the TilePlacement count and encourage
            // the computer to play words that involve placing 2 tiles in order
            // to make a min 3+ letter word

            ScrabbleMove sm = PickMove(primary, 60, 80);
            if (sm is null)
                sm = PickMove(fallback, 60, 80);

            return sm;
        }

        // ---- Medium: random from 25-50% --------------------------------------------------------------------------
        private static ScrabbleMove? SelectMedium(List<ScrabbleMove> primary, List<ScrabbleMove> fallback)
        {
            ScrabbleMove sm = PickMove(primary, 25, 50);
            if (sm is null)
                sm = PickMove(fallback, 25, 50);

            return sm;
        }

        // ---- Tricky : random from 10-20% --------------------------------------------------------------------------
        private static ScrabbleMove? SelectTricky(List<ScrabbleMove> primary, List<ScrabbleMove> fallback)
        {
            // 'Hard' mode selects the top move suggested (but without the strategic adjustments
            // that happen in 'Expert') - both are pretty difficult to play against (at least for me).
            // 'Tricky' mode "softens" 'Hard' mode by randomly selecting a move near, but not at, the top.
            // It provides a mode where there is a challenge and I might stand a chance, because I
            // can generally beat the computer fairly easily at 'Medium' level...

            // select a move in the top 10-20% of possible moves
            ScrabbleMove sm = PickMove(primary, 10, 20);
            if (sm is null)
                sm = PickMove(fallback, 10, 20);

            return sm;
        }

        // ---- Expert: score + strategic adjustments --------------------------------------------------------
        private static ScrabbleMove? SelectExpert(List<ScrabbleMove> moves, AiBoard board, Rack rack)
        {
            // Evaluate the top N candidates with strategic adjustments.
            const int CandidatePool = 10;
            int poolSize = Math.Min(CandidatePool, moves.Count);

            ScrabbleMove? best = null;
            double bestScore   = double.MinValue;
            int bestScoreIndex = -1;

            for (int i = 0; i < poolSize; i++)
            {
                var move  = moves[i];

                double rbb = RackBalanceBonus(rack, move);
                double op = OpeningPenalty(board, move);
                double adj = move.Score + rbb - op;
//                           + RackBalanceBonus(rack, move)
//                           - OpeningPenalty(board, move);

                Console.WriteLine(move.PrintMe() + 
                                  " + RBB [" + Math.Round(rbb, 2) + "] - OP [" + Math.Round(op, 2) + "] -> " + Math.Round(adj, 2));

                if (adj > bestScore)
                {
                    bestScore = adj;
                    best      = move;
                    bestScoreIndex = i;
                }
            }

            if (bestScoreIndex != -1)
                Console.WriteLine("Expert level move #" + (bestScoreIndex + 1) + " -> " + best.PrintMe());

            return best ?? moves[0];
        }

        private static ScrabbleMove? PickMove(List<ScrabbleMove> moves, int lb, int ub)
        {
            if (moves.Count == 0) return null;
            if (moves.Count == 1) return moves[0];
            if (moves.Count < 20) return moves[_rng.Next(moves.Count)];

            // start (of range) should be less than end and be between 0 and 100, but not checked
            // assuming sensible values passed
            // pick a move from within a range in the list of possible moves
            int nmoves = moves.Count;
            int startpos = nmoves * lb / 100;
            int endpos = nmoves * ub / 100;
            Console.WriteLine("#" + nmoves + ", s=" + startpos + ", e=" + endpos);
            if (startpos >= endpos)
                return moves[startpos];
            int selected_entry = _rng.Next(startpos, endpos);
            Console.WriteLine("PickMove #" + (selected_entry + 1) + " -> " + moves[selected_entry].PrintMe());
            return moves[selected_entry];
        }

        // ---- Rack-balance heuristic ----------------------------------------------------------------------------------------
        // Reward moves that leave a balanced, vowel/consonant-mixed rack.
        // Penalise keeping duplicate high-point tiles (Q, Z, X, J without U).

        private static double RackBalanceBonus(Rack rack, ScrabbleMove move)
        {
            // Simulate which tiles remain after the move
            var remainingRack = rack.Clone();
            foreach (var p in move.Placements)
            {
                if (p.IsBlank) remainingRack.ReturnBlank(); // we "give back" conceptually - just count
                else remainingRack.Return(p.Letter);        // not really needed; below just counts remaining
            }

            // Count vowels vs consonants in remaining rack
            int vowels = 0, consonants = 0;
            foreach (char c in remainingRack.Letters())
            {
                if ("AEIOU".Contains(c)) vowels++;
                else if (c != '?')        consonants++;
            }

            int total = vowels + consonants;
            if (total == 0) return 0;

            // Ideal: 40--60 % vowels
            double vowelRatio = (double)vowels / total;
            double balance    = 1.0 - Math.Abs(vowelRatio - 0.45) * 4; // −1..+1
            return balance * 3.0; // up to ±3 point adjustment
        }

        // ---- Board-opening penalty ------------------------------------------------------------------------------------------
        // Penalise moves whose placements are adjacent to triple-word squares,
        // since that hands the opponent a huge opportunity.

        private static double OpeningPenalty(AiBoard board, ScrabbleMove move)
        {
            double penalty = 0;
            foreach (var p in move.Placements)
            {
                // Check all four neighbours
                foreach (var (dr, dc) in new[]{(-1,0),(1,0),(0,-1),(0,1)})
                {
                    int nr = p.Row + dr, nc = p.Col + dc;
                    if (!board.InBounds(nr, nc)) continue;
                    var prem = board[nr, nc].Premium;
                    if (!board[nr, nc].IsOccupied && prem == Premium.TripleWord)
                        penalty += 8;  // big penalty for each exposed TW
                    else if (!board[nr, nc].IsOccupied && prem == Premium.DoubleWord)
                        penalty += 3;
                }
            }
            return penalty;
        }
    }
}
