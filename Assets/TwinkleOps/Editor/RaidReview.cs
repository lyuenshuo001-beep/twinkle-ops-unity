using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TwinkleOps.Editor
{
    public static class RaidReview
    {
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            ShaderUtil.allowAsyncCompilation=false; GraphicsSettings.useScriptableRenderPipelineBatching=false;
            var rifle=UnityEngine.Object.FindFirstObjectByType<Rifle>(); rifle.Equip(0,false);
            foreach(var a in UnityEngine.Object.FindObjectsByType<ViewArms>(FindObjectsSortMode.None)) a.Pose();
            foreach(var a in UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None)) { a.animator.Rebind(); a.animator.Play("Idle"); a.animator.Update(.3f); }
            var cam=rifle.motor.view; cam.backgroundColor=RenderSettings.fogColor; cam.clearFlags=CameraClearFlags.SolidColor;
            Shot(cam,"raid-firstperson");
            var camera=new GameObject("Review Camera").AddComponent<Camera>(); camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RenderSettings.fogColor;
            camera.transform.position=new Vector3(-43,48,-49); camera.transform.LookAt(new Vector3(0,0,18)); camera.fieldOfView=60; Shot(camera,"raid-overview");
            var z=UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None)[0]; camera.transform.position=z.transform.position+new Vector3(-2.2f,1.3f,2.4f); camera.transform.LookAt(z.transform.position+Vector3.up); camera.fieldOfView=48; Shot(camera,"raid-infected");
            var spawn=rifle.motor.transform.position;
            if(!NavMesh.SamplePosition(spawn,out var start,3,NavMesh.AllAreas)) throw new Exception("Player starts off the NavMesh");
            int reachable=0;
            foreach(var box in UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None))
            {
                var path=new NavMeshPath(); bool ok=NavMesh.SamplePosition(box.transform.position,out var hit,2,NavMesh.AllAreas) && NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete;
                Debug.Log("CRATE_ROUTE: "+box.displayName+" reachable="+ok+" at="+box.transform.position); if(ok) reachable++;
            }
            var exit=UnityEngine.Object.FindFirstObjectByType<ExtractionZone>(); var route=new NavMeshPath();
            bool exitReachable=NavMesh.SamplePosition(exit.transform.position,out var end,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,route)&&route.status==NavMeshPathStatus.PathComplete;
            File.WriteAllText("Logs/raid-routes.json","{\"reachableCrates\":"+reachable+",\"extractionReachable\":"+exitReachable.ToString().ToLowerInvariant()+"}");
            if(!exitReachable || reachable<10) throw new Exception("Scene has unreachable objectives; inspect Logs/raid-routes.json");
            Debug.Log("RAID_ROUTES_OK");
        }
        public static void Shot(Camera cam,string name)
        {
            var rt=new RenderTexture(1440,900,24); rt.Create();
            for(int i=0;i<4;i++) RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest { destination=rt });
            RenderTexture.active=rt; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); tex.Apply();
            File.WriteAllBytes("Logs/"+name+".png",tex.EncodeToPNG()); RenderTexture.active=null; rt.Release(); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
