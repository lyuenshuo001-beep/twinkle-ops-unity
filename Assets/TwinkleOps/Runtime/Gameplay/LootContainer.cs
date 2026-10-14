using System.Collections.Generic;
using UnityEngine;

namespace TwinkleOps
{
    public enum ContainerKind { Military, TrashCan, TrashBag, Suitcase, Toolbox, Safe }
    public sealed class LootContainer : MonoBehaviour
    {
        public ContainerKind kind;
        public string displayName="搜索物资箱";
        public float searchSeconds=2;
        public List<string> Contents { get; private set; }
        public void Reveal() { Contents ??= ContainerLoot.Roll(kind,new System.Random(System.Guid.NewGuid().GetHashCode())); }
        public bool Take(int index,Inventory bag)
        {
            if(Contents==null || index<0 || index>=Contents.Count || !bag.Add(Contents[index])) return false;
            Contents.RemoveAt(index); return true;
        }
    }
    public static class ContainerLoot
    {
        static readonly int[][] Weights={new[]{280,380,210,95,28,7},new[]{780,160,45,12,2,1},new[]{490,290,160,48,10,2},new[]{350,320,210,90,24,6},new[]{350,340,210,75,20,5},new[]{20,90,270,380,195,45}};
        public static List<string> Roll(ContainerKind kind,System.Random random)
        {
            var result=new List<string>();int count=kind==ContainerKind.Safe?random.Next(2,4):random.Next(2,6);
            for(int i=0;i<count;i++)
            {
                int roll=random.Next(1000),rarity=0;var weights=Weights[(int)kind];
                while(rarity<5 && roll>=weights[rarity])roll-=weights[rarity++];
                string[][] themed={new[]{"bandage","medicine","radio","drive","sensor","sample"},new[]{"battery","tools","phone","watch","gold","core"},new[]{"battery","medicine","phone","watch","gold","core"},new[]{"bandage","tools","phone","watch","gold","sample"},new[]{"battery","tools","radio","drive","sensor","core"},new[]{"battery","medicine","phone","watch","gold","core"}};
                if(random.Next(100)<80)result.Add(themed[(int)kind][rarity]);
                else result.Add(LootTable.RollOfRarity(random,(Rarity)rarity));
            }
            return result;
        }
    }
}

