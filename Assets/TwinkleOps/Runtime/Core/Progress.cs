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
        public List<string> stash=new();
        public ProgressData Copy() => new() { version=version, successfulRaids=successfulRaids, deaths=deaths, activeRaid=activeRaid, stash=new(stash) };
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
        public bool InRaid => !string.IsNullOrEmpty(Progress.activeRaid);
        public RaidSession(IProgressStore store)
        {
            this.store=store; Progress=store.Load();
            // A closed/crashed raid is a loss. Stash remains intact.
            if(InRaid) Die();
        }
        public void Begin()
        {
            if(InRaid) throw new InvalidOperationException("A raid is already active");
            var next=Progress.Copy(); next.activeRaid=Guid.NewGuid().ToString("N"); Commit(next); Bag.Clear();
        }
        public bool Extract()
        {
            if(!InRaid) return false;
            var next=Progress.Copy(); next.stash.AddRange(Bag.Entries.Select(x=>x.itemId));
            next.activeRaid=""; next.successfulRaids++; Commit(next); Bag.Clear(); return true;
        }
        public bool Die()
        {
            if(!InRaid) return false;
            var next=Progress.Copy(); next.activeRaid=""; next.deaths++; Commit(next); Bag.Clear(); return true;
        }
        void Commit(ProgressData next) { store.Save(next); Progress=next; }
    }
}
