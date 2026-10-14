using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace TwinkleOps.Editor
{
    public static class V11Daylight
    {
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.transform.rotation=Quaternion.Euler(53,-32,0);sun.color=new Color(1,.965f,.91f);sun.intensity=1.55f;sun.shadowStrength=.75f;sun.shadowBias=.035f;sun.shadowNormalBias=.25f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.58f,.69f,.82f);RenderSettings.ambientEquatorColor=new Color(.43f,.48f,.52f);RenderSettings.ambientGroundColor=new Color(.28f,.30f,.31f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=245;RenderSettings.fogColor=new Color(.68f,.78f,.85f);
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/TwinkleOps/Content/OvercastSky.mat");sky.SetColor("_SkyTint",new Color(.5f,.63f,.8f));sky.SetColor("_GroundColor",new Color(.43f,.48f,.50f));sky.SetFloat("_AtmosphereThickness",.75f);sky.SetFloat("_Exposure",1.15f);RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);
            var concrete=AssetDatabase.LoadAssetAtPath<Material>(OpenAssets.Root+"/Buildings/ConcreteAtlas.mat");concrete.SetColor("_BaseColor",new Color(.98f,.98f,.95f));concrete.DisableKeyword("_DETAIL_SCALED");concrete.SetFloat("_DetailAlbedoMapScale",0);concrete.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(concrete);
            var road=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/PolyHaven/asphalt_02/Asphalt.mat");road.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(road);
            var existing=GameObject.Find("Daylight grading");var volume=existing?existing.GetComponent<Volume>():new GameObject("Daylight grading").AddComponent<Volume>();volume.isGlobal=true;
            const string path="Assets/TwinkleOps/Content/V11/Daylight.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
            profile.components.RemoveAll(c=>!c);
            if(!profile.TryGet<ColorAdjustments>(out var color))color=profile.Add<ColorAdjustments>(true);
            if(!AssetDatabase.Contains(color))AssetDatabase.AddObjectToAsset(color,profile);
            color.postExposure.Override(.12f);color.contrast.Override(5);color.saturation.Override(3);
            if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
            if(!AssetDatabase.Contains(tone))AssetDatabase.AddObjectToAsset(tone,profile);
            volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
            // Daylight is lit in the scene; keep the Web camera free of a full-screen post-process pass.
            var player=Object.FindFirstObjectByType<PlayerMotor>();player.view.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            foreach(var id in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(id));pipeline.shadowDistance=65;pipeline.msaaSampleCount=2;pipeline.renderScale=1;EditorUtility.SetDirty(pipeline);}
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            V11District.Review();
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");player=Object.FindFirstObjectByType<PlayerMotor>();RaidReview.Shot(player.view,"v11-daylight-firstperson");
            File.WriteAllText("Logs/v11-daylight-settings.txt","Neutral daylight, 53-degree sun, lifted ambient shadows, linear distance haze 100–245m, reduced building grime detail and road gloss. Original licensed architectural models and PBR textures retained.\n");
            Debug.Log("V11_DAYLIGHT_READY");
        }
    }
}
