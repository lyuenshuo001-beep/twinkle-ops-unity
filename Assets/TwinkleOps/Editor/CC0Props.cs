using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TwinkleOps.Editor
{
    public static class CC0Props
    {
        static readonly string[] Ids={"concrete_road_barrier","barrel_03","old_military_crate"};
        const string Root="Assets/ThirdParty/PolyHaven";
        public static void PrepareAssets() { Import(); Preview(); ProjectSetup.Initialize(); }
        [MenuItem("Twinkle Ops/Import Downloaded CC0 Props")]
        public static void Import()
        {
            foreach(string id in Ids)
            {
                string src="SourceAssets/PolyHaven/"+id, dst=Root+"/"+id;
                if(!Directory.Exists(src)) throw new FileNotFoundException("Run Tools/Fetch-CC0Props.ps1 first: "+src);
                Directory.CreateDirectory(dst);
                foreach(string file in Directory.GetFiles(src))
                { string target=dst+"/"+Path.GetFileName(file); if(!File.Exists(target)) File.Copy(file,target); }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string id in Ids)
            {
                string folder=Root+"/"+id;
                var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"/model.fbx");
                importer.materialImportMode=ModelImporterMaterialImportMode.None; importer.SaveAndReimport();
                foreach(string name in new[]{"nor_gl","Rough","Metal","AO"})
                {
                    var texture=AssetImporter.GetAtPath(folder+"/"+name+".jpg") as TextureImporter;
                    if(!texture) continue;
                    texture.sRGBTexture=false; texture.maxTextureSize=2048;
                    if(name=="nor_gl") texture.textureType=TextureImporterType.NormalMap;
                    texture.SaveAndReimport();
                }
                var rough=Read(folder+"/Rough.jpg"); var metal=File.Exists(folder+"/Metal.jpg")?Read(folder+"/Metal.jpg"):null;
                var pixels=rough.GetPixels32(); var metals=metal?metal.GetPixels32():null;
                for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(metals==null?(byte)0:metals[i].r,0,0,(byte)(255-pixels[i].r));
                var packed=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true); packed.SetPixels32(pixels); packed.Apply();
                string packedPath=folder+"/MetallicSmoothness.png"; File.WriteAllBytes(packedPath,packed.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(rough); if(metal) UnityEngine.Object.DestroyImmediate(metal); UnityEngine.Object.DestroyImmediate(packed);
                AssetDatabase.ImportAsset(packedPath,ImportAssetOptions.ForceSynchronousImport);
                var packImporter=(TextureImporter)AssetImporter.GetAtPath(packedPath); packImporter.sRGBTexture=false; packImporter.SaveAndReimport();
                string materialPath=folder+"/URP.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,materialPath); }
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Diffuse.jpg"));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/nor_gl.jpg")); material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath)); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness",1);
                var ao=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/AO.jpg"); if(ao) { material.SetTexture("_OcclusionMap",ao); material.EnableKeyword("_OCCLUSIONMAP"); }
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/model.fbx");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                instance.name=id;
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                // Invisible interaction/physics volume; the visible object remains the authored FBX mesh.
                var renderers=instance.GetComponentsInChildren<Renderer>();
                var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var collider=instance.AddComponent<BoxCollider>(); collider.center=instance.transform.InverseTransformPoint(bounds.center);
                var scale=instance.transform.lossyScale; collider.size=new Vector3(bounds.size.x/Mathf.Abs(scale.x),bounds.size.y/Mathf.Abs(scale.y),bounds.size.z/Mathf.Abs(scale.z));
                if(id=="old_military_crate") instance.AddComponent<LootContainer>();
                PrefabUtility.SaveAsPrefabAsset(instance,folder+"/"+id+".prefab"); UnityEngine.Object.DestroyImmediate(instance); EditorUtility.SetDirty(material);
            }
            var manifest=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);
            if(manifest && !manifest.searchContainer)
            { manifest.searchContainer=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/old_military_crate/old_military_crate.prefab"); EditorUtility.SetDirty(manifest); }
            AssetDatabase.SaveAssets();
            Debug.Log("TWINKLE_OPS_CC0_PROPS_IMPORTED: 3 licensed real models, PBR converted to URP.");
        }
        static Texture2D Read(string path)
        { var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true); if(!t.LoadImage(File.ReadAllBytes(path))) throw new Exception("Unreadable texture: "+path); return t; }

        public static void Preview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Bounds bounds=new(); bool first=true; float x=0;
            foreach(string id in Ids)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+id+"/"+id+".prefab");
                if(!prefab) throw new Exception("Missing imported prefab: "+id);
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds b=obj.GetComponentsInChildren<Renderer>()[0].bounds;
                foreach(var r in obj.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                obj.transform.position+=new Vector3(x-b.min.x,-b.min.y,-b.center.z); x+=b.size.x+.4f;
                foreach(var r in obj.GetComponentsInChildren<Renderer>()) { if(first) { bounds=r.bounds; first=false; } else bounds.Encapsulate(r.bounds); }
            }
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.5f,.52f,.55f);
            var light=new GameObject("Preview Key").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2.1f; light.transform.rotation=Quaternion.Euler(48,-32,0);
            var cam=new GameObject("Preview Camera").AddComponent<Camera>(); cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.06f,.075f,.09f);
            cam.transform.position=bounds.center+new Vector3(0,bounds.size.x*.3f,-bounds.size.x*.9f); cam.transform.LookAt(bounds.center); cam.fieldOfView=45;
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32); rt.Create();
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest { destination=rt });
            var old=RenderTexture.active; RenderTexture.active=rt;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/cc0-props-preview.png",image.EncodeToPNG());
            RenderTexture.active=old; UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("TWINKLE_OPS_CC0_PREVIEW_RENDERED");
        }
    }
}
