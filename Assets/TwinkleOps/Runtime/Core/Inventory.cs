using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinkleOps
{
    public enum Rarity { White, Green, Blue, Purple, Gold, Red }

    [Serializable]
    public sealed class ItemSpec
    {
        public string id, name;
        public Rarity rarity;
        public int value, width = 1, height = 1;
        public float Weight => id=="gold"?2:id=="tools"?2.4f:id=="sensor"?1.8f:id=="core"?2.1f:id=="radio"?1.2f:id=="medicine"?.6f:.25f;
        public ItemSpec(string id, string name, Rarity rarity, int value, int width = 1, int height = 1)
        { this.id=id; this.name=name; this.rarity=rarity; this.value=value; this.width=width; this.height=height; }
    }

    public static class Items
    {
        public static readonly ItemSpec[] All = {
            new("bandage", "医用绷带", Rarity.White, 120), new("battery", "干电池", Rarity.White, 180),
            new("tools", "维修工具", Rarity.Green, 850, 2, 1), new("medicine", "急救药品", Rarity.Green, 1100, 1, 2),
            new("phone", "智能手机", Rarity.Blue, 3600), new("radio", "军用电台", Rarity.Blue, 4800, 1, 2),
            new("watch", "精密腕表", Rarity.Purple, 12500), new("drive", "加密硬盘", Rarity.Purple, 16800, 2, 1),
            new("gold", "投资金条", Rarity.Gold, 48000, 2, 1), new("sensor", "光学传感器", Rarity.Gold, 62000, 2, 2),
            new("sample", "机密研究样本", Rarity.Red, 150000, 1, 2), new("core", "实验处理器", Rarity.Red, 220000, 2, 2)
        };
        public static ItemSpec Get(string id) => All.FirstOrDefault(x => x.id == id)
            ?? throw new ArgumentException("Unknown item: " + id);
    }

    [Serializable]
    public sealed class PlacedItem
    {
        public string instanceId, itemId;
        public int x, y;
        public bool rotated;
        public ItemSpec Spec => Items.Get(itemId);
        public int Width => rotated ? Spec.height : Spec.width;
        public int Height => rotated ? Spec.width : Spec.height;
        public PlacedItem Copy() => new() { instanceId=instanceId, itemId=itemId, x=x, y=y, rotated=rotated };
    }

    public sealed class Inventory
    {
        readonly List<PlacedItem> entries = new();
        public int Columns { get; }
        public int Rows { get; }
        public IReadOnlyList<PlacedItem> Entries => entries;
        public int TotalValue => entries.Sum(x => x.Spec.value);
        public float Weight => entries.Sum(x=>x.Spec.Weight);
        public bool AddAt(string itemId,string instanceId,int x,int y,bool rotated)
        { var item=new PlacedItem{itemId=itemId,instanceId=instanceId??Guid.NewGuid().ToString("N"),x=x,y=y,rotated=rotated};if(entries.Any(e=>e.instanceId==item.instanceId)||!Fits(item,x,y,rotated))return false;entries.Add(item);return true; }
        public Inventory(int columns = 6, int rows = 5) { Columns=columns; Rows=rows; }
        public bool Fits(PlacedItem item, int x, int y, bool rotated)
        {
            int w=rotated ? item.Spec.height : item.Spec.width, h=rotated ? item.Spec.width : item.Spec.height;
            return x>=0 && y>=0 && x+w<=Columns && y+h<=Rows && !entries.Any(other =>
                other.instanceId!=item.instanceId && x<other.x+other.Width && x+w>other.x && y<other.y+other.Height && y+h>other.y);
        }
        public bool Add(string itemId, string instanceId = null)
        {
            var item=new PlacedItem { itemId=itemId, instanceId=instanceId ?? Guid.NewGuid().ToString("N") };
            _=item.Spec;
            if(entries.Any(x=>x.instanceId==item.instanceId)) return false;
            for(int r=0;r<2;r++) for(int y=0;y<Rows;y++) for(int x=0;x<Columns;x++)
                if(Fits(item,x,y,r==1)) { item.x=x; item.y=y; item.rotated=r==1; entries.Add(item); return true; }
            return false;
        }
        public bool Move(string id, int x, int y, bool rotated)
        {
            var item=entries.FirstOrDefault(e=>e.instanceId==id);
            if(item==null || !Fits(item,x,y,rotated)) return false;
            item.x=x; item.y=y; item.rotated=rotated; return true;
        }
        public bool Remove(string id) => entries.RemoveAll(e=>e.instanceId==id)>0;
        public bool Sort()
        {
            var packed=new Inventory(Columns,Rows);
            foreach(var item in entries.OrderByDescending(e=>e.Width*e.Height).ThenByDescending(e=>e.Spec.value))
                if(!packed.Add(item.itemId,item.instanceId)) return false;
            entries.Clear(); entries.AddRange(packed.entries); return true;
        }
        public void Clear() => entries.Clear();
    }

    public static class LootTable
    {
        // 55%, 25%, 12%, 5.5%, 2%, 0.5%; red is possible, never guaranteed.
        static readonly int[] Weights={550,250,120,55,20,5};
        public static Rarity RollRarity(int roll)
        {
            if(roll<0 || roll>=1000) throw new ArgumentOutOfRangeException(nameof(roll));
            for(int i=0;i<Weights.Length;i++) { if(roll<Weights[i]) return (Rarity)i; roll-=Weights[i]; }
            throw new InvalidOperationException();
        }
        public static List<string> Roll(Random random, int count)
        {
            var result=new List<string>();
            for(int i=0;i<count;i++)
            { var rarity=RollRarity(random.Next(1000)); var pool=Items.All.Where(x=>x.rarity==rarity).ToArray(); result.Add(pool[random.Next(pool.Length)].id); }
            return result;
        }
        public static string RollOfRarity(Random random,Rarity rarity)
        { var pool=Items.All.Where(x=>x.rarity==rarity).ToArray();return pool[random.Next(pool.Length)].id; }
    }
}
