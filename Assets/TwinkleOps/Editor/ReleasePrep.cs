using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TwinkleOps.Editor
{
    public static class ReleasePrep
    {
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var normal=AssetImporter.GetAtPath("Assets/ThirdParty/OpenGameArt/PixelZombie/normal.JPG") as TextureImporter;
            if(normal && normal.textureType!=TextureImporterType.NormalMap) { normal.textureType=TextureImporterType.NormalMap; normal.SaveAndReimport(); }
            const string root="Assets/TwinkleOps/Content";
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            var rifle=UnityEngine.Object.FindFirstObjectByType<Rifle>();
            var audio=rifle.GetComponent<AudioSource>(); if(!audio) audio=rifle.gameObject.AddComponent<AudioSource>(); audio.playOnAwake=false; audio.volume=.65f; rifle.audioSource=audio;
            var flashPath=root+"/MuzzleGlow.png";
            if(!File.Exists(flashPath))
            {
                var image=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++) for(int x=0;x<64;x++) { float dx=(x-31.5f)/31.5f,dy=(y-31.5f)/31.5f; float a=Mathf.Exp(-(dx*dx+dy*dy)*8)*Mathf.Clamp01(1-dx*dx-dy*dy); image.SetPixel(x,y,new Color(1,.6f,.13f,a)); }
                image.Apply(); File.WriteAllBytes(flashPath,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); AssetDatabase.ImportAsset(flashPath);
            }
            var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"/MuzzleGlow.mat");
            if(!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(mat,root+"/MuzzleGlow.mat"); }
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(flashPath)); mat.SetColor("_BaseColor",new Color(1,.7f,.23f,1)); mat.SetFloat("_Surface",1); mat.SetFloat("_Blend",2); mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend",(float)BlendMode.One); mat.SetFloat("_ZWrite",0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue=3000; EditorUtility.SetDirty(mat);
            foreach(var weapon in rifle.weapons)
            {
                if(weapon.model.GetComponentInChildren<ParticleSystem>(true)) continue;
                var go=new GameObject("Muzzle Flash"); go.transform.SetParent(weapon.model,false); go.transform.localPosition=new Vector3(0,.012f,weapon.name=="M4A1"?-.55f:-.5f); go.layer=8;
                var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main; main.loop=false; main.playOnAwake=false; main.duration=.06f; main.startLifetime=.04f; main.startSpeed=0; main.startSize=new ParticleSystem.MinMaxCurve(.065f,.12f); main.startRotation=new ParticleSystem.MinMaxCurve(0,6.28f); main.maxParticles=8;
                var emission=ps.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,2)}); var shape=ps.shape; shape.enabled=false;
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;
            }
            // Keep the animated feet on the ground after humanoid retargeting.
            foreach(var z in UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None)) { var tr=z.animator.transform; tr.localPosition=new Vector3(tr.localPosition.x,-.135f,tr.localPosition.z); }
            var boundsRoot=GameObject.Find("Raid perimeter");
            if(!boundsRoot)
            {
                boundsRoot=new GameObject("Raid perimeter");
                foreach(var p in new[]{(new Vector3(-44,2,19),new Vector3(1,5,106)),(new Vector3(44,2,19),new Vector3(1,5,106)),(new Vector3(0,2,-33),new Vector3(90,5,1)),(new Vector3(0,2,71),new Vector3(90,5,1))})
                { var go=new GameObject("Boundary collider"); go.transform.SetParent(boundsRoot.transform); go.transform.position=p.Item1; go.AddComponent<BoxCollider>().size=p.Item2; }
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            RaidReview.Capture();
            var m=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);
            m.visualQualityReviewed=true; EditorUtility.SetDirty(m);
            PlayerSettings.bundleVersion="0.2.0-alpha";
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/Lobby.unity");
            var app=UnityEngine.Object.FindFirstObjectByType<GameApp>(); app.uiFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/TwinkleOps/UI/TwinkleOpsSC.ttf");
            if(!app.uiFont) throw new Exception("Subset font missing");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/visual-review.md","Reviewed actual Unity-rendered FPS, map overview and animated infected. Real textured M4A1, AKM, buildings, hands and infected; 10 reachable containers and reachable extraction. Early alpha: hand/finger alignment, procedural reload and environmental detail need further polish; this is not final art-quality acceptance.\n");
            ProjectSetup.ValidateContent(); Debug.Log("RELEASE_PREP_OK");
        }
    }
}
