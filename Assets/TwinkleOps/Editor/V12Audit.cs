using System;using System.IO;using System.Linq;using UnityEditor.SceneManagement;using UnityEngine;
namespace TwinkleOps.Editor {
public static class V12Audit {
 [Serializable] class Entry {public string name,parent;public Vector3 position,min,max;}
 [Serializable] class Report {public Entry[] buildings;}
 public static void Inspect(){EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
 var objects=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Structures_") && (!t.parent||!t.parent.name.StartsWith("Structures_"))).Select(t=>{var b=OpenAssets.BoundsOf(t.gameObject);return new Entry{name=t.name,parent=t.parent?t.parent.name:"",position=t.position,min=b.min,max=b.max};}).ToArray();
 File.WriteAllText("Logs/v12-building-bounds.json",JsonUtility.ToJson(new Report{buildings=objects},true));Debug.Log("V12_AUDIT_READY");
 }
}}
