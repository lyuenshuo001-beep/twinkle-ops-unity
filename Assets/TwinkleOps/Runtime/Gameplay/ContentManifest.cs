using System.Collections.Generic;
using UnityEngine;

namespace TwinkleOps
{
    [CreateAssetMenu(menuName="Twinkle Ops/Content Manifest")]
    public sealed class ContentManifest : ScriptableObject
    {
        [Header("Licensed final art only. Never assign Unity primitives.")]
        public GameObject environment, m4a1, akm, arms, zombie, searchContainer, droppedLoot;
        public RuntimeAnimatorController weaponAnimator, zombieAnimator;
        public AudioClip gunshot, akmGunshot, reload, ambience;
        public Sprite[] itemIcons;
        public bool licensesReviewed, visualQualityReviewed;
        [TextArea] public string credits;
        [Header("Map placement; tune against the imported environment")]
        public Vector3 playerSpawn=new(0,1,0);
        public Vector3 containerA=new(4,0,4), containerB=new(-4,0,10), extraction=new(0,0,20);
        public Vector3[] extraContainers=System.Array.Empty<Vector3>();
        public Vector3[] zombieSpawns={new(8,0,8),new(-8,0,12)};
        public Vector3 weaponPosition=new(.15f,-.18f,.36f), weaponRotation, weaponScale=Vector3.one;

        public List<string> Missing(bool requireVisualReview=true)
        {
            var result=new List<string>();
            CheckModel(environment,"写实地图",result); CheckModel(m4a1,"M4A1 模型",result);
            CheckModel(zombie,"写实感染者模型",result); CheckModel(searchContainer,"可搜索容器模型",result);
            CheckModel(droppedLoot,"物资掉落模型",result);
            if(!weaponAnimator) result.Add("持枪 / ADS / 射击 / 换弹动画");
            if(!zombieAnimator) result.Add("感染者待机 / 奔跑 / 攻击 / 受击 / 死亡动画");
            if(!gunshot || !reload || !ambience) result.Add("实录枪声、换弹与环境音");
            if(itemIcons==null || itemIcons.Length!=Items.All.Length || System.Array.Exists(itemIcons,x=>!x)) result.Add("全部物品图标");
            if(!licensesReviewed || string.IsNullOrWhiteSpace(credits)) result.Add("资源来源及授权登记");
            if(requireVisualReview && !visualQualityReviewed) result.Add("场景与动画视觉验收");
            return result;
        }
        static void CheckModel(GameObject model,string label,List<string> result)
        {
            if(!model || model.GetComponentsInChildren<Renderer>(true).Length==0) result.Add(label);
        }
    }
}
