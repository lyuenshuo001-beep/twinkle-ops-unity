using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TwinkleOps.Editor
{
    public static class V11CombatSetup
    {
        const string Root="Assets/TwinkleOps/Content/V11";
        static AnimationClip Clip(string name,bool loop)
        {
            string path="Assets/ThirdParty/OpenGameArt/CityZombie/"+name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);var clips=importer.defaultClipAnimations;
            foreach(var c in clips) { c.loopTime=loop;c.lockRootRotation=c.lockRootHeightY=c.lockRootPositionXZ=true;c.keepOriginalOrientation=false;c.events=Array.Empty<AnimationEvent>(); }
            importer.clipAnimations=clips;importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        }
        public static void Apply()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string path=Root+"/InfectedV11.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Stride",AnimatorControllerParameterType.Float);
                var machine=controller.layers[0].stateMachine;var move=machine.AddState("Locomotion");machine.defaultState=move;
                var tree=new BlendTree {name="Idle Walk Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);
                tree.AddChild(Clip("Zombie Idle",true),0);tree.AddChild(Clip("Zombie Walk",true),1.35f);tree.AddChild(Clip("Zombie Running",true),4.1f);move.motion=tree;move.speedParameter="Stride";move.speedParameterActive=true;
                foreach(var pair in new[]{("AttackA","Zombie Attack"),("AttackB","Zombie Attack2"),("Lunge","Zombie Attack3"),("Hit","Zombie Reaction Hit"),("Death","Zombie Dying")})
                {
                    var s=machine.AddState(pair.Item1);s.motion=Clip(pair.Item2,false);s.speed=pair.Item1=="Hit"?1.3f:1;
                    if(pair.Item1=="Hit") { var t=s.AddTransition(move);t.hasExitTime=true;t.exitTime=.75f;t.duration=.13f; }
                }
            }
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            foreach(var z in UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None))Configure(z,controller);
            var rifle=UnityEngine.Object.FindFirstObjectByType<Rifle>();rifle.hitMask=CombatRay.Mask;
            rifle.weapons[1].shot=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/OpenGameArt/Audio/AKM_Single.wav");
            foreach(var w in rifle.weapons.Take(2)) { w.capacity=30;w.pellets=1;w.automatic=true;w.spread=w.name=="AKM"?.85f:.65f;w.aimSpread=.035f;w.reach=150; }
            foreach(var source in rifle.GetComponentsInChildren<AudioSource>(true))if(source!=rifle.audioSource){source.playOnAwake=false;source.Stop();source.enabled=false;}
            rifle.audioSource.playOnAwake=false;rifle.audioSource.loop=false;rifle.audioSource.volume=.6f;
            if(!rifle.GetComponent<CombatDebug>())rifle.gameObject.AddComponent<CombatDebug>();
            var m=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);m.zombieAnimator=controller;m.akmGunshot=rifle.weapons[1].shot;EditorUtility.SetDirty(m);
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(9).stringValue="InfectedHitbox";layers.GetArrayElementAtIndex(10).stringValue="InfectedHull";tags.ApplyModifiedProperties();
            PlayerSettings.bundleVersion="1.1.0-alpha";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Inspect();Debug.Log("V11_COMBAT_SETUP_OK");
        }
        public static void Configure(Infected z,RuntimeAnimatorController controller)
        {
            // Imported walk-pose renderer bounds were offset from the humanoid root.
            // Humanoid retargeting recenters the skeleton; keep its model root centered too.
            z.animator.transform.localPosition=new Vector3(0,.0525f,0);
            z.gameObject.layer=10;z.attackDistance=1.15f;z.animator.runtimeAnimatorController=controller;z.animator.applyRootMotion=false;z.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;z.animator.Rebind();z.animator.SetFloat("Stride",1);z.animator.Play("Locomotion");z.animator.Update(.05f);
            foreach(var old in z.GetComponentsInChildren<InfectedHitbox>())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var a=z.animator;
            void Segment(HumanBodyBones first,HumanBodyBones end,float radius,HitRegion region)
            {
                var bone=a.GetBoneTransform(first);if(!bone)return;var to=end==HumanBodyBones.LastBone?null:a.GetBoneTransform(end);Vector3 start=bone.position,finish=to?to.position:start+Vector3.up*.13f;
                if(region==HitRegion.Head) { start=bone.position-Vector3.up*.01f;finish=bone.position+Vector3.up*.16f; }
                var go=new GameObject("Hitbox_"+first);go.layer=9;go.transform.SetParent(bone,false);go.transform.position=(start+finish)/2;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,finish-start);
                var scale=go.transform.lossyScale;go.transform.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
                var c=go.AddComponent<CapsuleCollider>();c.isTrigger=true;c.direction=1;c.radius=radius;c.height=Vector3.Distance(start,finish)+radius*2;
                var h=go.AddComponent<InfectedHitbox>();h.owner=z;h.region=region;
            }
            Segment(HumanBodyBones.Head,HumanBodyBones.LastBone,.13f,HitRegion.Head);
            Segment(HumanBodyBones.Hips,HumanBodyBones.Chest,.18f,HitRegion.Body);
            Segment(HumanBodyBones.Chest,HumanBodyBones.Neck,.2f,HitRegion.Body);
            foreach(var pair in new[]{(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm),(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand),(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm),(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand),(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg),(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot),(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg),(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot)})Segment(pair.Item1,pair.Item2,pair.Item1.ToString().Contains("Leg")?.095f:.07f,HitRegion.Limb);
        }
        public static void Inspect()
        {
            var z=UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None)[0];
            var log=new System.Text.StringBuilder();
            foreach(var state in new[]{"Locomotion","AttackA","AttackB","Lunge","Hit","Death"})
            {
                z.animator.Play(state,0,0);z.animator.Update(0);
                for(int i=0;i<=10;i++)
                {
                    z.animator.Play(state,0,i/10f);z.animator.Update(0);
                    var l=z.transform.InverseTransformPoint(z.animator.GetBoneTransform(HumanBodyBones.LeftHand).position);var r=z.transform.InverseTransformPoint(z.animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                    log.AppendLine(state+" "+i/10f+" left="+l.ToString("F3")+" right="+r.ToString("F3"));
                }
            }
            File.WriteAllText("Logs/v11-attack-samples.txt",log.ToString());
        }
    }
}
