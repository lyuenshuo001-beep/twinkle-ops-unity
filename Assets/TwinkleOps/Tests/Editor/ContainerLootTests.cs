using NUnit.Framework;
using System;
using System.Linq;
namespace TwinkleOps.Tests
{
    public class ContainerLootTests
    {
        [Test] public void ProfilesPreserveAllRaritiesAndDifferentValueDistributions()
        {
            var values=new double[6];
            for(int kind=0;kind<6;kind++)
            {
                var random=new Random(413+kind);var seen=new bool[6];int count=0;double total=0;
                for(int i=0;i<20000;i++)foreach(string id in ContainerLoot.Roll((ContainerKind)kind,random))
                {var item=Items.Get(id);seen[(int)item.rarity]=true;total+=item.value;count++;}
                Assert.IsTrue(seen.All(x=>x),"Each profile retains a chance of all six rarities");values[kind]=total/count;
            }
            Assert.Greater(values[(int)ContainerKind.Safe],values[(int)ContainerKind.TrashCan]*6);
            Assert.Greater(values[(int)ContainerKind.Military],values[(int)ContainerKind.TrashCan]);
        }
    }
}
