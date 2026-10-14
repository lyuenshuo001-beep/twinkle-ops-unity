using System.Collections;
using UnityEngine;

namespace TwinkleOps
{
    public sealed class Rifle : MonoBehaviour
    {
        [System.Serializable] public sealed class WeaponSlot
        {
            public string name; public Transform model; public AudioClip shot;
            public float damage=34, rpm=750, recoil=1, reload=2.4f;
            public int ammo=30,reserve=120;
            public int capacity=30,pellets=1;
            public bool automatic=true,melee;
            public float spread=1.1f,aimSpread=.035f,reach=150;
        }
        public WeaponSlot[] weapons;
        public int ActiveWeapon { get; private set; }
        public string WeaponName => weapons!=null && weapons.Length>0?weapons[ActiveWeapon].name:"M4A1";
        public PlayerMotor motor;
        public Animator animator;
        public AudioSource audioSource;
        public AudioClip shot,reloadSound;
        public ParticleSystem muzzleFlash;
        public Transform weaponRoot;
        public LayerMask hitMask=~0;
        public int ammo=30, reserve=120;
        public float damage=34, roundsPerMinute=750, reloadSeconds=2.4f, range=150;
        public bool Reloading { get; private set; }
        public float HitUntil { get; private set; }
        public int ShotsFired { get; private set; }
        public int AudioEvents { get; private set; }
        public bool IsMelee => weapons!=null && weapons.Length>0 && weapons[ActiveWeapon].melee;
        public float Spread => weapons==null||weapons.Length==0?1: motor.AimHeld?weapons[ActiveWeapon].aimSpread:weapons[ActiveWeapon].spread+motor.MoveInput.magnitude*.7f;
        public float SwingProgress { get; private set; }
        float nextShot;
        Vector3 basePosition;
        Quaternion baseRotation;
        Transform magazine;
        Vector3 magazinePosition;
        float recoilScale=1, reloadStarted,equippedAt,swingStarted=-10;
        bool meleePending;
        void Start() { if(weapons!=null && weapons.Length>0) Equip(0,false); else CacheTransform(); }
        void CacheTransform()
        {
            if(!weaponRoot) return;
            basePosition=weaponRoot.localPosition; baseRotation=weaponRoot.localRotation;
            magazine=null;
            foreach(var t in weaponRoot.GetComponentsInChildren<Transform>()) if(t.name=="Magazine") { magazine=t; magazinePosition=t.localPosition; }
        }
        public void Equip(int index,bool save=true)
        {
            if(weapons==null || index<0 || index>=weapons.Length || (save && index==ActiveWeapon)) return;
            StopAllCoroutines(); Reloading=false; meleePending=false;equippedAt=Time.time;
            if(magazine) magazine.localPosition=magazinePosition;
            if(save) { weapons[ActiveWeapon].ammo=ammo; weapons[ActiveWeapon].reserve=reserve; }
            if(save && weaponRoot) { weaponRoot.localPosition=basePosition; weaponRoot.localRotation=baseRotation; }
            ActiveWeapon=index; var slot=weapons[index];
            foreach(var w in weapons) w.model.gameObject.SetActive(w==slot);
            weaponRoot=slot.model; shot=slot.shot; ammo=slot.ammo; reserve=slot.reserve;
            damage=slot.damage; roundsPerMinute=slot.rpm; recoilScale=slot.recoil; reloadSeconds=slot.reload;
            animator=weaponRoot.GetComponentInChildren<Animator>(); muzzleFlash=weaponRoot.GetComponentInChildren<ParticleSystem>();
            CacheTransform(); nextShot=Time.time+.3f;
        }
        void LateUpdate()
        {
            if(!motor) return;
            if(motor.InputEnabled && weapons!=null && weapons.Length>1)
            { if(motor.SwitchPressed) Equip((ActiveWeapon+1)%weapons.Length); else if(motor.WeaponSelected>=0) Equip(motor.WeaponSelected); }
            bool ads=motor.InputEnabled && motor.AimHeld && !Reloading && !IsMelee;
            motor.view.fieldOfView=Mathf.Lerp(motor.view.fieldOfView,ads?48:72,Time.deltaTime*14);
            if(animator) animator.SetBool("ADS",ads);
            if(weaponRoot)
            {
                float bob=Mathf.Sin(Time.time*10)*motor.MoveInput.magnitude*.009f;
                var target=basePosition+(ads?new Vector3(-basePosition.x,ActiveWeapon==1?.12f:.085f,.06f):new Vector3(0,bob,0));
                float reloadPhase=Reloading?Mathf.Sin(Mathf.Clamp01((Time.time-reloadStarted)/reloadSeconds)*Mathf.PI):0;
                target+=new Vector3(.05f,-.1f,-.08f)*reloadPhase;
                target+=Vector3.down*(.16f*Mathf.Clamp01(1-(Time.time-equippedAt)/.3f));
                SwingProgress=Mathf.Clamp01((Time.time-swingStarted)/.55f);
                float swing=IsMelee?Mathf.Sin(SwingProgress*Mathf.PI):0;
                target+=new Vector3(-.23f,.08f,.24f)*swing;
                weaponRoot.localPosition=Vector3.Lerp(weaponRoot.localPosition,target,Time.deltaTime*15);
                weaponRoot.localRotation=Quaternion.Slerp(weaponRoot.localRotation,baseRotation*Quaternion.Euler(12*reloadPhase+20*swing,-70*swing,-25*reloadPhase-35*swing),Time.deltaTime*16);
                if(magazine) magazine.localPosition=magazinePosition+Vector3.down*(.22f*reloadPhase);
            }
            if(!motor.InputEnabled) { meleePending=false;return; }
            if(meleePending && Time.time-swingStarted>=.2f) { meleePending=false;MeleeStrike(); }
            if(motor.ReloadPressed && !Reloading && !IsMelee && ammo<weapons[ActiveWeapon].capacity && reserve>0) StartCoroutine(Reload());
            if((weapons[ActiveWeapon].automatic?(motor.FireHeld||motor.FirePressed):motor.FirePressed)) TryFire();
        }
        public bool TryFire()
        {
            if(!motor.InputEnabled || Reloading || Time.time<nextShot || (!IsMelee&&ammo<=0))return false;
            nextShot=Time.time+60/roundsPerMinute;ShotsFired++;
            if(shot && audioSource) { audioSource.PlayOneShot(shot);AudioEvents++; }
            if(IsMelee) { swingStarted=Time.time;meleePending=true;return true; }
            ammo--;if(muzzleFlash)muzzleFlash.Play();if(animator)animator.SetTrigger("Fire");
            var cam=motor.view.transform;
            for(int i=0;i<Mathf.Max(1,weapons[ActiveWeapon].pellets);i++)
            {
                Vector2 random=Random.insideUnitCircle*Mathf.Tan(Spread*Mathf.Deg2Rad);
                Vector3 direction=(cam.forward+cam.right*random.x+cam.up*random.y).normalized;
                bool hit=CombatRay.Cast(cam.position,direction,weapons[ActiveWeapon].reach,out var info);
                if(hit && CombatRay.Damage(info,damage))HitUntil=Time.time+.16f;
                CombatDebug.Trace(cam.position,hit?info.point:cam.position+direction*weapons[ActiveWeapon].reach);
            }
            motor.AddRecoil((motor.AimHeld?.65f:1.2f)*recoilScale);
            if(weaponRoot) { weaponRoot.localPosition-=Vector3.forward*.028f; weaponRoot.localRotation*=Quaternion.Euler(-3,Random.Range(-.5f,.5f),0); }
            return true;
        }
        void MeleeStrike()
        {
            var cam=motor.view.transform;Physics.SyncTransforms();
            var hits=Physics.SphereCastAll(cam.position,.22f,cam.forward,1.7f,CombatRay.Mask,QueryTriggerInteraction.Collide);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(!hit.collider.GetComponent<InfectedHitbox>())break;
                if(Physics.Linecast(cam.position,hit.point,CombatRay.EnvironmentMask,QueryTriggerInteraction.Ignore))break;
                if(CombatRay.Damage(hit,damage)) { HitUntil=Time.time+.22f;CombatDebug.Trace(cam.position,hit.point);break; }
            }
        }
        IEnumerator Reload()
        {
            Reloading=true; reloadStarted=Time.time; if(animator) animator.SetTrigger("Reload"); if(reloadSound) audioSource.PlayOneShot(reloadSound);
            yield return new WaitForSeconds(reloadSeconds);
            int count=Mathf.Min(weapons[ActiveWeapon].capacity-ammo,reserve); ammo+=count; reserve-=count; Reloading=false;
        }
        void OnDisable() { StopAllCoroutines(); Reloading=false; }
    }
}
