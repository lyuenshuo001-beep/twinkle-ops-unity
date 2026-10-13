using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TwinkleOps.Editor
{
 public static class CorrectWeapons
 {
  public static void Apply()
  {
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
   var wrapper=new GameObject("AKM View");
   var mesh=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/OpenGameArt/AKM/AKM.prefab"));
   mesh.transform.SetParent(wrapper.transform,false); // Preserve the FBX's rotation and centimetre conversion.
   var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,"Assets/TwinkleOps/Content/AKMView.prefab"); Object.DestroyImmediate(wrapper);
   var manifest=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath); manifest.akm=prefab;EditorUtility.SetDirty(manifest);
   var rifle=Object.FindFirstObjectByType<Rifle>();var previous=rifle.weapons[1].model;
   var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(rifle.motor.view.transform,false);model.transform.localPosition=manifest.weaponPosition;model.transform.localEulerAngles=new Vector3(0,180,0);
   foreach(var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=8;
   rifle.weapons[1].model=model.transform;model.SetActive(false);Object.DestroyImmediate(previous.gameObject);
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
   ReleasePrep.Prepare();
   EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");rifle=Object.FindFirstObjectByType<Rifle>();rifle.Equip(1,false);
   foreach(var a in Object.FindObjectsByType<ViewArms>(FindObjectsSortMode.None)) a.Pose();
   RaidReview.Shot(rifle.motor.view,"raid-ak-corrected");
   Debug.Log("AK_TRANSFORM_CORRECTED");
  }
 }
}
