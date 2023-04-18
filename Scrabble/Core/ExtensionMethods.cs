using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scrabble.Core
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        public static List<T> Clone<T>(this IEnumerable<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            var newList = new List<T>();
            foreach (var item in list)
            {
                newList.Add(item);
            }

            return newList;
        }
    }

    internal class ThreadSafeRandom
    {
        private static readonly Random _global = new Random();
        private static readonly ThreadLocal<Random> _local = new ThreadLocal<Random>(() =>
        {
            int seed;
            lock (_global)
            {
                seed = _global.Next();
            }
            return new Random(seed);
        });

        public static int Next(int maxValue)
        {
            return _local.Value.Next(maxValue);
        }
    }


}
