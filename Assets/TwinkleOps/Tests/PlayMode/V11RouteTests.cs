using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TwinkleOps.Tests
{
    public class V11RouteTests
    {
        const string Prefix="TwinkleOps.routes.qa.";
        [UnityTest] public IEnumerator ControllerCanReachNewDistrictsAndUpperFloor()
        {
            PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);GameApp.ProgressStoreFactory=()=>new PlayerPrefsStore(Prefix);
            yield return SceneManager.LoadSceneAsync("Lobby");yield return null;
            typeof(GameApp).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(GameApp.Instance,null);yield return null;yield return null;
            var p=GameApp.Instance.Player;p.enabled=false;Object.FindAnyObjectByType<InfectedDirector>().enabled=false;
            foreach(var z in Object.FindObjectsByType<Infected>(FindObjectsSortMode.None))z.gameObject.SetActive(false);
            var controller=p.GetComponent<CharacterController>();Vector3 origin=p.transform.position;
            var targets=new[]{new Vector3(52,0,15),new Vector3(0,0,79),new Vector3(-63,-4,6)}.ToList();
            var upper=GameObject.Find("Clinic upper floor waypoint");Assert.NotNull(upper);Assert.Greater(upper.transform.position.y,6);targets.Add(upper.transform.position);
            Assert.AreEqual(4,targets.Count,"Clinic must have a reachable upper floor via stairs");
            foreach(var target in targets)
            {
                controller.enabled=false;p.transform.position=origin;controller.enabled=true;
                Assert.IsTrue(NavMesh.SamplePosition(target,out var end,3,NavMesh.AllAreas));var path=new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(origin,end.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);
                for(int c=1;c<path.corners.Length;c++)
                {
                    int frames=0,stalled=0;var corner=path.corners[c];
                    while(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(corner.x,corner.z))>.32f && frames++<3000)
                    {
                        Vector3 d=corner-p.transform.position;d.y=0;var before=p.transform.position;
                        controller.Move(Vector3.ClampMagnitude(d,.095f)+Vector3.down*.07f);
                        if(Vector3.Distance(before,p.transform.position)<.005f)stalled++;else stalled=0;
                        Assert.Less(stalled,25,"CharacterController blocked on route to "+target+" at "+p.transform.position+" next corner "+corner);yield return null;
                    }
                    Assert.Less(frames,3000,"Route did not converge");
                }
                Assert.Less(Vector3.Distance(p.transform.position,end.position),1.6f,"Must physically arrive on correct level");
                Debug.Log("V11_PHYSICAL_ROUTE_OK "+target+" actual="+p.transform.position);
            }
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(GameApp.Instance)Object.Destroy(GameApp.Instance.gameObject);GameApp.ProgressStoreFactory=null;PlayerPrefs.DeleteKey(Prefix+0);PlayerPrefs.DeleteKey(Prefix+1);yield return null;}
    }
}
