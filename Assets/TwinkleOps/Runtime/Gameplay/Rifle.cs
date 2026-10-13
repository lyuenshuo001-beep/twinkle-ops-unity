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
        public float Spread => motor.AimHeld?.12f:1.1f+motor.MoveInput.magnitude*1.8f;
        float nextShot;
        Vector3 basePosition;
        Quaternion baseRotation;
        Transform magazine;
        Vector3 magazinePosition;
        float recoilScale=1, reloadStarted;
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
            StopAllCoroutines(); Reloading=false;
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
            bool ads=motor.InputEnabled && motor.AimHeld && !Reloading;
            motor.view.fieldOfView=Mathf.Lerp(motor.view.fieldOfView,ads?48:72,Time.deltaTime*14);
            if(animator) animator.SetBool("ADS",ads);
            if(weaponRoot)
            {
                float bob=Mathf.Sin(Time.time*10)*motor.MoveInput.magnitude*.009f;
                var target=basePosition+(ads?new Vector3(-basePosition.x,ActiveWeapon==1?.12f:.085f,.06f):new Vector3(0,bob,0));
                float reloadPhase=Reloading?Mathf.Sin(Mathf.Clamp01((Time.time-reloadStarted)/reloadSeconds)*Mathf.PI):0;
                target+=new Vector3(.05f,-.1f,-.08f)*reloadPhase;
                weaponRoot.localPosition=Vector3.Lerp(weaponRoot.localPosition,target,Time.deltaTime*15);
                weaponRoot.localRotation=Quaternion.Slerp(weaponRoot.localRotation,baseRotation*Quaternion.Euler(12*reloadPhase,0,-25*reloadPhase),Time.deltaTime*12);
                if(magazine) magazine.localPosition=magazinePosition+Vector3.down*(.22f*reloadPhase);
            }
            if(!motor.InputEnabled) return;
            if(motor.ReloadPressed && !Reloading && ammo<30 && reserve>0) StartCoroutine(Reload());
            if(motor.FireHeld && !Reloading && ammo>0 && Time.time>=nextShot) Shoot();
        }
        void Shoot()
        {
            nextShot=Time.time+60/roundsPerMinute; ammo--;
            if(shot) audioSource.PlayOneShot(shot); if(muzzleFlash) muzzleFlash.Play(); if(animator) animator.SetTrigger("Fire");
            var cam=motor.view.transform;
            Vector2 random=Random.insideUnitCircle*Mathf.Tan(Spread*Mathf.Deg2Rad);
            Vector3 direction=(cam.forward+cam.right*random.x+cam.up*random.y).normalized;
            if(Physics.Raycast(cam.position,direction,out var hit,range,hitMask,QueryTriggerInteraction.Ignore))
            { var zombie=hit.collider.GetComponentInParent<Infected>(); if(zombie && zombie.TakeDamage(damage)) HitUntil=Time.time+.12f; }
            motor.AddRecoil((motor.AimHeld?.65f:1.2f)*recoilScale);
            if(weaponRoot) { weaponRoot.localPosition-=Vector3.forward*.028f; weaponRoot.localRotation*=Quaternion.Euler(-3,Random.Range(-.5f,.5f),0); }
        }
        IEnumerator Reload()
        {
            Reloading=true; reloadStarted=Time.time; if(animator) animator.SetTrigger("Reload"); if(reloadSound) audioSource.PlayOneShot(reloadSound);
            yield return new WaitForSeconds(reloadSeconds);
            int count=Mathf.Min(30-ammo,reserve); ammo+=count; reserve-=count; Reloading=false;
        }
        void OnDisable() { StopAllCoroutines(); Reloading=false; }
    }
}
