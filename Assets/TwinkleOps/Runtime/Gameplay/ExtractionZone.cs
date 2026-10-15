using UnityEngine;
namespace TwinkleOps
{
    public enum ExtractionCondition { None, Lightweight, Signal }
    public sealed class ExtractionZone : MonoBehaviour
    {
        public string displayName="北侧撤离";
        public float radius=3, seconds=8;
        public ExtractionCondition condition;
        public float weightLimit=8,signalDelay=90;
        public bool Available(GameApp app)=>condition==ExtractionCondition.None||(condition==ExtractionCondition.Lightweight?app.Session.Bag.Weight<=weightLimit:Time.time-app.RaidStartedAt>=signalDelay);
        public string Requirement(GameApp app)=>condition==ExtractionCondition.None?"无附加条件":condition==ExtractionCondition.Lightweight?"背包不超过 "+weightLimit+" kg":"信号就绪还需 "+Mathf.CeilToInt(Mathf.Max(0,signalDelay-(Time.time-app.RaidStartedAt)))+" 秒";
        public float Remaining { get; private set; }
        void OnEnable() { Remaining=seconds; }
        void Update()
        {
            var app=GameApp.Instance;
            if(!app || !app.Player || !app.Session.InRaid) { Remaining=seconds; return; }
            bool inside=Vector3.Distance(transform.position,app.Player.transform.position)<=radius;
            bool ready=Available(app);Remaining=inside&&ready?Mathf.Max(0,Remaining-Time.deltaTime):seconds;
            if(inside && ready && Remaining<=0) app.Extract();
        }
        void OnDrawGizmos() { Gizmos.color=Color.green; Gizmos.DrawWireSphere(transform.position,radius); }
    }
}
