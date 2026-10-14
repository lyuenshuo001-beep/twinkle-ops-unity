using UnityEngine;
using UnityEngine.AI;
namespace TwinkleOps
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Infected : MonoBehaviour
    {
        public Transform target;
        public Animator animator;
        public float health=100, maxHealth=100, detectionRange=32, attackDistance=1.15f, attackDamage=18;
        public bool Dead { get; private set; }
        public bool Attacking { get; private set; }
        public int SuccessfulHits { get; private set; }
        public float AttackProgress { get; private set; }
        public System.Action<Infected> recycled;
        NavMeshAgent agent;
        Transform leftHand,rightHand;
        float nextPath,nextSense,nextAttack,nextFlinch,attackStarted,deathAt;
        bool visible,hitDelivered,lunge;
        int attackIndex;
        string attackState;
        void Awake()
        {
            agent=GetComponent<NavMeshAgent>(); if(!animator)animator=GetComponentInChildren<Animator>();
            if(animator) { animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate; leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand); }
            agent.updateRotation=false;agent.stoppingDistance=.82f;agent.acceleration=14;agent.angularSpeed=540;agent.radius=.28f;
        }
        public void ResetForSpawn(Vector3 position,Transform player)
        {
            gameObject.SetActive(true); health=maxHealth;Dead=Attacking=hitDelivered=false; SuccessfulHits=0;
            target=player;agent.enabled=true;agent.Warp(position);agent.isStopped=false;nextAttack=Time.time+.8f;nextSense=nextPath=0;
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=true;
            animator.Rebind();animator.Update(0);animator.CrossFadeInFixedTime("Locomotion",.05f);
        }
        void Update()
        {
            if(Dead) { if(Time.time>=deathAt) { if(recycled!=null)recycled(this);else gameObject.SetActive(false); }return; }
            if(!target || !agent.enabled || !agent.isOnNavMesh)return;
            Vector3 delta=target.position-transform.position;float distance=delta.magnitude;delta.y=0;
            if(Time.time>=nextSense)
            { nextSense=Time.time+.18f;visible=!Physics.Linecast(transform.position+Vector3.up*1.35f,target.position+Vector3.up*1.1f,CombatRay.EnvironmentMask,QueryTriggerInteraction.Ignore); }
            if(Attacking)
            {
                var state=animator.GetCurrentAnimatorStateInfo(0);
                AttackProgress=state.IsName(attackState)?state.normalizedTime:0;
                Face(delta,AttackProgress<.55f?500:150);
                if(lunge && AttackProgress<.4f && distance>.88f) { agent.isStopped=false;agent.speed=4.8f;SetPath(); }else agent.isStopped=true;
                if(!hitDelivered && AttackProgress>=.22f && AttackProgress<=.7f && CanContactPlayer())
                { hitDelivered=true;SuccessfulHits++;GameApp.Instance?.Damage(attackDamage); }
                if((state.IsName(attackState)&&AttackProgress>=.93f)||Time.time-attackStarted>3.8f)
                { Attacking=false;nextAttack=Time.time+.2f;animator.CrossFadeInFixedTime("Locomotion",.18f); }
                animator.SetFloat("Speed",0,.12f,Time.deltaTime);return;
            }
            agent.isStopped=distance>detectionRange || (visible&&distance<=.86f);
            var current=animator.GetCurrentAnimatorStateInfo(0);
            if(current.IsName("Hit") && current.normalizedTime<.65f)agent.isStopped=true;
            agent.speed=4.1f;
            if(!agent.isStopped)SetPath();
            Vector3 facing=agent.velocity.sqrMagnitude>.08f?agent.velocity:delta;
            if(distance<detectionRange)Face(facing,430);
            animator.SetFloat("Speed",agent.velocity.magnitude,.12f,Time.deltaTime);
            animator.SetFloat("Stride",Mathf.Clamp(agent.velocity.magnitude/4.1f,.85f,1.15f));
            bool canLunge=distance>1.15f && distance<2 && attackIndex%3==2;
            if(visible && (distance<=attackDistance || canLunge) && Time.time>=nextAttack && Vector3.Angle(transform.forward,delta)<28)
            {
                lunge=canLunge;attackState=lunge?"Lunge":attackIndex%2==0?"AttackA":"AttackB";attackIndex++;
                Attacking=true;hitDelivered=false;attackStarted=Time.time;AttackProgress=0;agent.isStopped=!lunge;
                animator.CrossFadeInFixedTime(attackState,.1f);
            }
        }
        void SetPath() { if(Time.time<nextPath)return;nextPath=Time.time+.23f;agent.SetDestination(target.position); }
        void Face(Vector3 direction,float speed) { direction.y=0;if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),speed*Time.deltaTime); }
        public bool CanContactPlayer()
        {
            if(!target || !visible)return false;var delta=target.position-transform.position;delta.y=0;
            if(delta.magnitude>1.5f || Vector3.Angle(transform.forward,delta)>55)return false;
            return (Touches(leftHand)||Touches(rightHand)) && !Physics.Linecast(transform.position+Vector3.up*1.35f,target.position+Vector3.up*1.1f,CombatRay.EnvironmentMask,QueryTriggerInteraction.Ignore);
        }
        bool Touches(Transform hand)
        {
            if(!hand)return false;Vector3 axis=target.position;axis.y+=Mathf.Clamp(hand.position.y-target.position.y,.35f,1.55f);
            return Vector3.Distance(axis,hand.position)<.53f;
        }
        public bool TakeDamage(float amount) => TakeDamage(amount,HitRegion.Body,transform.position+Vector3.up);
        public bool TakeDamage(float amount,HitRegion region,Vector3 point)
        {
            if(Dead || amount<=0)return false;health=Mathf.Max(0,health-amount);
            if(health<=0)
            {
                Dead=true;Attacking=false;deathAt=Time.time+9;if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;agent.enabled=false;
                foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
                animator.CrossFadeInFixedTime("Death",.1f);return true;
            }
            if(Time.time>=nextFlinch && (!Attacking || region==HitRegion.Head))
            { nextFlinch=Time.time+.8f;Attacking=false;nextAttack=Time.time+.6f;animator.CrossFadeInFixedTime("Hit",.08f); }
            return true;
        }
    }
}
