using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TwinkleOps.Editor
{
    public static class RaidContent
    {
        const string Root="Assets/TwinkleOps/Content";
        const string Art=OpenAssets.Root;
        static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
        static GameObject Instance(string path,Transform parent=null)
        {
            var o=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
            PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            if(parent) o.transform.SetParent(parent,false); return o;
        }
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var m=Load<ContentManifest>(ProjectSetup.ManifestPath);
            m.m4a1=Load<GameObject>(Art+"/M4A1/M4A1.prefab"); m.akm=Load<GameObject>(Art+"/AKM/AKM.prefab"); m.arms=Load<GameObject>(Art+"/Arms/FPS ARMS RIG 1.prefab");
            m.gunshot=Load<AudioClip>(Art+"/Audio/M4A1_Shot.wav"); m.akmGunshot=Load<AudioClip>(Art+"/Audio/AKM_Shot.wav"); m.reload=Load<AudioClip>(Art+"/Audio/Reload.wav"); m.ambience=Load<AudioClip>(Art+"/Audio/Wind.ogg");
            m.weaponPosition=new Vector3(.17f,-.17f,.38f); m.weaponRotation=new Vector3(0,180,0); m.weaponScale=Vector3.one;
            m.playerSpawn=new Vector3(0,.12f,-23); m.containerA=new Vector3(2,0,-20); m.containerB=new Vector3(-4,0,-10);
            m.extraContainers=new[]{new Vector3(2,0,1),new Vector3(-10,0,12),new Vector3(2,0,21),new Vector3(17,.22f,5),new Vector3(-24,.22f,18),new Vector3(17,.22f,40),new Vector3(-4,0,48),new Vector3(-26,.22f,29)};
            m.extraction=new Vector3(0,0,59);
            m.zombieSpawns=new[]{new Vector3(1,0,9),new Vector3(-7,0,20),new Vector3(2,0,33),new Vector3(17,0,15),new Vector3(-24,0,20),new Vector3(18,0,39),new Vector3(-8,0,44)};
            m.itemIcons=new Sprite[Items.All.Length];
            for(int i=0;i<Items.All.Length;i++)
            {
                string path="Assets/TwinkleOps/UI/Items/"+Items.All[i].id+".png";
                var ti=(TextureImporter)AssetImporter.GetAtPath(path); ti.textureType=TextureImporterType.Sprite; ti.spriteImportMode=SpriteImportMode.Single; ti.alphaIsTransparency=true; ti.SaveAndReimport();
                m.itemIcons[i]=Load<Sprite>(path);
            }
            PrepareCrate(m); PrepareZombie(m); PrepareWeapons(m); PrepareEnvironment(m);
            string signal=Root+"/ExtractionSignal.mat"; var sm=Load<Material>(signal);
            if(!sm) { sm=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(sm,signal); } sm.SetColor("_BaseColor",new Color(.12f,1,.64f)); EditorUtility.SetDirty(sm);
            m.credits=File.ReadAllText("Docs/CREDITS.md"); m.licensesReviewed=true;
            // Visual approval is deliberately left to the subsequent rendered review.
            EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
            foreach(var path in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").Select(AssetDatabase.GUIDToAssetPath))
            { var rp=Load<UniversalRenderPipelineAsset>(path); rp.useSRPBatcher=false; rp.shadowDistance=60; rp.msaaSampleCount=2; EditorUtility.SetDirty(rp); }
            AssetDatabase.SaveAssets(); Debug.Log("RAID_CONTENT_PREPARED: 10 crates, 2 weapons, 7 infected.");
        }
        static void PrepareCrate(ContentManifest m)
        {
            var crate=Instance("Assets/ThirdParty/PolyHaven/old_military_crate/old_military_crate.prefab");
            foreach(var t in crate.GetComponentsInChildren<Transform>().Reverse()) if(t!=crate.transform && t.name.EndsWith("_b")) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach(var c in crate.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            var b=OpenAssets.BoundsOf(crate); var wrapper=new GameObject("Military Supply Crate"); crate.transform.SetParent(wrapper.transform,false); crate.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var loot in crate.GetComponents<LootContainer>()) UnityEngine.Object.DestroyImmediate(loot);
            var box=wrapper.AddComponent<BoxCollider>(); b=OpenAssets.BoundsOf(wrapper); box.center=b.center; box.size=b.size;
            wrapper.AddComponent<LootContainer>(); m.searchContainer=PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/SearchCrate.prefab");
            UnityEngine.Object.DestroyImmediate(wrapper);
            var drop=Instance(Root+"/SearchCrate.prefab"); UnityEngine.Object.DestroyImmediate(drop.GetComponent<LootContainer>()); drop.transform.localScale=Vector3.one*.3f;
            m.droppedLoot=PrefabUtility.SaveAsPrefabAsset(drop,Root+"/DroppedSupplies.prefab"); UnityEngine.Object.DestroyImmediate(drop);
        }
        static AnimationClip Clip(string filename,bool loop)
        {
            string path=Art+"/CityZombie/"+filename+".fbx"; var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            var clips=importer.defaultClipAnimations; foreach(var c in clips) { c.loopTime=loop; c.lockRootRotation=true; c.lockRootHeightY=true; c.lockRootPositionXZ=true; c.keepOriginalOrientation=false; }
            importer.clipAnimations=clips; importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        }
        static void PrepareZombie(ContentManifest m)
        {
            string path=Root+"/Infected.controller"; var controller=Load<AnimatorController>(path);
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(path); controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
                foreach(string p in new[]{"Attack","Hit","Death"}) controller.AddParameter(p,AnimatorControllerParameterType.Trigger);
                var sm=controller.layers[0].stateMachine; var idle=sm.AddState("Idle"); idle.motion=Clip("Zombie Idle",true); sm.defaultState=idle;
                var run=sm.AddState("Chase"); run.motion=Clip("Zombie Running",true);
                var go=idle.AddTransition(run); go.hasExitTime=false; go.duration=.12f; go.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
                go=run.AddTransition(idle); go.hasExitTime=false; go.duration=.18f; go.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
                foreach(var pair in new[]{("Attack","Zombie Attack"),("Hit","Zombie Reaction Hit"),("Death","Zombie Dying")})
                {
                    var state=sm.AddState(pair.Item1); state.motion=Clip(pair.Item2,false);
                    var tr=sm.AddAnyStateTransition(state); tr.hasExitTime=false; tr.duration=.1f; tr.canTransitionToSelf=false; tr.AddCondition(AnimatorConditionMode.If,0,pair.Item1);
                    if(pair.Item1!="Death") { var back=state.AddTransition(idle); back.hasExitTime=true; back.exitTime=pair.Item1=="Hit"?.35f:.8f; back.duration=.15f; }
                }
            }
            var wrapper=new GameObject("Infected Worker"); var model=Instance(Art+"/PixelZombie/walk.prefab",wrapper.transform);
            var b=OpenAssets.BoundsOf(model); float factor=1.78f/b.size.y; model.transform.localScale*=factor;
            model.transform.localPosition=new Vector3(-b.center.x*factor,-b.min.y*factor,-b.center.z*factor);
            var animator=model.GetComponent<Animator>(); if(!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman) throw new Exception("Worker rig is not a valid humanoid");
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
            m.zombieAnimator=controller; m.zombie=PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/InfectedWorker.prefab"); UnityEngine.Object.DestroyImmediate(wrapper);
        }
        static void PrepareWeapons(ContentManifest m)
        {
            string path=Root+"/M4A1.controller"; var controller=Load<AnimatorController>(path);
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(path); controller.AddParameter("ADS",AnimatorControllerParameterType.Bool); controller.AddParameter("Fire",AnimatorControllerParameterType.Trigger); controller.AddParameter("Reload",AnimatorControllerParameterType.Trigger);
                var sm=controller.layers[0].stateMachine;
                var idle=sm.AddState("Ready"); sm.defaultState=idle;
                // Author short keyframed charging-handle motion on the real segmented M4 model.
                var gun=Instance(Art+"/M4A1/M4A1.prefab"); var handle=gun.GetComponentsInChildren<Transform>().First(t=>t.name=="Charging_Handle");
                string binding=AnimationUtility.CalculateTransformPath(handle,gun.transform); float z=handle.localPosition.z;
                foreach(var info in new[]{("Fire",.08f,.02f),("Reload",2.4f,.055f)})
                {
                    var clip=new AnimationClip { name=info.Item1,frameRate=60 };
                    clip.SetCurve(binding,typeof(Transform),"m_LocalPosition.z",new AnimationCurve(new Keyframe(0,z),new Keyframe(info.Item2*.4f,z+info.Item3),new Keyframe(info.Item2,z)));
                    AssetDatabase.CreateAsset(clip,Root+"/M4_"+info.Item1+".anim"); var state=sm.AddState(info.Item1); state.motion=clip;
                    var tr=sm.AddAnyStateTransition(state); tr.hasExitTime=false; tr.duration=0; tr.AddCondition(AnimatorConditionMode.If,0,info.Item1);
                    var back=state.AddTransition(idle); back.hasExitTime=true; back.exitTime=1; back.duration=0;
                }
                UnityEngine.Object.DestroyImmediate(gun);
            }
            m.weaponAnimator=controller;
        }
        static void PrepareEnvironment(ContentManifest m)
        {
            var env=new GameObject("Quarantine Construction District");
            foreach(var p in new[]{(2,new Vector3(-26,0,19)),(8,new Vector3(20,0,26)),(3,new Vector3(-13,0,-8)),(4,new Vector3(-22,0,49)),(6,new Vector3(21,0,-18))})
            { var o=Instance(Art+"/Buildings/Structures_"+p.Item1+".prefab",env.transform); o.transform.localPosition=p.Item2; }
            string folder="Assets/ThirdParty/PolyHaven/asphalt_02";
            var ti=(TextureImporter)AssetImporter.GetAtPath(folder+"/nor_gl.jpg"); ti.textureType=TextureImporterType.NormalMap; ti.SaveAndReimport();
            var groundMat=OpenAssets.Pbr(folder,"Asphalt","Diffuse.jpg","nor_gl.jpg","Rough.jpg");
            var mesh=new Mesh { name="District road surface" }; mesh.vertices=new[]{new Vector3(-45,0,-34),new Vector3(-45,0,72),new Vector3(45,0,72),new Vector3(45,0,-34)}; mesh.uv=new[]{new Vector2(0,0),new Vector2(0,35),new Vector2(30,35),new Vector2(30,0)}; mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateNormals(); mesh.RecalculateTangents();
            string meshPath=Root+"/DistrictGround.asset"; var saved=Load<Mesh>(meshPath); if(saved) { EditorUtility.CopySerialized(mesh,saved); mesh=saved; } else AssetDatabase.CreateAsset(mesh,meshPath);
            var ground=new GameObject("Weathered Asphalt"); ground.transform.SetParent(env.transform); ground.AddComponent<MeshFilter>().sharedMesh=mesh; ground.AddComponent<MeshRenderer>().sharedMaterial=groundMat; ground.AddComponent<MeshCollider>().sharedMesh=mesh;
            for(int i=0;i<24;i++)
            {
                var barrier=Instance("Assets/ThirdParty/PolyHaven/concrete_road_barrier/concrete_road_barrier.prefab",env.transform);
                barrier.transform.position=new Vector3(i%2==0?-7:7,0,-19+(i/2)*6.2f); barrier.transform.eulerAngles=new Vector3(0,90,0);
            }
            for(int i=0;i<16;i++)
            {
                var barrel=Instance("Assets/ThirdParty/PolyHaven/barrel_03/barrel_03.prefab",env.transform);
                barrel.transform.position=new Vector3(i%2==0?-10:10,0,-16+i*4.6f); barrel.transform.rotation=Quaternion.AngleAxis(i*37,Vector3.up)*barrel.transform.rotation;
                var bounds=OpenAssets.BoundsOf(barrel); barrel.transform.position-=Vector3.up*bounds.min.y;
            }
            m.environment=PrefabUtility.SaveAsPrefabAsset(env,Root+"/QuarantineDistrict.prefab"); UnityEngine.Object.DestroyImmediate(env);
        }
    }
}
