using System.Collections.Generic;
using UnityEngine;

namespace TwinkleOps
{
    public sealed class LootContainer : MonoBehaviour
    {
        public string displayName="搜索物资箱";
        public float searchSeconds=2;
        public List<string> Contents { get; private set; }
        public void Reveal() { Contents ??= LootTable.Roll(new System.Random(System.Guid.NewGuid().GetHashCode()),Random.Range(2,6)); }
        public bool Take(int index,Inventory bag)
        {
            if(Contents==null || index<0 || index>=Contents.Count || !bag.Add(Contents[index])) return false;
            Contents.RemoveAt(index); return true;
        }
    }
}
