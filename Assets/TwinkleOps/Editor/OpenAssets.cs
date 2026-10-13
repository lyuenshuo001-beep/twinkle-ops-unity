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
    public static class OpenAssets
    {
        public const string Root="Assets/ThirdParty/OpenGameArt";
        public static Bounds BoundsOf(GameObject obj)
        {
            var rs=obj.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds;
            foreach(var r in rs) b.Encapsulate(r.bounds); return b;
        }
        public static Material Pbr(string folder,string name,string diffuse,string normal=null,string rough=null,string metal=null)
        {
            var path=folder+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.name=name; m.SetColor("_BaseColor",Color.white); m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+diffuse));
            if(normal!=null) { m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+normal)); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness",.28f);
            if(rough!=null)
            {
                var r=new Texture2D(2,2,TextureFormat.RGBA32,false,true); r.LoadImage(File.ReadAllBytes(folder+"/"+rough));
                var p=r.GetPixels32(); Texture2D mt=null;
                if(metal!=null) { mt=new Texture2D(2,2,TextureFormat.RGBA32,false,true); mt.LoadImage(File.ReadAllBytes(folder+"/"+metal)); }
                var mp=mt?mt.GetPixels32():null;
                for(int i=0;i<p.Length;i++) p[i]=new Color32(mp==null?(byte)0:mp[i].r,0,0,(byte)(255-p[i].r));
                r.SetPixels32(p); r.Apply(); var packed=folder+"/"+name+"_Packed.png"; File.WriteAllBytes(packed,r.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(r); if(mt) UnityEngine.Object.DestroyImmediate(mt);
                AssetDatabase.ImportAsset(packed,ImportAssetOptions.ForceSynchronousImport);
                var ti=(TextureImporter)AssetImporter.GetAtPath(packed); ti.sRGBTexture=false; ti.maxTextureSize=2048; ti.SaveAndReimport();
                m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed)); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness",.8f);
            }
            EditorUtility.SetDirty(m); return m;
        }
        public static void Import()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var file in Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(file); ti.maxTextureSize=2048;
                if(file.ToLowerInvariant().Contains("normal")) ti.textureType=TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
            var m4=Pbr(Root+"/M4A1","M4A1_PBR","M4A1_Base_Color.png","M4A1_Normal.png","M4A1_Roughness.png","M4A1_Metallic.png");
            var ak=Pbr(Root+"/AKM","AKM_PBR","Material_BaseColor.png","Material_Normal.png","Material_Roughness.png","Material_Metallic.png");
            var building=Pbr(Root+"/Buildings","ConcreteAtlas","UnfinishedAtlas_1k_Base Color.png","UnfinishedAtlas_1k_NORMAL.png","UnfinishedAtlas_1k_ROUGHNESS.png","UnfinishedAtlas_1k_METALLIC.png");
            var arms=Pbr(Root+"/Arms","OperatorSkin","new_diff.png");
            string z=Root+"/CityZombie";
            var body=Pbr(z,"InfectedSkin",Path.GetFileName(Directory.GetFiles(z,"*Body1_BaseColor*")[0]),Path.GetFileName(Directory.GetFiles(z,"*Body1_Normal*")[0]));
            var outfit=Pbr(z,"InfectedOutfit",Path.GetFileName(Directory.GetFiles(z,"*Outfit1_BaseColor*")[0]),Path.GetFileName(Directory.GetFiles(z,"*Outfit1_Normal*")[0]));
            var pixel=Pbr(Root+"/PixelZombie","WorkerInfected","difusse.jpg","normal.JPG"); pixel.SetFloat("_BumpScale",.35f);
            foreach(var file in Directory.GetFiles(Root,"*.fbx",SearchOption.AllDirectories))
            {
                string f=file.Replace('\\','/'); var importer=(ModelImporter)AssetImporter.GetAtPath(f);
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                if(f.Contains("CityZombie") || f.Contains("PixelZombie")) { importer.animationType=ModelImporterAnimationType.Human; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; }
                importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(f); var obj=(GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var r in obj.GetComponentsInChildren<Renderer>())
                {
                    Debug.Log("SOURCE_MATERIAL: "+f+" "+r.name+" "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")));
                    r.sharedMaterials=r.sharedMaterials.Select(m=>f.Contains("M4A1")?m4:f.Contains("AKM")?ak:f.Contains("Buildings")?building:f.Contains("Arms")?arms:f.Contains("PixelZombie")?pixel:(m && m.name.Contains("Outfit")?outfit:body)).ToArray();
                    if(r.name.EndsWith("-colonly")) { r.enabled=false; if(!r.GetComponent<MeshCollider>()) r.gameObject.AddComponent<MeshCollider>(); }
                }
                if(obj.GetComponentsInChildren<Renderer>().Length>0)
                {
                    var b=BoundsOf(obj); Debug.Log("MODEL_BOUNDS: "+f+" center="+b.center.ToString("F3")+" size="+b.size.ToString("F3"));
                    PrefabUtility.SaveAsPrefabAsset(obj,Path.ChangeExtension(f,"prefab"));
                }
                Debug.Log("MODEL_CLIPS: "+f+" "+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(f).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).Select(c=>c.name+":"+c.length)));
                UnityEngine.Object.DestroyImmediate(obj);
            }
            AssetDatabase.SaveAssets(); Debug.Log("OPEN_ASSETS_IMPORTED");
        }
        public static void Preview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            GraphicsSettings.useScriptableRenderPipelineBatching=false;
            ShaderUtil.allowAsyncCompilation=false;
            string[] paths={Root+"/M4A1/M4A1.prefab",Root+"/AKM/AKM.prefab",Root+"/CityZombie/InfectedCityMan.prefab",Root+"/Buildings/Structures_1.prefab"};
            for(int i=0;i<paths.Length;i++)
            {
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]));
                var b=BoundsOf(obj); obj.transform.localScale*=2/Mathf.Max(b.size.x,b.size.y,b.size.z); b=BoundsOf(obj);
                obj.transform.position+=new Vector3(i*2.5f-b.center.x,-b.min.y,-b.center.z);
            }
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=Color.gray;
            var light=new GameObject("Light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2; light.transform.rotation=Quaternion.Euler(45,-25,0);
            var cam=new GameObject("Camera").AddComponent<Camera>(); cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.05f,.065f,.08f);
            cam.transform.position=new Vector3(3.75f,4,-11); cam.transform.LookAt(new Vector3(3.75f,.7f,0)); cam.fieldOfView=48;
            var rt=new RenderTexture(1800,900,24); rt.Create();
            for(int i=0;i<4;i++) RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest { destination=rt });
            RenderTexture.active=rt; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); tex.Apply();
            File.WriteAllBytes("Logs/open-assets-preview.png",tex.EncodeToPNG()); RenderTexture.active=null; rt.Release();
            Debug.Log("OPEN_ASSETS_PREVIEW_OK");
        }
        public static void InspectModels()
        {
            string[] paths={Root+"/PixelZombie/walk.prefab",Root+"/Buildings/Structures_2.prefab",Root+"/Buildings/Structures_8.prefab",Root+"/Arms/FPS ARMS RIG 1.prefab"};
            foreach(var path in paths)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                GraphicsSettings.useScriptableRenderPipelineBatching=false; ShaderUtil.allowAsyncCompilation=false;
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)); var b=BoundsOf(obj);
                RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.5f,.52f,.55f);
                var light=new GameObject("Light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2; light.transform.rotation=Quaternion.Euler(45,-25,0);
                var cam=new GameObject("Camera").AddComponent<Camera>(); cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.05f,.065f,.08f);
                float size=Mathf.Max(b.size.x,b.size.y,b.size.z); cam.transform.position=b.center+new Vector3(-1,.65f,-1.1f)*size; cam.transform.LookAt(b.center); cam.fieldOfView=48;
                var rt=new RenderTexture(1100,800,24); rt.Create();
                for(int i=0;i<4;i++) RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest { destination=rt });
                RenderTexture.active=rt; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); tex.Apply();
                File.WriteAllBytes("Logs/inspect-"+Path.GetFileNameWithoutExtension(path)+".png",tex.EncodeToPNG()); RenderTexture.active=null; rt.Release();
                foreach(var t in obj.GetComponentsInChildren<Transform>()) if(path.Contains("Arms") && (t.name.Contains("arm")||t.name.Contains("hand")||t.name.Contains("clavicle"))) Debug.Log("ARM_BONE "+t.name+" "+t.position.ToString("F3"));
            }
        }
    }
}
