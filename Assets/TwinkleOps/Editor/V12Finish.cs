using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;
namespace TwinkleOps.Editor {
public static class V12Finish {
 public static void Apply(){
  EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
  const string root="Assets/TwinkleOps/Content/V12";
  var wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/V12/plastered_wall_02/Interior plaster.mat");
  var floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/V12/concrete_floor/Interior floor.mat");
  int count=0;
  foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   var r=filter.GetComponent<MeshRenderer>();if(!r||!r.enabled||!filter.sharedMesh||!r.sharedMaterials.Any(m=>m&&m.name=="ConcreteAtlas"))continue;
   // Preserve source geometry and collider. Replace undersized atlas UVs with metre-scaled surface UVs.
   var source=filter.sharedMesh;var verts=source.vertices;var sourceNormals=source.normals;var triangles=source.triangles;
   var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var walls=new List<int>();var floors=new List<int>();
   for(int t=0;t<triangles.Length;t+=3){Vector3 a=filter.transform.TransformPoint(verts[triangles[t]]),b=filter.transform.TransformPoint(verts[triangles[t+1]]),c=filter.transform.TransformPoint(verts[triangles[t+2]]);Vector3 normal=Vector3.Cross(b-a,c-a).normalized;bool horizontal=Mathf.Abs(normal.y)>.65f;var group=horizontal?floors:walls;
    for(int j=0;j<3;j++){int index=triangles[t+j];var world=filter.transform.TransformPoint(verts[index]);group.Add(v.Count);v.Add(verts[index]);n.Add(sourceNormals.Length==verts.Length?sourceNormals[index]:filter.transform.InverseTransformDirection(normal));uv.Add(horizontal?new Vector2(world.x,world.z)/3:Mathf.Abs(normal.x)>Mathf.Abs(normal.z)?new Vector2(world.z,world.y)/3:new Vector2(world.x,world.y)/3);}
   }
   var mesh=new Mesh{name="Retextured "+source.name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(walls,0);mesh.SetTriangles(floors,1);mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,root+"/BuildingSurface"+count+".asset");filter.sharedMesh=mesh;r.sharedMaterials=new[]{wall,floor};count++;
  }
  var content=AssetDatabase.LoadAssetAtPath<ContentManifest>("Assets/TwinkleOps/Content/ContentManifest.asset");content.credits=File.ReadAllText("Docs/CREDITS.md");EditorUtility.SetDirty(content);
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("V12_MATERIAL_REPAIR "+count);V12Map.Review();
 }
}}
