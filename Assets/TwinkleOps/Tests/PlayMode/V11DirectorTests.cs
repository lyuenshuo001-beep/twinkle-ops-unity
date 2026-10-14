using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TwinkleOps.Tests
{
    public class V11DirectorTests
    {
        const string Prefix="TwinkleOps.director.qa.";
        static void Wave(InfectedDirector d){typeof(InfectedDirector).GetField("nextWave",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,0f);typeof(InfectedDirector).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);}
        [UnityTest] public IEnumerator SpawnsOutsideViewAndReusesDeadInfected()
        {
            GameApp.ProgressStoreFactory=()=>new PlayerPrefsStore(Prefix);PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);
            yield return SceneManager.LoadSceneAsync("Lobby");yield return null;typeof(GameApp).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(GameApp.Instance,null);yield return null;yield return new WaitForSeconds(.3f);
            var p=GameApp.Instance.Player;p.enabled=false;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(0,.1f,20);cc.enabled=true;p.view.transform.rotation=Quaternion.identity;
            var d=Object.FindAnyObjectByType<InfectedDirector>();d.enabled=false;
            var all=Object.FindObjectsByType<Infected>(FindObjectsInactive.Include,FindObjectsSortMode.None);Assert.AreEqual(d.poolSize,all.Length);
            foreach(var z in all){z.enabled=false;z.gameObject.SetActive(false);}
            d.spawnPoints=new[]{new Vector3(0,0,-5)};Wave(d);Assert.AreEqual(1,d.Spawned);Assert.AreEqual(1,d.ActiveCount);
            var spawned=Object.FindAnyObjectByType<Infected>();var id=spawned.GetEntityId();
            d.spawnPoints=new[]{new Vector3(0,0,45)};Wave(d);Assert.AreEqual(1,d.Spawned,"Must not pop into unobstructed camera view");
            spawned.TakeDamage(1000);typeof(Infected).GetField("deathAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(spawned,0f);spawned.enabled=true;yield return null;Assert.IsFalse(spawned.gameObject.activeSelf);
            d.spawnPoints=new[]{new Vector3(0,0,-5)};Wave(d);Assert.AreEqual(2,d.Spawned);Assert.AreEqual(id,Object.FindAnyObjectByType<Infected>().GetEntityId());
            Assert.AreEqual(d.poolSize,Object.FindObjectsByType<Infected>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);
            Debug.Log("V11_DIRECTOR_POOL_AND_VISIBILITY_OK");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(GameApp.Instance)Object.Destroy(GameApp.Instance.gameObject);GameApp.ProgressStoreFactory=null;PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);yield return null;}
    }
}
