using System;
using System.Collections.Generic;
using System.Linq;

namespace FindItemsForQuotaBepin5
{
    internal class Przydatek
    {
        public static List<int> PrzydatekFast(List<int> a, int B)
        {
            int size = a.Count();
            if (size == 0) return null;
            Random r = new(Guid.NewGuid().GetHashCode());
            var x_best = new List<int>(new int[size]);
            var delta_best = B;
            for (int trial = 0; trial < 40; ++trial)
            {
                var delta_curr = B;
                var x = new List<int>(new int[size]);
                foreach (int i in Enumerable.Range(0, size).OrderBy(y => r.Next()))
                {
                    if (a[i] <= delta_curr)
                    {
                        x[i] = 1;
                        delta_curr -= a[i];
                    }
                }
                var indices = x.Select((value, index) => value == 1 ? index : -1)
                            .Where(index => index != -1).ToList();
                foreach (int i in Enumerable.Range(0, indices.Count()).OrderBy(y => r.Next()))
                {
                    var index = indices[i];
                    if (delta_curr == 0) break;
                    var T = a.Select((al, l) => x[l] == 0 && 0 < al - a[index] && al - a[index] <= delta_curr ? (al, l) : (0, -1))
                             .Where(valueIndexPair => valueIndexPair.Item2 != -1);
                    if (T.Any())
                    {
                        // Could be made O(1) by sorting the list prior to starting, unnecessary
                        // for small List.Count() (such as lethal companies <=200)
                        var k = MaxIndex(T);
                        delta_curr -= a[k] - a[index];
                        x[k] = 1;
                        x[index] = 0;
                    }
                }
                if (delta_curr < delta_best)
                {
                    x_best = x;
                    delta_best = delta_curr;
                }
                if (delta_best == 0) break;
            }
            return x_best;
        }

        private static int MaxIndex(IEnumerable<(int, int)> T)
        {
            int maxValue = int.MinValue;
            int maxIndex = -1;
            foreach (var valueIndexPair in T)
            {
                int value = valueIndexPair.Item1;
                int index = valueIndexPair.Item2;
                if (value > maxValue) { maxValue = value; maxIndex = index; }
            }
            return maxIndex;
        }
    }
}
