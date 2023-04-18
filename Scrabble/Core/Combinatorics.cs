using System.Collections.Generic;

namespace Combinatorics
{
    public class CombinationGenerator
    {
        private static Dictionary<int, long> factorials = new Dictionary<int, long>
        {
            {0, 1L }, {1, 1L}, {2, 2L}, {3, 6L}, {4, 24L}, {5, 120L},
            {6, 720L}, {7, 5040L}, {8, 40320L}, {9, 362880L}, {10, 3628800L},
            {11, 39916800L}, {12, 479001600L}, {13, 6227020800L},
            {14, 87178291200L}, {15, 1307674368000L}
        };

        private readonly long totalCombinations = 0;
        private long combinationsLeft = 0;
        private int[] data;
        private readonly int choose;
        private readonly int domain;

        public CombinationGenerator(int domain, int choose)
        {
            this.domain = domain;
            this.choose = choose;

            totalCombinations = factorials[domain] / (factorials[choose] * factorials[domain - choose]);
            combinationsLeft = totalCombinations;
            data = new int[choose];
            for (int i=0; i < choose; i++)
            {
                data[i] = i;
            }
        }

        public bool HasNext()
        {
            return combinationsLeft > 0;
        }

        public int[] GetNext()
        {
            if (combinationsLeft == totalCombinations)
            {
                combinationsLeft--;
            } else
            {
                var i = choose - 1;
                while (data[i] == (domain - choose + i))
                {
                    i--;
                }
                data[i]++;
                for (int j=i+1; j < choose; j++)
                {
                    data[j] = data[i] + j - i;
                }
                combinationsLeft--;
            }
            return data;
        }
    }

}

