using UnityEngine;
using UnityEngine.AI;

namespace TwinkleOps
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Infected : MonoBehaviour
    {
        public Transform target;
        public Animator animator;
        public float health=100, detectionRange=24, attackDistance=1.6f, attackDamage=18;
        NavMeshAgent agent;
        float nextPath, nextAttack, pendingHit=-1;
        bool dead;
        void Awake() { agent=GetComponent<NavMeshAgent>(); if(!animator) animator=GetComponentInChildren<Animator>(); }
        void Update()
        {
            if(dead || !target || !agent.isOnNavMesh) return;
            float distance=Vector3.Distance(transform.position,target.position);
            bool visible=!Physics.Linecast(transform.position+Vector3.up*1.4f,target.position+Vector3.up*1.2f,out var hit,~(1<<8),QueryTriggerInteraction.Ignore)
                || hit.transform.IsChildOf(target);
            if(pendingHit>=0 && Time.time>=pendingHit)
            { pendingHit=-1; if(distance<attackDistance+.25f && visible) GameApp.Instance?.Damage(attackDamage); }
            agent.isStopped=distance<=attackDistance || distance>detectionRange;
            if(!agent.isStopped && Time.time>=nextPath) { nextPath=Time.time+.25f; agent.SetDestination(target.position); }
            animator?.SetFloat("Speed",agent.velocity.magnitude);
            if(distance<=attackDistance && visible && Time.time>=nextAttack)
            {
                nextAttack=Time.time+1.6f; pendingHit=Time.time+.55f; animator?.SetTrigger("Attack");
                Vector3 look=target.position-transform.position; look.y=0; if(look.sqrMagnitude>.01f) transform.rotation=Quaternion.LookRotation(look);
            }
        }
        public bool TakeDamage(float amount)
        {
            if(dead) return false;
            health-=amount; animator?.SetTrigger(health<=0?"Death":"Hit");
            if(health<=0)
            {
                dead=true; pendingHit=-1; if(agent.isOnNavMesh) agent.isStopped=true; agent.enabled=false;
                foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=false;
                Destroy(gameObject,18);
            }
            return true;
        }
    }
}
