using System;
using System.Linq;
using NUnit.Framework;

namespace TwinkleOps.Tests
{
    public sealed class RaidTests
    {
        sealed class MemoryStore : IProgressStore
        {
            public ProgressData saved=new();
            public bool fail;
            public ProgressData Load()=>saved.Copy();
            public void Save(ProgressData data) { if(fail) throw new InvalidOperationException("disk full"); saved=data.Copy(); }
        }
        [Test] public void FullBagRejectsWithoutDroppingExistingLoot()
        { var bag=new Inventory(1,1); Assert.IsTrue(bag.Add("watch")); Assert.IsFalse(bag.Add("phone")); Assert.AreEqual(12500,bag.TotalValue); }
        [Test] public void ItemRotatesToFitNarrowSpace()
        { var bag=new Inventory(1,2); Assert.IsTrue(bag.Add("tools")); Assert.IsTrue(bag.Entries[0].rotated); }
        [Test] public void OverlapAndOutOfBoundsMovesAreRejected()
        { var bag=new Inventory(2,1); bag.Add("watch","a"); bag.Add("phone","b"); Assert.IsFalse(bag.Move("b",0,0,false)); Assert.IsFalse(bag.Move("b",2,0,false)); Assert.AreEqual(1,bag.Entries[1].x); }
        [Test] public void DuplicatePickupIdCannotDuplicateLoot()
        { var bag=new Inventory(); Assert.IsTrue(bag.Add("gold","one")); Assert.IsFalse(bag.Add("gold","one")); Assert.AreEqual(1,bag.Entries.Count); }
        [Test] public void SortPreservesAllInstanceIdsAndValue()
        { var bag=new Inventory(); foreach(var item in Items.All) Assert.IsTrue(bag.Add(item.id)); int value=bag.TotalValue; var ids=bag.Entries.Select(x=>x.instanceId).ToArray(); bag.Sort(); CollectionAssert.AreEquivalent(ids,bag.Entries.Select(x=>x.instanceId)); Assert.AreEqual(value,bag.TotalValue); }
        [Test] public void RarityDistributionHasExactlyDeclaredWeights()
        { var counts=new int[6]; for(int i=0;i<1000;i++) counts[(int)LootTable.RollRarity(i)]++; CollectionAssert.AreEqual(new[]{550,250,120,55,20,5},counts); }
        [Test] public void SeededLootCanBeReproduced()
        { CollectionAssert.AreEqual(LootTable.Roll(new Random(17),100),LootTable.Roll(new Random(17),100)); }
        [Test] public void EveryTierHasValidItems()
        { for(int i=0;i<6;i++) Assert.IsTrue(Items.All.Any(x=>(int)x.rarity==i && x.value>0 && x.width>0 && x.height>0)); }
        [Test] public void ExtractStoresLootAndCannotCommitTwice()
        { var store=new MemoryStore(); var raid=new RaidSession(store); raid.Begin(); raid.Bag.Add("sample"); Assert.IsTrue(raid.Extract()); Assert.IsFalse(raid.Extract()); CollectionAssert.AreEqual(new[]{"sample"},new RaidSession(store).Progress.stash); Assert.AreEqual(1,store.saved.successfulRaids); }
        [Test] public void DeathOnlyLosesCurrentRaidLoot()
        { var store=new MemoryStore(); store.saved.stash.Add("watch"); var raid=new RaidSession(store); raid.Begin(); raid.Bag.Add("sample"); raid.Die(); CollectionAssert.AreEqual(new[]{"watch"},store.saved.stash); Assert.IsEmpty(raid.Bag.Entries); }
        [Test] public void InterruptedRaidBecomesLossOnNextLaunch()
        { var store=new MemoryStore(); var raid=new RaidSession(store); raid.Begin(); raid.Bag.Add("core"); var reload=new RaidSession(store); Assert.IsFalse(reload.InRaid); Assert.AreEqual(1,reload.Progress.deaths); Assert.IsEmpty(reload.Progress.stash); }
        [Test] public void FailedExtractionSaveKeepsRaidAndBagForRetry()
        { var store=new MemoryStore(); var raid=new RaidSession(store); raid.Begin(); raid.Bag.Add("core"); store.fail=true; Assert.Throws<InvalidOperationException>(()=>raid.Extract()); Assert.IsTrue(raid.InRaid); Assert.AreEqual(1,raid.Bag.Entries.Count); store.fail=false; Assert.IsTrue(raid.Extract()); Assert.AreEqual(1,store.saved.stash.Count); }
        [Test] public void FailedBeginDoesNotCreateRaid()
        { var store=new MemoryStore(); var raid=new RaidSession(store); store.fail=true; Assert.Throws<InvalidOperationException>(()=>raid.Begin()); Assert.IsFalse(raid.InRaid); }
        [Test] public void BeginCannotReplaceAnActiveRaid()
        { var raid=new RaidSession(new MemoryStore()); raid.Begin(); string id=raid.Progress.activeRaid; Assert.Throws<InvalidOperationException>(()=>raid.Begin()); Assert.AreEqual(id,raid.Progress.activeRaid); }
        [Test] public void InvalidRollAndUnknownItemAreRejected()
        { Assert.Throws<ArgumentOutOfRangeException>(()=>LootTable.RollRarity(1000)); Assert.Throws<ArgumentException>(()=>new Inventory().Add("missing")); }
    }
}
