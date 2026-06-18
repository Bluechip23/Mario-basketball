using System.Linq;
using NUnit.Framework;
using MarioBasketball.Characters;
using MarioBasketball.Gameplay;
using MarioBasketball.UI;

namespace MarioBasketball.Tests
{
    /// <summary>
    /// Fast EditMode unit tests over the pure game logic (no scene/GameObjects).
    /// Their real job in CI is twofold: prove the whole project still compiles,
    /// and lock down a few rules that are easy to break by accident.
    /// </summary>
    public class LogicTests
    {
        [Test]
        public void Roster_HasExpectedSizeAndCharacters()
        {
            var names = CharacterLibrary.All().Select(c => c.characterName).ToList();
            Assert.AreEqual(23, names.Count, "Roster size changed — update this test if intended.");
            CollectionAssert.Contains(names, "Mario");
            CollectionAssert.Contains(names, "Boo");
            CollectionAssert.Contains(names, "Qui-gon");
            CollectionAssert.DoesNotContain(names, "Cliffy Guy"); // renamed to Qui-gon
            Assert.AreEqual(names.Count, names.Distinct().Count(), "Duplicate character names.");
        }

        [Test]
        public void Boo_IsWideOpenSniper()
        {
            var boo = CharacterLibrary.All().First(c => c.characterName == "Boo");
            Assert.AreEqual(HiddenTrait.WideOpenSniper, boo.hiddenTrait);
        }

        [Test]
        public void CreatePlayer_CostTiersRiseAsAStatClimbs()
        {
            Assert.AreEqual(1, CreatePlayerMenu.CostToReach(3));
            Assert.AreEqual(2, CreatePlayerMenu.CostToReach(5));
            Assert.AreEqual(3, CreatePlayerMenu.CostToReach(8));
            Assert.AreEqual(4, CreatePlayerMenu.CostToReach(9));
            Assert.AreEqual(5, CreatePlayerMenu.CostToReach(10));
        }

        [Test]
        public void ShotMath_BaseFromStat_HitsItsEndpoints()
        {
            Assert.AreEqual(ShotMath.BaseMin, ShotMath.BaseFromStat(1f), 1e-4f);
            Assert.AreEqual(ShotMath.BaseMax, ShotMath.BaseFromStat(10f), 1e-4f);
            Assert.Greater(ShotMath.BaseFromStat(7f), ShotMath.BaseFromStat(4f));
        }

        [Test]
        public void ShotMath_MakeChanceFromQuality_StaysClamped()
        {
            Assert.LessOrEqual(ShotMath.MakeChanceFromQuality(100f, onFire: true), ShotMath.MaxChance);
            Assert.GreaterOrEqual(ShotMath.MakeChanceFromQuality(-100f, onFire: false), ShotMath.MinChance);
        }
    }
}
