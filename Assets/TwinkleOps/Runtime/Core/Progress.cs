using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace TwinkleOps
{
    [Serializable] public sealed class ProgressData
    {
        public int version=1, successfulRaids, deaths;
        public string activeRaid="";
        public int economyVersion, lastRaidValue, lastLostValue; public long credits;
        public bool lastSuccess;
        public List<string> secured=new(),prepared=new();
        public List<string> stash=new();
        public ProgressData Copy() => new() { version=version, successfulRaids=successfulRaids, deaths=deaths, activeRaid=activeRaid, stash=new(stash), secured=new(secured??new()),prepared=new(prepared??new()), economyVersion=economyVersion, credits=credits,lastRaidValue=lastRaidValue,lastLostValue=lastLostValue,lastSuccess=lastSuccess };
    }
    public interface IProgressStore { ProgressData Load(); void Save(ProgressData data); }

    // Two independently checksummed slots: a damaged latest write can fall back to the previous snapshot.
    public sealed class PlayerPrefsStore : IProgressStore
    {
        readonly string Prefix;
        public PlayerPrefsStore(string prefix="TwinkleOps.progress.v1.") { Prefix=prefix; }
        [Serializable] sealed class Envelope { public long revision; public string payload, checksum; }
        long revision;
        static string Hash(string value)
        { using var sha=SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))); }
        public ProgressData Load()
        {
            ProgressData best=null; bool any=false;
            for(int i=0;i<2;i++)
            {
                string raw=PlayerPrefs.GetString(Prefix+i,""); if(raw.Length==0) continue; any=true;
                try
                {
                    var e=JsonUtility.FromJson<Envelope>(raw);
                    if(e==null || e.revision<=revision || e.checksum!=Hash(e.revision+":"+e.payload)) continue;
                    var candidate=JsonUtility.FromJson<ProgressData>(e.payload);
                    if(candidate==null || candidate.version!=1 || candidate.stash==null || candidate.activeRaid==null) continue;
                    foreach(var id in candidate.stash) _=Items.Get(id);
                    candidate.prepared??=new();var packCheck=new Inventory();foreach(var id in candidate.prepared)if(!packCheck.Add(id))throw new InvalidOperationException("Invalid prepared loadout");
                    candidate.secured??=new();foreach(var id in candidate.secured)_=Items.Get(id);
                    var secureCheck=new Inventory(2,2);foreach(var id in candidate.secured)if(!secureCheck.Add(id))throw new InvalidOperationException("Invalid safe box");
                    best=candidate; revision=e.revision;
                } catch(Exception) { /* Try the other journal slot; never overwrite unreadable saves. */ }
            }
            if(best==null && any) throw new InvalidOperationException("存档校验失败。请保留存档并联系开发者；未创建新存档覆盖原数据。");
            return best ?? new ProgressData();
        }
        public void Save(ProgressData data)
        {
            long next=revision+1;
            var e=new Envelope { revision=next, payload=JsonUtility.ToJson(data) };
            e.checksum=Hash(next+":"+e.payload);
            PlayerPrefs.SetString(Prefix+(next%2),JsonUtility.ToJson(e)); PlayerPrefs.Save(); revision=next;
        }
    }

    public sealed class RaidSession
    {
        readonly IProgressStore store;
        public ProgressData Progress { get; private set; }
        public Inventory Bag { get; } = new();
        public Inventory Safe { get; }=new(2,2);
        public bool InRaid => !string.IsNullOrEmpty(Progress.activeRaid);
        public RaidSession(IProgressStore store)
        {
            this.store=store; Progress=store.Load();Progress.secured??=new();Progress.prepared??=new();
            if(Progress.economyVersion==0){Progress=Progress.Copy();Progress.economyVersion=1;Progress.credits=25000;}
            // A closed/crashed raid is a loss. Stash remains intact.
            if(InRaid) Die();
        }
        public void Begin()
        {
            if(InRaid) throw new InvalidOperationException("A raid is already active");
            var packed=Progress.prepared.ToArray();var next=Progress.Copy();next.prepared.Clear(); next.activeRaid=Guid.NewGuid().ToString("N"); Commit(next);Bag.Clear();foreach(var id in packed)Bag.Add(id); Safe.Clear();
        }
        public bool Extract()
        {
            if(!InRaid) return false;
            var next=Progress.Copy(); next.stash.AddRange(Bag.Entries.Select(x=>x.itemId));next.stash.AddRange(next.secured);next.lastRaidValue=Bag.TotalValue+Safe.TotalValue;next.lastLostValue=0;next.lastSuccess=true;next.secured.Clear();
            next.activeRaid=""; next.successfulRaids++; Commit(next); Bag.Clear(); Safe.Clear(); return true;
        }
        public bool Die()
        {
            if(!InRaid) return false;
            var next=Progress.Copy(); next.stash.AddRange(next.secured);next.lastRaidValue=next.secured.Sum(id=>Items.Get(id).value);next.lastLostValue=Bag.TotalValue;next.lastSuccess=false;next.secured.Clear();next.activeRaid=""; next.deaths++; Commit(next); Bag.Clear(); Safe.Clear(); return true;
        }
        public bool Protect(string instanceId)
        {
            if(!InRaid)return false;var item=Bag.Entries.FirstOrDefault(x=>x.instanceId==instanceId);if(item==null||!Safe.Add(item.itemId,item.instanceId))return false;
            var next=Progress.Copy();next.secured=Safe.Entries.Select(e=>e.itemId).ToList();
            try{Commit(next);}catch{Safe.Remove(instanceId);throw;}Bag.Remove(instanceId);return true;
        }
        public bool Unprotect(string instanceId)
        {
            if(!InRaid)return false;var item=Safe.Entries.FirstOrDefault(x=>x.instanceId==instanceId);if(item==null||!Bag.Add(item.itemId,item.instanceId))return false;
            var next=Progress.Copy();next.secured=Safe.Entries.Where(e=>e.instanceId!=instanceId).Select(e=>e.itemId).ToList();
            try{Commit(next);}catch{Bag.Remove(instanceId);throw;}Safe.Remove(instanceId);return true;
        }
        public bool Sell(int index)
        {if(InRaid||index<0||index>=Progress.stash.Count)return false;var next=Progress.Copy();next.credits+=Items.Get(next.stash[index]).value;next.stash.RemoveAt(index);Commit(next);return true;}
        public bool BuyMedicine(string id)
        {if(InRaid||(id!="bandage"&&id!="medicine"))return false;int price=Items.Get(id).value*2;if(Progress.credits<price)return false;var next=Progress.Copy();next.credits-=price;next.stash.Add(id);Commit(next);return true;}
        public void SortStash(){if(InRaid)return;var next=Progress.Copy();next.stash=next.stash.OrderByDescending(id=>Items.Get(id).value).ToList();Commit(next);}
        public bool Pack(int index)
        {if(InRaid||index<0||index>=Progress.stash.Count)return false;var check=new Inventory();foreach(var id in Progress.prepared)check.Add(id);if(!check.Add(Progress.stash[index]))return false;var next=Progress.Copy();next.prepared.Add(next.stash[index]);next.stash.RemoveAt(index);Commit(next);return true;}
        public void UnpackAll(){if(InRaid)return;var next=Progress.Copy();next.stash.AddRange(next.prepared);next.prepared.Clear();Commit(next);}
        void Commit(ProgressData next) { store.Save(next); Progress=next; }
    }
}
