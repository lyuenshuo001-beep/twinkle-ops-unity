using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
namespace TwinkleOps.Tests
{
    public class V11VisualTests
    {
        const string Prefix="TwinkleOps.visual.qa.";
        static void Capture(Camera cam,string name)
        {
            var rt=new RenderTexture(1280,800,24);rt.Create();
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes("Logs/"+name+".png",tex.EncodeToPNG());RenderTexture.active=null;rt.Release();Object.Destroy(rt);Object.Destroy(tex);
        }
        [UnityTest] public IEnumerator CaptureActualRunningScene()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null){Assert.Ignore("Rendered QA requires a graphics device; run separately without -nographics");yield break;}
            GameApp.ProgressStoreFactory=()=>new PlayerPrefsStore(Prefix);PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);
            yield return SceneManager.LoadSceneAsync("Lobby");yield return null;typeof(GameApp).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(GameApp.Instance,null);yield return null;yield return new WaitForSeconds(1);
            var p=GameApp.Instance.Player;var rifle=p.GetComponent<Rifle>();var director=Object.FindAnyObjectByType<InfectedDirector>();director.enabled=false;
            var zombies=Object.FindObjectsByType<Infected>(FindObjectsSortMode.None);foreach(var enemy in zombies)enemy.gameObject.SetActive(false);
            for(int i=0;i<4;i++){rifle.Equip(i);yield return new WaitForSeconds(.45f);Capture(p.view,"v11-live-weapon-"+i);}
            rifle.Equip(2);yield return new WaitForSeconds(.35f);rifle.TryFire();yield return new WaitForSeconds(.21f);Capture(p.view,"v11-live-knife-swing");
            var z=zombies[0];z.ResetForSpawn(p.transform.position+Vector3.forward*2.8f,p.transform);z.enabled=false;z.GetComponent<NavMeshAgent>().isStopped=true;z.transform.rotation=Quaternion.LookRotation(-Vector3.forward);
            rifle.Equip(0);yield return new WaitForSeconds(.3f);z.animator.SetFloat("Speed",4.1f);yield return new WaitForSeconds(.3f);Capture(p.view,"v11-live-infected-run");
            z.animator.Play("AttackA",0,.35f);yield return null;Capture(p.view,"v11-live-infected-attack");
            CombatDebug.Visible=true;yield return new WaitForSeconds(.15f);Capture(p.view,"v11-live-hitboxes");CombatDebug.Visible=false;
            Debug.Log("V11_RENDERED_SCENE_CAPTURED");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {CombatDebug.Visible=false;if(GameApp.Instance)Object.Destroy(GameApp.Instance.gameObject);GameApp.ProgressStoreFactory=null;PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);yield return null;}
    }
}
