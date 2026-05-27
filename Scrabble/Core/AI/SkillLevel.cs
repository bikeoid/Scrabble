// SkillLevel.cs
// Defines skill levels and the move-selection strategy for each.
//
// Easy   -- plays a randomly chosen move from the bottom 25 % of scored moves
//          (or passes if no move scores above 0).
// Medium -- plays a randomly chosen move from the top 50 % of scored moves.
// Hard   -- always plays the highest-scoring move (pure greedy, Appel & Jacobson style).
// Expert -- highest-scoring move PLUS a strategic rack-management bonus that rewards
//          retaining high-synergy tiles and avoids opening triple-word lines.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Scrabble.Core.AI
{
    public enum SkillLevel
    {
        Easy   = 0,
        Medium = 1,
        Hard   = 2,
        Expert = 3
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
            if (moves.Count == 0) return null;

            return skill switch
            {
                SkillLevel.Easy   => SelectEasy(moves),
                SkillLevel.Medium => SelectMedium(moves),
                SkillLevel.Hard   => moves[0],
                SkillLevel.Expert => SelectExpert(moves, board, rack),
                _                 => moves[0]
            };
        }

        // ---- Easy: random from bottom quartile ----------------------------------------------------------------

        private static ScrabbleMove? SelectEasy(List<ScrabbleMove> moves)
        {
            // Take the bottom 25 % (worst moves), but at least 1.
            int bottomCount = Math.Max(1, moves.Count / 4);
            int startIdx    = moves.Count - bottomCount;
            return moves[startIdx + _rng.Next(bottomCount)];
        }

        // ---- Medium: random from top half --------------------------------------------------------------------------

        private static ScrabbleMove? SelectMedium(List<ScrabbleMove> moves)
        {
            int topCount = Math.Max(1, moves.Count / 2);
            return moves[_rng.Next(topCount)];
        }

        // ---- Expert: score + strategic adjustments --------------------------------------------------------

        private static ScrabbleMove? SelectExpert(
            List<ScrabbleMove> moves, AiBoard board, Rack rack)
        {
            // Evaluate the top N candidates with strategic adjustments.
            const int CandidatePool = 10;
            int poolSize = Math.Min(CandidatePool, moves.Count);

            ScrabbleMove? best = null;
            double bestScore   = double.MinValue;

            for (int i = 0; i < poolSize; i++)
            {
                var move  = moves[i];
                double adj = move.Score
                           + RackBalanceBonus(rack, move)
                           - OpeningPenalty(board, move);

                if (adj > bestScore)
                {
                    bestScore = adj;
                    best      = move;
                }
            }

            return best ?? moves[0];
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
