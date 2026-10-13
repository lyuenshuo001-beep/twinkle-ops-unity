using UnityEngine;
namespace TwinkleOps
{
    public sealed class ExtractionZone : MonoBehaviour
    {
        public float radius=3, seconds=8;
        public float Remaining { get; private set; }
        void OnEnable() { Remaining=seconds; }
        void Update()
        {
            var app=GameApp.Instance;
            if(!app || !app.Player || !app.Session.InRaid) { Remaining=seconds; return; }
            bool inside=Vector3.Distance(transform.position,app.Player.transform.position)<=radius;
            Remaining=inside?Mathf.Max(0,Remaining-Time.deltaTime):seconds;
            if(inside && Remaining<=0) app.Extract();
        }
        void OnDrawGizmos() { Gizmos.color=Color.green; Gizmos.DrawWireSphere(transform.position,radius); }
    }
}
