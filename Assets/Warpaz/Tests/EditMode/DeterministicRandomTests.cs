using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class DeterministicRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            CollectionAssert.AreEqual(Sequence(42UL), Sequence(42UL));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            CollectionAssert.AreNotEqual(Sequence(1UL), Sequence(2UL));
        }

        [Test]
        public void SameDeckAndSeed_ProduceSameShuffleOrder()
        {
            var a = ShuffleDeck(9UL);
            var b = ShuffleDeck(9UL);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, ShuffleDeck(10UL));
        }

        [Test]
        public void SaveAndRestore_PreservesSequence()
        {
            var rng = new SeededRng(77UL);
            for (var i = 0; i < 5; i++)
            {
                rng.Next(0, 1000);
            }

            var snapshot = rng.Save();
            Assert.AreEqual(77UL, snapshot.Seed);

            var continued = new int[8];
            for (var i = 0; i < continued.Length; i++)
            {
                continued[i] = rng.Next(0, 1000);
            }

            rng.Restore(snapshot);
            var restored = new int[8];
            for (var i = 0; i < restored.Length; i++)
            {
                restored[i] = rng.Next(0, 1000);
            }

            CollectionAssert.AreEqual(continued, restored);

            var clone = new SeededRng(snapshot);
            var fromClone = new int[8];
            for (var i = 0; i < fromClone.Length; i++)
            {
                fromClone[i] = clone.Next(0, 1000);
            }

            CollectionAssert.AreEqual(continued, fromClone);
        }

        [Test]
        public void Shuffle_UsesOnlyIDeterministicRandom()
        {
            IDeterministicRandom rng = new SeededRng(3UL);
            var items = new[] { 1, 2, 3, 4, 5 };
            DeterministicShuffle.FisherYates(items, rng);

            var copy = new[] { 1, 2, 3, 4, 5 };
            DeterministicShuffle.FisherYates(copy, new SeededRng(3UL));
            CollectionAssert.AreEqual(copy, items);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, items);
        }

        private static int[] Sequence(ulong seed)
        {
            IDeterministicRandom rng = new SeededRng(seed);
            var values = new int[16];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = rng.Next(0, 1000);
            }

            return values;
        }

        private static List<int> ShuffleDeck(ulong seed)
        {
            var deck = new List<int>(40);
            for (var i = 1; i <= 40; i++)
            {
                deck.Add(i);
            }

            DeterministicShuffle.FisherYates(deck, new SeededRng(seed));
            return deck;
        }
    }
}
