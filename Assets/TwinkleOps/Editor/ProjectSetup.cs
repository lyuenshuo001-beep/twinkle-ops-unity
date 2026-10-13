using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TwinkleOps.Editor
{
    public static class ProjectSetup
    {
        const string Root="Assets/TwinkleOps";
        public const string ManifestPath=Root+"/Content/ContentManifest.asset";
        const string Lobby=Root+"/Scenes/Lobby.unity", Raid=Root+"/Scenes/InfectionZone.unity";
        [MenuItem("Twinkle Ops/1. Initialize Foundation")]
        public static void Initialize()
        {
            Directory.CreateDirectory(Root+"/Content"); Directory.CreateDirectory(Root+"/Scenes");
            AssetDatabase.Refresh();
            var manifest=AssetDatabase.LoadAssetAtPath<ContentManifest>(ManifestPath);
            if(!manifest) { manifest=ScriptableObject.CreateInstance<ContentManifest>(); AssetDatabase.CreateAsset(manifest,ManifestPath); }
            PlayerSettings.companyName="TwinkleOps"; PlayerSettings.productName="TWINKLE OPS";
            PlayerSettings.bundleVersion="0.1.0-foundation";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.runInBackground=true;
            EditorSettings.serializationMode=SerializationMode.ForceText;
            EditorSettings.externalVersionControl="Visible Meta Files";
            if(!File.Exists(Lobby))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Lobby Camera").AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.035f,.045f,.05f);
                camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                var app=new GameObject("Twinkle Ops").AddComponent<GameApp>(); app.content=manifest;
                app.uiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/UI/NotoSansSC.ttf");
                EditorSceneManager.SaveScene(scene,Lobby);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Lobby,true)};
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/environment.json",JsonUtility.ToJson(new EnvironmentReport {
                unity=Application.unityVersion, urp=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UniversalRenderPipeline).Assembly)?.version ?? "unknown",
                windows=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64),
                web=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL),missing=manifest.Missing().ToArray()
            },true));
            Debug.Log("TWINKLE_OPS_SETUP_OK");
        }
        [Serializable] sealed class EnvironmentReport { public string unity,urp; public bool windows,web; public string[] missing; }

        [MenuItem("Twinkle Ops/2. Validate Art Readiness")]
        public static void ValidateContent()
        { ValidateContentInternal(true); }
        static void ValidateContentInternal(bool requireVisualReview)
        {
            var m=AssetDatabase.LoadAssetAtPath<ContentManifest>(ManifestPath);
            if(!m) throw new BuildFailedException("Missing ContentManifest");
            var missing=m.Missing(requireVisualReview);
            if(missing.Count>0) throw new BuildFailedException("Art gate blocked: "+string.Join("; ",missing));
            foreach(var prefab in new[]{m.environment,m.m4a1,m.zombie,m.searchContainer,m.droppedLoot})
            {
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    if(filter.sharedMesh && AssetDatabase.GetAssetPath(filter.sharedMesh)=="Library/unity default resources")
                        throw new BuildFailedException("Primitive mesh found in final art: "+prefab.name);
                foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    if(renderer.sharedMaterials.Any(mat=>!mat || !mat.shader || !mat.shader.isSupported || mat.shader.name=="Hidden/InternalErrorShader"))
                        throw new BuildFailedException("Missing/unsupported material: "+renderer.name);
            }
            ValidateAnimator(m.weaponAnimator,new[]{"Fire","Reload","ADS"});
            ValidateAnimator(m.zombieAnimator,new[]{"Speed","Attack","Hit","Death"});
            Debug.Log("TWINKLE_OPS_ART_GATE_OK");
        }
        static void ValidateAnimator(RuntimeAnimatorController runtime,string[] parameters)
        {
            var controller=runtime as UnityEditor.Animations.AnimatorController;
            if(!controller || controller.animationClips.Length==0) throw new BuildFailedException("Animator must contain real animation clips");
            foreach(var parameter in parameters)
                if(!controller.parameters.Any(p=>p.name==parameter)) throw new BuildFailedException("Animator missing parameter: "+parameter);
        }
        [MenuItem("Twinkle Ops/3. Assemble Licensed Raid Scene")]
        public static void AssembleRaid()
        {
            ValidateContentInternal(false);
            if(File.Exists(Raid)) throw new BuildFailedException("InfectionZone already exists. Edit it directly; automatic assembly will not overwrite it.");
            var m=AssetDatabase.LoadAssetAtPath<ContentManifest>(ManifestPath);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var env=(GameObject)PrefabUtility.InstantiatePrefab(m.environment);
            foreach(var filter in env.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh && !filter.GetComponent<Collider>()) filter.gameObject.AddComponent<MeshCollider>();
            var surface=env.GetComponent<NavMeshSurface>(); if(!surface) surface=env.AddComponent<NavMeshSurface>(); surface.BuildNavMesh();
            if(!surface.navMeshData || UnityEngine.AI.NavMesh.CalculateTriangulation().vertices.Length==0) throw new BuildFailedException("No walkable navigation surface");
            if(!AssetDatabase.Contains(surface.navMeshData)) AssetDatabase.CreateAsset(surface.navMeshData,AssetDatabase.GenerateUniqueAssetPath(Root+"/Content/InfectionZoneNavMesh.asset"));
            var sun=new GameObject("Overcast sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.2f; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(48,-30,0);
            RenderSettings.ambientLight=new Color(.35f,.39f,.43f); RenderSettings.fog=true; RenderSettings.fogColor=new Color(.25f,.29f,.31f); RenderSettings.fogDensity=.012f;
            var player=new GameObject("Operator"); player.layer=8; player.transform.position=m.playerSpawn;
            var cc=player.AddComponent<CharacterController>(); cc.height=1.8f; cc.center=new Vector3(0,.9f,0); cc.radius=.3f; cc.stepOffset=.3f;
            var cam=new GameObject("First person camera").AddComponent<Camera>(); cam.transform.SetParent(player.transform,false); cam.transform.localPosition=new Vector3(0,1.65f,0); cam.nearClipPlane=.04f; cam.fieldOfView=72; cam.tag="MainCamera";
            cam.gameObject.AddComponent<AudioListener>(); cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var motor=player.AddComponent<PlayerMotor>(); motor.view=cam;
            var gun=(GameObject)PrefabUtility.InstantiatePrefab(m.m4a1); gun.transform.SetParent(cam.transform,false); gun.transform.localPosition=m.weaponPosition; gun.transform.localEulerAngles=m.weaponRotation; gun.transform.localScale=m.weaponScale;
            foreach(var t in gun.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=8;
            foreach(var c in gun.GetComponentsInChildren<Collider>()) c.enabled=false;
            var rifle=player.AddComponent<Rifle>(); rifle.motor=motor; rifle.weaponRoot=gun.transform; rifle.hitMask=~(1<<8);
            rifle.animator=gun.GetComponentInChildren<Animator>(); if(!rifle.animator) rifle.animator=gun.AddComponent<Animator>(); rifle.animator.runtimeAnimatorController=m.weaponAnimator;
            rifle.audioSource=gun.AddComponent<AudioSource>(); rifle.audioSource.playOnAwake=false; rifle.shot=m.gunshot; rifle.reloadSound=m.reload;
            rifle.muzzleFlash=gun.GetComponentInChildren<ParticleSystem>();
            if(m.akm)
            {
                var ak=(GameObject)PrefabUtility.InstantiatePrefab(m.akm); ak.transform.SetParent(cam.transform,false); ak.transform.localPosition=m.weaponPosition; ak.transform.localEulerAngles=new Vector3(0,180,0);
                foreach(var t in ak.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=8;
                rifle.weapons=new[]{new Rifle.WeaponSlot { name="M4A1",model=gun.transform,shot=m.gunshot,damage=34,rpm=750,recoil=1,reload=2.4f },new Rifle.WeaponSlot { name="AKM",model=ak.transform,shot=m.akmGunshot,damage=46,rpm=600,recoil=1.45f,reload=2.8f }};
                ak.SetActive(false);
            }
            if(m.arms)
            {
                var arms=(GameObject)PrefabUtility.InstantiatePrefab(m.arms); arms.transform.SetParent(cam.transform,false); arms.transform.localPosition=new Vector3(0,-.27f,-.13f);
                foreach(var animator in arms.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
                foreach(var t in arms.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=8;
                arms.AddComponent<ViewArms>().rifle=rifle;
            }
            int boxIndex=0;
            foreach(var pos in new[]{m.containerA,m.containerB}.Concat(m.extraContainers))
            { var box=(GameObject)PrefabUtility.InstantiatePrefab(m.searchContainer); box.transform.position=pos; var loot=box.GetComponent<LootContainer>(); if(!loot) loot=box.AddComponent<LootContainer>(); loot.displayName="物资箱 "+(++boxIndex).ToString("00"); if(!box.GetComponentInChildren<Collider>()) throw new BuildFailedException("Search container needs collider"); }
            foreach(var pos in m.zombieSpawns)
            {
                if(!UnityEngine.AI.NavMesh.SamplePosition(pos,out var hit,3,UnityEngine.AI.NavMesh.AllAreas)) throw new BuildFailedException("Zombie spawn is off navigation mesh");
                var zombie=(GameObject)PrefabUtility.InstantiatePrefab(m.zombie); zombie.transform.position=hit.position;
                var animator=zombie.GetComponentInChildren<Animator>(); if(!animator) throw new BuildFailedException("Zombie needs rigged Animator"); animator.runtimeAnimatorController=m.zombieAnimator; animator.applyRootMotion=false;
                var agent=zombie.GetComponent<UnityEngine.AI.NavMeshAgent>(); if(!agent) agent=zombie.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.speed=4.6f; agent.stoppingDistance=1.4f;
                if(!zombie.GetComponentInChildren<Collider>()) { var col=zombie.AddComponent<CapsuleCollider>(); col.center=new Vector3(0,.9f,0); col.height=1.8f; col.radius=.3f; }
                var infected=zombie.GetComponent<Infected>(); if(!infected) infected=zombie.AddComponent<Infected>(); infected.animator=animator; infected.target=player.transform;
            }
            var exit=new GameObject("Extraction - 8 seconds").AddComponent<ExtractionZone>(); exit.transform.position=m.extraction;
            var marker=exit.gameObject.AddComponent<LineRenderer>(); marker.useWorldSpace=false; marker.loop=true; marker.positionCount=64; marker.startWidth=marker.endWidth=.07f;
            marker.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Content/ExtractionSignal.mat");
            for(int i=0;i<64;i++) marker.SetPosition(i,new Vector3(Mathf.Sin(i*Mathf.PI/32)*2.8f,.07f,Mathf.Cos(i*Mathf.PI/32)*2.8f));
            var ambience=new GameObject("Ambience").AddComponent<AudioSource>(); ambience.clip=m.ambience; ambience.loop=true; ambience.playOnAwake=true; ambience.volume=.3f;
            EditorSceneManager.SaveScene(scene,Raid);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Lobby,true),new EditorBuildSettingsScene(Raid,true)};
            AssetDatabase.SaveAssets();
        }
        public static void BuildWindowsFoundation() => Build(BuildTarget.StandaloneWindows64,true);
        public static void BuildWebFoundation() => Build(BuildTarget.WebGL,true);
        [MenuItem("Twinkle Ops/Build/Windows Game")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64,false);
        [MenuItem("Twinkle Ops/Build/Web Game")]
        public static void BuildWeb() => Build(BuildTarget.WebGL,false);
        static void Build(BuildTarget target,bool foundation)
        {
            if(!foundation) { ValidateContent(); if(!File.Exists(Raid)) throw new BuildFailedException("Assemble and verify the raid scene first"); }
            string platform=target==BuildTarget.WebGL?"Web":"Windows";
            string folder="Builds/"+(foundation?"Validation/":"Game/")+platform;
            Directory.CreateDirectory(folder); Directory.CreateDirectory("Logs");
            string path=target==BuildTarget.WebGL?folder:folder+"/TwinkleOps.exe";
            var scenes=foundation?new[]{Lobby}:new[]{Lobby,Raid};
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=scenes,locationPathName=path,target=target,options=BuildOptions.None });
            File.WriteAllText("Logs/build-"+platform.ToLowerInvariant()+".json",JsonUtility.ToJson(new BuildRecord { result=report.summary.result.ToString(),bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,foundation=foundation },true));
            if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException(platform+" build failed");
            if(target==BuildTarget.WebGL) File.WriteAllText(folder+"/.nojekyll","");
            Directory.CreateDirectory(folder+"/ThirdPartyNotices");
            File.Copy(Root+"/UI/OFL.txt",folder+"/ThirdPartyNotices/NotoSansSC-OFL.txt",true);
            File.Copy("Docs/CREDITS.md",folder+"/ThirdPartyNotices/CREDITS.md",true);
            File.WriteAllText(folder+"/BUILD-STATUS.txt",foundation?"ENGINEERING VALIDATION ONLY. Lobby shell; art assets and playable raid are not included. Not a completed game.":"Phase 1 build. See Docs/QA.md for manual acceptance requirements.");
            Debug.Log("TWINKLE_OPS_BUILD_OK: "+platform+" foundation="+foundation);
        }
        [Serializable] sealed class BuildRecord { public string result; public ulong bytes; public double seconds; public bool foundation; }
    }
}
