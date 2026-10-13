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
    public static class RaidPolish
    {
        public static void FixAndCapture()
        {
            // This edits only the new scene authored in this task, preserving its meta GUID.
            RaidContent.Prepare();
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            var m=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();
            var env=GameObject.Find("QuarantineDistrict");
            if(!env) env=UnityEngine.Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>().gameObject;
            // Refresh prefab changes from Prepare; scene root remains the existing instance.
            foreach(var r in env.GetComponentsInChildren<Renderer>()) if(r.name.EndsWith("-colonly")) r.enabled=false;
            NavMesh.SamplePosition(player.transform.position,out var start,3,NavMesh.AllAreas);
            foreach(var box in UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None))
            {
                var origin=box.transform.position; bool placed=false;
                for(int ring=0;ring<=7 && !placed;ring++) for(int i=0;i<(ring==0?1:12) && !placed;i++)
                {
                    var probe=origin+new Vector3(Mathf.Sin(i*Mathf.PI/6)*ring,0,Mathf.Cos(i*Mathf.PI/6)*ring);
                    if(!NavMesh.SamplePosition(probe,out var hit,2,NavMesh.AllAreas)) continue;
                    var path=new NavMeshPath();
                    if(!NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete) continue;
                    box.transform.position=hit.position+Vector3.up*.025f; placed=true;
                }
                if(!placed) throw new Exception("Cannot place reachable container "+box.name);
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(OpenAssets.Root+"/Buildings/ConcreteAtlas.mat");
            material.SetColor("_BaseColor",new Color(.65f,.65f,.61f));
            material.SetTexture("_DetailAlbedoMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHaven/concrete_road_barrier/Diffuse.jpg")); material.SetTextureScale("_DetailAlbedoMap",new Vector2(8,8)); material.SetFloat("_DetailAlbedoMapScale",.55f); material.EnableKeyword("_DETAIL_SCALED"); EditorUtility.SetDirty(material);
            var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional); sun.intensity=1.05f; sun.color=new Color(1,.89f,.71f); sun.transform.rotation=Quaternion.Euler(27,-38,0);
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.31f,.36f,.4f); RenderSettings.ambientEquatorColor=new Color(.17f,.2f,.21f); RenderSettings.ambientGroundColor=new Color(.1f,.11f,.1f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.009f; RenderSettings.fogColor=new Color(.22f,.27f,.29f);
            var cam=player.view; cam.clearFlags=CameraClearFlags.Skybox;
            var sky=new Material(Shader.Find("Skybox/Procedural")); sky.SetColor("_SkyTint",new Color(.37f,.43f,.48f)); sky.SetColor("_GroundColor",new Color(.15f,.18f,.17f)); sky.SetFloat("_AtmosphereThickness",1.2f); sky.SetFloat("_Exposure",.65f);
            var skyPath="Assets/TwinkleOps/Content/OvercastSky.mat"; var oldSky=AssetDatabase.LoadAssetAtPath<Material>(skyPath); if(oldSky) { EditorUtility.CopySerialized(sky,oldSky); sky=oldSky; } else AssetDatabase.CreateAsset(sky,skyPath); RenderSettings.skybox=sky;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            RaidReview.Capture();
        }
    }
}
