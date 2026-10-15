using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace TwinkleOps.Editor {public static class V12ReleaseReady {
 public static void Apply(){EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");int restored=0;
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name=="Structures_3")foreach(var f in t.GetComponentsInChildren<MeshFilter>()){
   var original=PrefabUtility.GetCorrespondingObjectFromSource(f);var r=f.GetComponent<MeshRenderer>();if(original&&r&&r.enabled){f.sharedMesh=original.sharedMesh;r.sharedMaterials=original.GetComponent<MeshRenderer>().sharedMaterials;restored++;}
  }
  if(restored==0)throw new System.Exception("Detailed brick/wood facade source could not be restored");
  var content=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);content.credits=File.ReadAllText("Docs/CREDITS.md");EditorUtility.SetDirty(content);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("V12_RELEASE_READY restored="+restored);
 }
}}
