using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.AI;
namespace TwinkleOps.Tests
{
    public class V11CombatTests
    {
        const string Prefix="TwinkleOps.v11.qa.";
        [UnityTest] public IEnumerator AnimatedBonesContactHitboxesAudioAndMedicine()
        {
            PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);
            GameApp.ProgressStoreFactory=()=>new PlayerPrefsStore(Prefix);
            yield return SceneManager.LoadSceneAsync("Lobby");yield return null;
            var app=GameApp.Instance;
            typeof(GameApp).GetMethod("Begin",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);
            yield return null;yield return new WaitForSeconds(.5f);
            var player=app.Player;Assert.NotNull(player);
            var director=Object.FindAnyObjectByType<InfectedDirector>();if(director)director.enabled=false;
            var zombies=Object.FindObjectsByType<Infected>(FindObjectsSortMode.None);
            foreach(var enemy in zombies)enemy.gameObject.SetActive(false);
            var z=zombies[0];z.ResetForSpawn(player.transform.position+Vector3.forward*4,player.transform);
            z.enabled=false;z.GetComponent<NavMeshAgent>().isStopped=true;
            z.transform.rotation=Quaternion.LookRotation(-Vector3.forward);
            var hand=z.animator.GetBoneTransform(HumanBodyBones.RightHand);
            z.animator.Play("AttackA",0,0);yield return null;
            var from=hand.position;yield return new WaitForSeconds(.5f);
            Debug.Log("V11_BONES delta="+Vector3.Distance(from,hand.position)+" hand="+z.transform.InverseTransformPoint(hand.position));
            Assert.Greater(Vector3.Distance(from,hand.position),.1f,"Retargeted attack must move actual hand bones");
            foreach(var state in new[]{"AttackA","AttackB","Lunge"})
            {
                for(int i=0;i<10;i++)
                {
                    z.animator.Play(state,0,i*.1f);z.animator.Update(.001f);
                    Debug.Log("V11_HAND "+state+" "+i+" L="+z.transform.InverseTransformPoint(z.animator.GetBoneTransform(HumanBodyBones.LeftHand).position).ToString("F3")+" R="+z.transform.InverseTransformPoint(hand.position).ToString("F3"));
                }
            }
            foreach(var region in new[]{HitRegion.Head,HitRegion.Body,HitRegion.Limb})
            {
                var box=z.GetComponentsInChildren<InfectedHitbox>().First(h=>h.region==region);
                var c=box.GetComponent<Collider>();Physics.SyncTransforms();
                // Fire a real ray at an isolated bone collider, avoiding occlusion by other body parts.
                foreach(var h in z.GetComponentsInChildren<InfectedHitbox>())h.GetComponent<Collider>().enabled=h==box;
                Vector3 start=c.bounds.center+Vector3.forward*2;
                Assert.IsTrue(CombatRay.Cast(start,-Vector3.forward,3,out var hit),region+" ray must hit");
                Assert.AreEqual(box,hit.collider.GetComponent<InfectedHitbox>());
                float hp=z.health;Assert.IsTrue(CombatRay.Damage(hit,10));
                Assert.AreEqual(10*box.Multiplier,hp-z.health,.01f);
            }
            z.ResetForSpawn(player.transform.position+Vector3.forward*3,player.transform);z.enabled=true;
            float deadline=Time.time+12;
            while(z.SuccessfulHits==0 && Time.time<deadline)yield return null;
            Debug.Log("V11_CONTACT hits="+z.SuccessfulHits+" distance="+Vector3.Distance(z.transform.position,player.transform.position)+" health="+app.Health);
            Assert.Greater(z.SuccessfulHits,0,"Running infected must reach and physically contact player during attack");
            Assert.Less(app.Health,100);z.gameObject.SetActive(false);
            var rifle=player.GetComponent<Rifle>();rifle.Equip(1);yield return new WaitForSeconds(.4f);
            int count=rifle.ShotsFired,audio=rifle.AudioEvents,ammo=rifle.ammo;
            Assert.IsTrue(rifle.TryFire());Assert.IsFalse(rifle.TryFire());
            Assert.AreEqual(count+1,rifle.ShotsFired);Assert.AreEqual(audio+1,rifle.AudioEvents);Assert.AreEqual(ammo-1,rifle.ammo);
            Assert.Less(rifle.shot.length,.65f,"AK sound must be an isolated discharge, not a burst");
            app.Damage(45);float before=app.Health;app.Session.Bag.Add("bandage");
            string id=app.Session.Bag.Entries.Last().instanceId;Assert.IsTrue(app.UseMedicine(id));Assert.AreEqual(before+25,app.Health,.01f);Assert.IsFalse(app.UseMedicine(id));
            app.Session.Bag.Add("medicine");Assert.IsTrue(app.UseMedicine(app.Session.Bag.Entries.Last().instanceId));Assert.AreEqual(100,app.Health);
            app.Session.Bag.Add("bandage");Assert.IsFalse(app.UseMedicine(app.Session.Bag.Entries.Last().instanceId));
            z.ResetForSpawn(player.transform.position+Vector3.forward*1.25f,player.transform);z.enabled=false;z.GetComponent<NavMeshAgent>().isStopped=true;
            player.enabled=false;player.view.transform.LookAt(z.animator.GetBoneTransform(HumanBodyBones.Chest).position);
            rifle.Equip(2);yield return new WaitForSeconds(.35f);
            Assert.IsTrue(rifle.TryFire());yield return new WaitForSeconds(.3f);Assert.Less(z.health,100,"Knife must deal physical contact damage");
            yield return new WaitForSeconds(.4f);player.view.transform.LookAt(z.animator.GetBoneTransform(HumanBodyBones.Chest).position);Assert.IsTrue(rifle.TryFire());yield return new WaitForSeconds(.3f);
            Assert.IsTrue(z.Dead,"Two body knife strikes must kill a normal infected");
            Debug.Log("V11_COMBAT_TEST_OK");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameApp.Instance)Object.Destroy(GameApp.Instance.gameObject);GameApp.ProgressStoreFactory=null;
            PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);yield return null;
        }
    }
}
