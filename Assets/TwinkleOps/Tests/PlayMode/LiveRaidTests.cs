using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TwinkleOps.Tests
{
    public class LiveRaidTests
    {
        const string Prefix="TwinkleOps.integration.qa.";
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
        Keyboard testKeyboard;
        static void Call(object obj,string method,params object[] args) => obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
        [UnityTest]
        public IEnumerator RealScene_MovementWeaponsLootExtractionPersistenceAndDeath()
        {
            background=InputSystem.settings.backgroundBehavior;
            editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            PlayerPrefs.DeleteKey(Prefix+0); PlayerPrefs.DeleteKey(Prefix+1);
            GameApp.ProgressStoreFactory=()=>new PlayerPrefsStore(Prefix);
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            var app=GameApp.Instance; Assert.NotNull(app); Assert.NotNull(app.Session);
            Assert.IsEmpty(app.content.Missing(),"Content gate must be ready");
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded("InfectionZone"),"Raid must be in the active build profile");
            Call(app,"Begin");
            for(int i=0;i<100 && !app.Player;i++) yield return new WaitForSeconds(.1f);
            Assert.NotNull(app.Player,"Lobby must enter the real raid: "+typeof(GameApp).GetField("notice",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(app));
            var player=app.Player; var rifle=player.GetComponent<Rifle>(); Assert.GreaterOrEqual(rifle.weapons.Length,4);
            var director=Object.FindAnyObjectByType<InfectedDirector>();if(director)director.enabled=false;
            var boxes=Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None); Assert.GreaterOrEqual(boxes.Length,20);
            var key=testKeyboard=InputSystem.AddDevice<Keyboard>(); key.MakeCurrent(); var before=player.transform.position;
            InputSystem.QueueStateEvent(key,new KeyboardState(Key.W)); yield return new WaitForSeconds(.5f);
            Assert.IsTrue(key.wKey.isPressed,"Synthetic keyboard must retain W during headless play");
            Assert.IsTrue(player.InputEnabled,"Raid must enable player input");
            InputSystem.QueueStateEvent(key,new KeyboardState()); yield return null;
            Assert.Greater(Vector3.Distance(before,player.transform.position),1,"W must move the physical controller");
            rifle.Equip(1); Assert.AreEqual("AKM",rifle.WeaponName); Assert.IsTrue(rifle.audioSource.isActiveAndEnabled,"Switching must preserve gun audio");
            var gunBounds=rifle.weaponRoot.GetComponentInChildren<MeshRenderer>().bounds;
            Assert.Greater(gunBounds.size.z,gunBounds.size.x*3,"AKM barrel must face forward, preserving imported axes");
            Assert.Less(Vector3.Distance(rifle.muzzleFlash.transform.position,player.view.transform.position),2,"AKM muzzle must remain near the barrel after FBX scale conversion");
            yield return new WaitForSeconds(.35f);Assert.IsTrue(rifle.TryFire()); Assert.AreEqual(29,rifle.ammo); rifle.Equip(0); Assert.AreEqual(30,rifle.ammo); rifle.Equip(1); Assert.AreEqual(29,rifle.ammo);
            // Start the same reload coroutine that the real input handler starts.
            rifle.StartCoroutine((IEnumerator)typeof(Rifle).GetMethod("Reload",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rifle,null));
            yield return new WaitForSeconds(3); Assert.AreEqual(30,rifle.ammo); Assert.AreEqual(119,rifle.reserve);
            var infected=Object.FindObjectsByType<Infected>(FindObjectsSortMode.None);
            foreach(var z in infected) { z.enabled=false; z.GetComponent<NavMeshAgent>().enabled=false; }
            var target=infected[0]; target.transform.position=player.transform.position+player.transform.forward*3;
            Physics.SyncTransforms();
            target.transform.rotation=Quaternion.LookRotation(-player.transform.forward);
            target.animator.Play("Locomotion",0,0);target.animator.Update(.05f);
            var head=target.animator.GetBoneTransform(HumanBodyBones.Head);
            player.enabled=false;player.view.transform.LookAt(head.position+Vector3.up*.04f);
            rifle.weapons[1].spread=0;rifle.weapons[1].aimSpread=0;
            Assert.IsTrue(rifle.TryFire());yield return new WaitForSeconds(.12f);
            if(!target.Dead){player.view.transform.LookAt(head.position);Assert.IsTrue(rifle.TryFire());}
            Assert.LessOrEqual(target.health,0,"Real head hitscan must kill the infected");player.enabled=true;
            Assert.IsNotNull(rifle.muzzleFlash,"Weapon must include a muzzle effect");
            boxes[0].Reveal(); Assert.Greater(boxes[0].Contents.Count,0); Assert.IsTrue(boxes[0].Take(0,app.Session.Bag));
            int value=app.Session.Bag.TotalValue; Assert.Greater(value,0);
            Call(app,"Drop",app.Session.Bag.Entries[0]); Assert.AreEqual(0,app.Session.Bag.Entries.Count);
            var dropped=Object.FindFirstObjectByType<DroppedItem>(); Assert.NotNull(dropped); Assert.IsTrue(dropped.Take(app.Session.Bag)); Assert.AreEqual(value,app.Session.Bag.TotalValue);
            var zone=Object.FindFirstObjectByType<ExtractionZone>(); var cc=player.GetComponent<CharacterController>(); cc.enabled=false; player.transform.position=zone.transform.position; cc.enabled=true;
            yield return new WaitForSeconds(8.5f);
            Assert.IsNull(app.Player,"Extraction must return to lobby"); Assert.AreEqual(1,app.Session.Progress.successfulRaids); Assert.AreEqual(1,app.Session.Progress.stash.Count);
            var saved=new PlayerPrefsStore(Prefix).Load(); Assert.AreEqual(1,saved.successfulRaids); Assert.AreEqual(1,saved.stash.Count);
            Call(app,"Begin"); for(int i=0;i<100 && !app.Player;i++) yield return new WaitForSeconds(.1f);
            app.Session.Bag.Add("phone"); app.Damage(100); yield return null;
            Assert.AreEqual(1,app.Session.Progress.deaths); Assert.AreEqual(1,app.Session.Progress.stash.Count); Assert.AreEqual(0,app.Session.Bag.Entries.Count);
            Debug.Log("TWINKLE_OPS_LIVE_RAID_TEST_OK");
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(testKeyboard!=null && testKeyboard.added) InputSystem.RemoveDevice(testKeyboard);
            InputSystem.settings.backgroundBehavior=background;
            InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            if(GameApp.Instance) Object.Destroy(GameApp.Instance.gameObject);
            GameApp.ProgressStoreFactory=null;
            PlayerPrefs.DeleteKey(Prefix+0); PlayerPrefs.DeleteKey(Prefix+1); PlayerPrefs.Save();
            yield return null;
        }
    }
}
