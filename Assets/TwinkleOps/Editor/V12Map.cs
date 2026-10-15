using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.AI;using UnityEngine.Rendering.Universal;using Unity.AI.Navigation;
namespace TwinkleOps.Editor {
public static class V12Map {
 const string Art="Assets/ThirdParty/V12",Root="Assets/TwinkleOps/Content/V12",Scene="Assets/TwinkleOps/Scenes/InfectionZone.unity";
 static Transform district;static Material floor,plaster,brick,trim,windows;static int meshId;
 static T Load<T>(string p)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(p);
 public static void Apply(){
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Directory.CreateDirectory(Root);AssetDatabase.Refresh();
  foreach(var f in Directory.GetFiles(Art,"*",SearchOption.AllDirectories).Where(f=>f.EndsWith(".jpg")||f.EndsWith(".png"))){var t=AssetImporter.GetAtPath(f)as TextureImporter;if(t==null)continue;t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.Compressed;if(f.Contains("nor_gl"))t.textureType=TextureImporterType.NormalMap;t.SaveAndReimport();}
  string factory=Art+"/modular_factory_facade";
  brick=OpenAssets.Pbr(factory,"Factory brick","brick_diff.jpg","brick_nor_gl.jpg","brick_rough.jpg");trim=OpenAssets.Pbr(factory,"Factory trim","trim_01_diff.jpg","trim_01_nor_gl.jpg","trim_01_rough.jpg");windows=OpenAssets.Pbr(factory,"Factory windows","windows_diff.jpg","windows_nor_gl.jpg","windows_rough.jpg");
  floor=OpenAssets.Pbr(Art+"/concrete_floor","Interior floor","diff.jpg","nor_gl.jpg","rough.jpg");plaster=OpenAssets.Pbr(Art+"/plastered_wall_02","Interior plaster","diff.jpg","nor_gl.jpg","rough.jpg");
  foreach(var mat in new[]{brick,trim,windows,floor,plaster}){mat.SetFloat("_Cull",0);mat.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(mat);}
  foreach(string module in new[]{"PanelSolid","PanelDoor","PanelWindow","Cornice","Crown","Base"}){
   var obj=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(factory+"/"+module+".fbx"));PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   foreach(var r in obj.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name.Contains("windows")?windows:m&&m.name.Contains("trim")?trim:brick).ToArray();
   var parent=new GameObject(module);obj.transform.SetParent(parent.transform,false);var b=OpenAssets.BoundsOf(obj);obj.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   if(module.StartsWith("Panel")){
    void Box(Vector3 c,Vector3 s){var box=parent.AddComponent<BoxCollider>();box.center=c;box.size=s;}
    if(module=="PanelDoor"){Box(new Vector3(-1.2f,1.5f,0),new Vector3(.6f,3,.2f));Box(new Vector3(1.2f,1.5f,0),new Vector3(.6f,3,.2f));Box(new Vector3(0,2.85f,0),new Vector3(1.8f,.3f,.2f));}
    else Box(new Vector3(0,1.5f,0),new Vector3(3,3,.2f));
   }
   PrefabUtility.SaveAsPrefabAsset(parent,Root+"/"+module+".prefab");UnityEngine.Object.DestroyImmediate(parent);
  }
  EditorSceneManager.OpenScene(Scene);if(GameObject.Find("V12 Connected District"))throw new Exception("V12 already applied. Preserve scene; use targeted repair.");district=new GameObject("V12 Connected District").transform;
  // Reception links the existing clinic entrance to the central workshop service alley.
  Room("Clinic reception",36,51,9,27,.03f,new[]{2},new[]{2},new[]{2},new[]{2});
  Wall("Consultation partition",new Vector3(43.5f,.03f,9),new Vector3(43.5f,.03f,27),new[]{1,4});
  Supply("old_bed_frame",new Vector3(39,.08f,13));Supply("old_bed_frame",new Vector3(47,.08f,23));Supply("metal_toolbox",new Vector3(39,.08f,24));Supply("vintage_suitcase",new Vector3(47,.08f,12));
  Sign("02 / MEDICAL\nRECEPTION",new Vector3(43.5f,2.6f,8.8f),0);
  // A second east-side building creates a covered alternative to the exposed outer road.
  Room("Diagnostics corridor",38,50,33,57,.03f,new[]{1,2},new[]{1,2},new[]{2,5},new[]{2,5});
  Wall("Diagnostics rooms",new Vector3(44,.03f,33),new Vector3(44,.03f,57),new[]{1,4,6});
  for(int i=0;i<3;i++){Supply("old_bed_frame",new Vector3(40,.08f,36+i*8));Supply("Shelf_01",new Vector3(48,.08f,36+i*8));}
  Supply("metal_toolbox",new Vector3(48,.08f,46));Supply("vintage_suitcase",new Vector3(40,.08f,51));
  // Cross-district gallery: multiple entrances, side rooms and an uninterrupted through route.
  Room("Transit gallery",-46,50,61,73,.03f,new[]{2,7,15,23,29},new[]{2,7,15,23,29},new[]{1,2},new[]{1,2});
  for(int x=-34;x<=38;x+=18)Wall("Gallery crosswall "+x,new Vector3(x,.03f,61),new Vector3(x,.03f,73),new[]{1,2});
  for(int x=-40;x<45;x+=12){Supply("Shelf_01",new Vector3(x,.08f,71));Supply("tool_cart",new Vector3(x+3,.08f,63));}
  Supply("metal_toolbox",new Vector3(-37,.08f,70));Supply("vintage_suitcase",new Vector3(20,.08f,70));
  Sign("03 / LOGISTICS  >",new Vector3(0,2.6f,60.8f),0);Sign("<  LOWER GARAGE",new Vector3(-31,2.6f,60.8f),0);
  // North garage ramp provides a second exit, independent of the original southern ramp.
  var retaining=GameObject.Find("Garage retaining walls");if(retaining){retaining.SetActive(false);}
  Quad("Garage west retaining",new(-80,-4,2),new(-80,-4,40),new(-80,0,40),new(-80,0,2),plaster);
  Quad("Garage east retaining",new(-45,-4,40),new(-45,-4,2),new(-45,0,2),new(-45,0,40),plaster);
  Quad("Garage north retaining",new(-73,-4,40),new(-45,-4,40),new(-45,0,40),new(-73,0,40),plaster);
  Quad("Garage north ramp",new(-79,-4,38),new(-79,.03f,54),new(-73,.03f,54),new(-73,-4,38),floor);
  Room("Garage dispatch",-79,-46,54,60,.03f,new[]{0,1,9},new[]{0,1,9},new[]{0,1},new[]{0,1});
  Supply("tool_cart",new Vector3(-69,.08f,58));Supply("metal_trash_can",new Vector3(-58,.08f,58));
  Sign("01 / GARAGE DISPATCH",new Vector3(-63,2.6f,53.8f),0);
  // Dress only physically solid source facades, preserving their existing door openings.
  Physics.SyncTransforms();for(float z=1.5f;z<52;z+=3)for(int level=0;level<2;level++){
   bool solid=true;foreach(float dz in new[]{-1.3f,0,1.3f})solid&=Physics.Raycast(new Vector3(5,1.5f+level*3,z+dz),Vector3.right,out var hit,3,CombatRay.EnvironmentMask,QueryTriggerInteraction.Ignore)&&hit.distance>2.2f;
   if(solid)Module("PanelWindow",new Vector3(7.60f,.5f+level*3,z),90);
  }
  foreach(var r in district.GetComponentsInChildren<MeshRenderer>())r.gameObject.isStatic=true;
  var director=UnityEngine.Object.FindFirstObjectByType<InfectedDirector>();director.spawnPoints=director.spawnPoints.Concat(new[]{new Vector3(40,0,46),new Vector3(46,0,20),new Vector3(-32,0,68),new Vector3(20,0,68),new Vector3(-67,0,57)}).ToArray();
  foreach(var p in new[]{("V12 route medical",new Vector3(40,.1f,18)),("V12 route diagnostics",new Vector3(47,.1f,45)),("V12 route gallery",new Vector3(-20,.1f,67)),("V12 route dispatch",new Vector3(-67,.1f,57)),("V12 route north ramp",new Vector3(-76,-3,42))}){var o=new GameObject(p.Item1);o.transform.SetParent(district);o.transform.position=p.Item2;}
  Bake();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
 }
 static GameObject Module(string id,Vector3 p,float yaw=0){var o=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+"/"+id+".prefab"));o.transform.SetParent(district,false);o.transform.position=p;o.transform.rotation=Quaternion.Euler(0,yaw,0);return o;}
 static void Wall(string name,Vector3 a,Vector3 b,int[] doors){int count=Mathf.RoundToInt(Vector3.Distance(a,b)/3);Vector3 d=(b-a).normalized;float yaw=Vector3.SignedAngle(Vector3.right,d,Vector3.up);
  for(int i=0;i<count;i++){var p=a+d*(i*3+1.5f);Module(doors.Contains(i)?"PanelDoor":i%3==1?"PanelWindow":"PanelSolid",p,yaw);Module("Cornice",p+Vector3.up*2.95f,yaw);}
 }
 static void Room(string name,float x0,float x1,float z0,float z1,float y,int[] south,int[] north,int[] west,int[] east){
  Quad(name+" floor",new(x0,y,z0),new(x0,y,z1),new(x1,y,z1),new(x1,y,z0),floor);Quad(name+" ceiling",new(x0,y+3.08f,z0),new(x0,y+3.08f,z1),new(x1,y+3.08f,z1),new(x1,y+3.08f,z0),plaster);
  Wall(name+" south",new(x0,y,z0),new(x1,y,z0),south);Wall(name+" north",new(x0,y,z1),new(x1,y,z1),north);Wall(name+" west",new(x0,y,z0),new(x0,y,z1),west);Wall(name+" east",new(x1,y,z0),new(x1,y,z1),east);
 }
 static void Quad(string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material mat){var mesh=new Mesh{name=name};mesh.vertices=new[]{a,b,c,d};mesh.triangles=new[]{0,1,2,0,2,3,2,1,0,3,2,0};float u=Vector3.Distance(a,b)/3,v=Vector3.Distance(b,c)/3;mesh.uv=new[]{Vector2.zero,new Vector2(u,0),new Vector2(u,v),new Vector2(0,v)};mesh.normals=Enumerable.Repeat(Vector3.Cross(b-a,c-a).normalized,4).ToArray();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Root+"/Surface"+(meshId++)+".asset");var o=new GameObject(name);o.transform.SetParent(district,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=mat;o.AddComponent<MeshCollider>().sharedMesh=mesh;}
 static void Supply(string id,Vector3 p){var o=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/TwinkleOps/Content/V11/"+id+".prefab"));o.transform.SetParent(district);o.transform.position=p;if(o.GetComponent<LootContainer>())foreach(var t in o.GetComponentsInChildren<Transform>())t.gameObject.layer=11;}
 static void Sign(string label,Vector3 p,float yaw){var o=new GameObject(label);o.transform.SetParent(district);o.transform.position=p;o.transform.rotation=Quaternion.Euler(0,yaw,0);var t=o.AddComponent<TextMesh>();t.text=label;t.fontSize=70;t.characterSize=.035f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.1f,.3f,.34f);}
 static void Bake(){var s=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();s.layerMask=CombatRay.EnvironmentMask&~(1<<11);s.BuildNavMesh();AssetDatabase.CreateAsset(s.navMeshData,Root+"/ConnectedNavigation.asset");}
 public static void Review(){EditorSceneManager.OpenScene(Scene);var player=UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();var report=new System.Text.StringBuilder();bool all=true;
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("V12 route")).Concat(UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None).Select(t=>t.transform))){var path=new NavMeshPath();bool ok=NavMesh.SamplePosition(t.position,out var h,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(player.transform.position,h.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;report.AppendLine(t.name+" "+t.position+" reachable="+ok);all&=ok;}
  File.WriteAllText("Logs/v12-routes.txt",report.ToString());ShaderUtil.allowAsyncCompilation=false;var cam=new GameObject("V12 review").AddComponent<Camera>();cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cam.farClipPlane=260;
  foreach(var shot in new[]{("reception",new Vector3(34,2,4),new Vector3(45,1.5f,18)),("gallery",new Vector3(-39,1.7f,67),new Vector3(28,1.7f,67)),("dispatch",new Vector3(-76,-2,44),new Vector3(-65,1,57)),("overview",new Vector3(-100,95,-90),new Vector3(0,0,40))}){cam.transform.position=shot.Item2;cam.transform.LookAt(shot.Item3);RaidReview.Shot(cam,"v12-"+shot.Item1);}
  if(!all)throw new Exception("V12 unreachable objectives; inspect Logs/v12-routes.txt");Debug.Log("V12_MAP_ROUTES_OK");
 }
}}
