using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// In-place Fisher–Yates that draws exclusively from <see cref="IDeterministicRandom"/>.
    /// </summary>
    public static class DeterministicShuffle
    {
        public static void FisherYates<T>(IList<T> items, IDeterministicRandom rng)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                var tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
