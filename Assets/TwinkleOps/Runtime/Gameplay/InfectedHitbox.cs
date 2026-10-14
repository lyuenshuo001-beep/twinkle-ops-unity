using UnityEngine;
namespace TwinkleOps
{
    public enum HitRegion { Head, Body, Limb }
    public sealed class InfectedHitbox : MonoBehaviour
    {
        public Infected owner;
        public HitRegion region;
        public float Multiplier => region==HitRegion.Head?2.25f:region==HitRegion.Body?1:.65f;
        public bool Apply(float damage,Vector3 point) => owner && owner.TakeDamage(damage*Multiplier,region,point);
        void OnDrawGizmosSelected()
        {
            Gizmos.color=region==HitRegion.Head?Color.red:region==HitRegion.Body?Color.green:Color.cyan;
            var c=GetComponent<CapsuleCollider>();if(!c)return;
            var before=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;
            Gizmos.DrawWireCube(c.center,new Vector3(c.radius*2,c.height,c.radius*2));Gizmos.matrix=before;
        }
    }
    public static class CombatRay
    {
        public const int Mask=~((1<<8)|(1<<10)|(1<<2));
        public const int EnvironmentMask=~((1<<8)|(1<<9)|(1<<10)|(1<<2));
        public static bool Cast(Vector3 origin,Vector3 direction,float range,out RaycastHit hit)
        {
            // Animator bone transforms must reach PhysX before the shot, including between FixedUpdates.
            Physics.SyncTransforms();
            return Physics.Raycast(origin,direction,out hit,range,Mask,QueryTriggerInteraction.Collide);
        }
        public static bool Damage(RaycastHit hit,float amount)
        {
            var box=hit.collider.GetComponent<InfectedHitbox>();
            if(!box) return false;
            bool applied=box.Apply(amount,hit.point);
            if(applied) CombatDebug.LastHit=box.region+"  "+(amount*box.Multiplier).ToString("F0");
            return applied;
        }
    }
}
