using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TwinkleOps.Editor
{
    public static class V11Props
    {
        const string Art="Assets/ThirdParty/V11",Root="Assets/TwinkleOps/Content/V11";
        static T Load<T>(string p) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(p);
        static GameObject Model(string path)
        {
            var o=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);return o;
        }
        public static GameObject Prop(string id,float size,ContainerKind kind,string label)
        {
            string folder=Art+"/"+id;var model=Model(folder+"/"+(id=="Safe"?"Safe.fbx":"model.fbx"));
            foreach(var r in model.GetComponentsInChildren<Renderer>().ToArray())
            { string n=r.name.ToLowerInvariant();if(n.Contains("lod") && !n.Contains("lod0") && !n.Contains("lod_0"))UnityEngine.Object.DestroyImmediate(r.gameObject); }
            var mat=OpenAssets.Pbr(folder,id,id=="Safe"?"diff.png":"diff.jpg",File.Exists(folder+"/normal.jpg")?"normal.jpg":null,id=="Safe"?"rough.png":"rough.jpg",File.Exists(folder+"/metal.jpg")?"metal.jpg":id=="Safe"?"metal.png":null);
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
            var b=OpenAssets.BoundsOf(model);model.transform.localScale*=size/Mathf.Max(b.size.x,b.size.y,b.size.z);b=OpenAssets.BoundsOf(model);
            var root=new GameObject(label);model.transform.SetParent(root.transform,false);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            b=OpenAssets.BoundsOf(root);var box=root.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;
            if(!string.IsNullOrEmpty(label)) { var loot=root.AddComponent<LootContainer>();loot.kind=kind;loot.displayName=label;loot.searchSeconds=kind==ContainerKind.Safe?4:kind==ContainerKind.TrashCan?1.3f:2; }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+id+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static Transform Weapon(Rifle rifle,string name,string path,float length,Vector3 rotation)
        {
            var root=new GameObject(name+" View");root.transform.SetParent(rifle.motor.view.transform,false);
            var model=Model(path);model.transform.SetParent(root.transform,false);model.transform.localRotation=Quaternion.Euler(rotation)*model.transform.localRotation;
            var b=OpenAssets.BoundsOf(model);model.transform.localScale*=length/Mathf.Max(b.size.x,b.size.y,b.size.z);b=OpenAssets.BoundsOf(model);
            model.transform.position+=root.transform.position-b.center;
            root.transform.localPosition=new Vector3(.18f,-.2f,.42f);
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            return root.transform;
        }
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var f in Directory.GetFiles(Art,"*",SearchOption.AllDirectories).Where(f=>f.EndsWith(".jpg")||f.EndsWith(".png")))
            {
                var t=AssetImporter.GetAtPath(f) as TextureImporter;if(t==null)continue;
                t.maxTextureSize=f.Contains("Knife")?1024:1024;t.textureCompression=TextureImporterCompression.Compressed;
                if(f.Contains("normal")||f.EndsWith("_nm.png"))t.textureType=TextureImporterType.NormalMap;
                t.SaveAndReimport();
            }
            var props=new[]{Prop("metal_trash_can",.95f,ContainerKind.TrashCan,"街边垃圾桶"),Prop("vintage_suitcase",.7f,ContainerKind.Suitcase,"旅行行李箱"),Prop("metal_toolbox",.5f,ContainerKind.Toolbox,"维修工具箱"),Prop("trashbag",.65f,ContainerKind.TrashBag,"遗弃垃圾袋"),Prop("Safe",1,ContainerKind.Safe,"机械保险箱")};
            Prop("old_bed_frame",2.1f,ContainerKind.Military,"");Prop("Shelf_01",2.1f,ContainerKind.Military,"");Prop("tool_cart",1,ContainerKind.Military,"");
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            var rifle=UnityEngine.Object.FindFirstObjectByType<Rifle>();
            if(rifle.weapons.Length==2)
            {
                var knife=Weapon(rifle,"M9",Art+"/Knife/M9.obj",.32f,new Vector3(90,0,0));
                var handle=OpenAssets.Pbr(Art+"/Knife","M9 Handle","Handle_albedo.png","Handle_nm.png","Handle_roughness.png","Handle_metallic.png");
                var blade=OpenAssets.Pbr(Art+"/Knife","M9 Blade","Knife_albedo.png","Knife_nm.png","Knife_roughness.png","Knife_metallic.png");
                foreach(var r in knife.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.Contains("Handle")?handle:blade).ToArray();
                var shotgun=Weapon(rifle,"M870",Art+"/M870/M870.fbx",1.08f,Vector3.zero);
                var metal=OpenAssets.Pbr(Art+"/M870","M870 Wood Steel","diff.png",null,"rough.png","metal.png");
                foreach(var r in shotgun.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>metal).ToArray();
                var flash=UnityEngine.Object.Instantiate(rifle.weapons[0].model.GetComponentInChildren<ParticleSystem>(true),shotgun);flash.transform.localPosition=new Vector3(0,.04f,.53f);
                rifle.weapons=rifle.weapons.Concat(new[]{
                    new Rifle.WeaponSlot{name="M9",model=knife,melee=true,automatic=false,damage=55,rpm=95,ammo=0,reserve=0,capacity=0,reach=1.7f,shot=Load<AudioClip>(Art+"/Audio/Knife.wav")},
                    new Rifle.WeaponSlot{name="M870",model=shotgun,automatic=false,damage=16,rpm=70,recoil=2.8f,ammo=6,reserve=36,capacity=6,pellets=8,spread=3.4f,aimSpread=2.1f,reach=55,reload=3.4f,shot=Load<AudioClip>(Art+"/Audio/Shotgun.wav")}
                }).ToArray();knife.gameObject.SetActive(false);shotgun.gameObject.SetActive(false);
            }
            var boxes=UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None).OrderBy(b=>b.transform.position.z).ToArray();
            for(int i=0;i<boxes.Length;i++)
            {
                if(boxes[i].name.StartsWith("V11 "))continue;
                if(i%5==0){boxes[i].displayName="军用补给箱";boxes[i].kind=ContainerKind.Military;continue;}
                var box=(GameObject)PrefabUtility.InstantiatePrefab(props[(i-1)%4]);box.name="V11 "+box.name;box.transform.position=boxes[i].transform.position;box.transform.rotation=boxes[i].transform.rotation;UnityEngine.Object.DestroyImmediate(boxes[i].gameObject);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            foreach(int index in new[]{2,3}) { rifle.Equip(index,false);foreach(var a in UnityEngine.Object.FindObjectsByType<ViewArms>(FindObjectsSortMode.None))a.Pose();RaidReview.Shot(rifle.motor.view,"v11-weapon-"+index); }
            Debug.Log("V11_PROPS_READY");
        }
    }
}
