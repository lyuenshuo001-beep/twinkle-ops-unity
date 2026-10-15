using System;using System.Linq;using NUnit.Framework;using UnityEngine;
namespace TwinkleOps.Tests {
public class V12SystemsTests {
 sealed class Store:IProgressStore{public ProgressData data=new();public bool fail;public ProgressData Load()=>data.Copy();public void Save(ProgressData d){if(fail)throw new InvalidOperationException("storage unavailable");data=d.Copy();}}
 [Test] public void SafeBoxSurvivesDeathAndInterruptedRaid(){foreach(bool restart in new[]{false,true}){var s=new Store();var r=new RaidSession(s);r.Begin();r.Bag.Add("core","c");r.Bag.Add("watch","w");Assert.IsTrue(r.Protect("c"));Assert.IsFalse(r.Protect("w"));if(restart)r=new RaidSession(s);else r.Die();CollectionAssert.AreEqual(new[]{"core"},r.Progress.stash);Assert.AreEqual(1,r.Progress.deaths);}}
 [Test] public void FailedSafeSaveCannotDuplicateOrLoseItems(){var s=new Store();var r=new RaidSession(s);r.Begin();r.Bag.Add("gold","g");s.fail=true;Assert.Throws<InvalidOperationException>(()=>r.Protect("g"));Assert.AreEqual(1,r.Bag.Entries.Count);Assert.IsEmpty(r.Safe.Entries);s.fail=false;r.Protect("g");s.fail=true;Assert.Throws<InvalidOperationException>(()=>r.Unprotect("g"));Assert.IsEmpty(r.Bag.Entries);Assert.AreEqual(1,r.Safe.Entries.Count);}
 [Test] public void SellingAndBuyingPersistAndFailedSaveIsAtomic(){var s=new Store();s.data.stash.Add("gold");var r=new RaidSession(s);Assert.IsTrue(r.Sell(0));Assert.AreEqual(73000,r.Progress.credits);Assert.IsTrue(r.BuyMedicine("medicine"));var reload=new RaidSession(s);Assert.AreEqual(70800,reload.Progress.credits);CollectionAssert.AreEqual(new[]{"medicine"},reload.Progress.stash);s.fail=true;Assert.Throws<InvalidOperationException>(()=>reload.Sell(0));Assert.AreEqual(70800,reload.Progress.credits);Assert.AreEqual(1,reload.Progress.stash.Count);}
 [Test] public void PreparedSuppliesPersistUntilRaidAndAreLostOnDeath(){var s=new Store();var r=new RaidSession(s);r.BuyMedicine("medicine");r.Pack(0);r=new RaidSession(s);Assert.AreEqual(1,r.Progress.prepared.Count);r.Begin();Assert.AreEqual("medicine",r.Bag.Entries.Single().itemId);Assert.IsEmpty(r.Progress.prepared);r.Die();Assert.IsEmpty(r.Progress.stash);Assert.AreEqual(1100,r.Progress.lastLostValue);}
 [Test] public void SearchIsSequentialAndCannotRerollOrTakeUnknown(){var o=new GameObject();try{var c=o.AddComponent<LootContainer>();c.Reveal();var initial=c.Contents.ToArray();var bag=new Inventory();Assert.IsFalse(c.Take(0,bag));c.TickSearch(.1f);float progress=c.SearchProgress;c.Reveal();Assert.AreEqual(progress,c.SearchProgress);CollectionAssert.AreEqual(initial,c.Contents);Assert.IsTrue(c.TickSearch(10));Assert.AreEqual(1,c.RevealedCount);Assert.IsFalse(c.Take(1,bag));Assert.IsTrue(c.Take(0,bag));Assert.AreEqual(0,c.RevealedCount);Assert.AreEqual(initial[0],bag.Entries.Single().itemId);}finally{UnityEngine.Object.DestroyImmediate(o);}}
 [Test] public void SafeAndBagExtractionCountsValueOnce(){var r=new RaidSession(new Store());r.Begin();r.Bag.Add("sample","s");r.Bag.Add("watch","w");r.Protect("s");Assert.IsTrue(r.Extract());Assert.AreEqual(162500,r.Progress.lastRaidValue);Assert.AreEqual(2,r.Progress.stash.Count);Assert.IsFalse(r.Extract());}
 [Serializable] class LegacyEnvelope {public long revision=1;public string payload,checksum;}
 [Test] public void LegacyJournalMigratesWithoutResettingStash(){
  const string key="TwinkleOps.v12.migration.qa.";PlayerPrefs.DeleteKey(key+0);PlayerPrefs.DeleteKey(key+1);
  try{
   var e=new LegacyEnvelope{payload="{\"version\":1,\"successfulRaids\":3,\"deaths\":2,\"activeRaid\":\"\",\"stash\":[\"gold\",\"watch\"]}"};
   using(var sha=System.Security.Cryptography.SHA256.Create())e.checksum=Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(e.revision+":"+e.payload)));
   PlayerPrefs.SetString(key+1,JsonUtility.ToJson(e));PlayerPrefs.Save();var r=new RaidSession(new PlayerPrefsStore(key));
   Assert.AreEqual(3,r.Progress.successfulRaids);Assert.AreEqual(2,r.Progress.deaths);CollectionAssert.AreEqual(new[]{"gold","watch"},r.Progress.stash);Assert.AreEqual(25000,r.Progress.credits);
   Assert.IsTrue(r.Sell(0));var reload=new RaidSession(new PlayerPrefsStore(key));Assert.AreEqual(73000,reload.Progress.credits);CollectionAssert.AreEqual(new[]{"watch"},reload.Progress.stash);
  }finally{PlayerPrefs.DeleteKey(key+0);PlayerPrefs.DeleteKey(key+1);PlayerPrefs.Save();}
 }

}}
