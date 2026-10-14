using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
namespace TwinkleOps.Editor
{
    public static class V11District
    {
        const string Root="Assets/TwinkleOps/Content/V11";
        static Transform district;
        static Material asphalt;
        static GameObject Instance(string path,Vector3 position,float yaw=0)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));obj.transform.SetParent(district,false);obj.transform.localPosition=position;obj.transform.Rotate(0,yaw,0,Space.World);return obj;
        }
        static GameObject Building(int id,Vector3 position,float yaw=0)
        {
            var b=Instance(OpenAssets.Root+"/Buildings/Structures_"+id+".prefab",position,yaw);
            foreach(var f in b.GetComponentsInChildren<MeshFilter>())
            {
                if(!f.GetComponent<Collider>())f.gameObject.AddComponent<MeshCollider>();
                if(f.name.EndsWith("-colonly"))f.GetComponent<Renderer>().enabled=false;
            }
            return b;
        }
        static void Surface(string name,float x1,float x2,float z1,float z2,float y1,float y2)
        {
            var mesh=new Mesh{name=name};mesh.vertices=new[]{new Vector3(x1,y1,z1),new Vector3(x1,y2,z2),new Vector3(x2,y2,z2),new Vector3(x2,y1,z1)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x/3,v.z/3)).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Root+"/"+name+".asset");
            var o=new GameObject(name);o.transform.SetParent(district,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=asphalt;o.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        static void Sign(string text,Vector3 pos,Color color)
        {
            var obj=new GameObject("District sign "+text);obj.transform.SetParent(district,false);obj.transform.position=pos;
            var label=obj.AddComponent<TextMesh>();label.text=text;label.fontSize=70;label.characterSize=.065f;label.anchor=TextAnchor.MiddleCenter;label.color=color;obj.transform.rotation=Quaternion.Euler(0,180,0);
        }
        static void Supply(string id,Vector3 position) => Instance(Root+"/"+id+".prefab",position);
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            if(GameObject.Find("V11 District Extensions"))throw new Exception("V11 district already exists; use review/fix methods instead of overwriting it.");
            district=new GameObject("V11 District Extensions").transform;
            asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/PolyHaven/asphalt_02/Asphalt.mat");
            Surface("East district paving",45,90,-34,115,0,0);Surface("North district paving",-45,45,72,115,0,0);
            Surface("West approach",-90,-45,-34,-10,0,0);Surface("West service lane",-90,-80,-10,115,0,0);Surface("West north lane",-80,-45,40,115,0,0);
            Surface("Garage ramp",-69,-57,-10,2,0,-4);Surface("Garage floor",-80,-45,2,40,-4,-4);
            // Three distinct reused architectural forms, with fitted interiors and paths between them.
            Building(5,new Vector3(64,0,15));Building(7,new Vector3(64,0,47),90);
            Building(10,new Vector3(3,0,91),90);Building(1,new Vector3(-24,0,93));Building(9,new Vector3(31,0,95));
            Building(10,new Vector3(-64,-4,23));
            Sign("FIELD CLINIC  /  02",new Vector3(64,3,3.8f),new Color(.7f,.95f,.88f));
            Sign("LOGISTICS  /  03",new Vector3(3,3,77),new Color(1,.75f,.32f));
            Sign("P  /  LOWER GARAGE",new Vector3(-63,2,-9),new Color(.5f,.75f,1));
            for(int i=0;i<4;i++) { Supply("old_bed_frame",new Vector3(59+(i%2)*7,.1f,11+(i/2)*7));Supply("metal_toolbox",new Vector3(59+(i%2)*7,.18f,13+(i/2)*7)); }
            Supply("tool_cart",new Vector3(64,.15f,17));Supply("Safe",new Vector3(65,.2f,20));
            for(int i=0;i<5;i++){Supply("Shelf_01",new Vector3(-3+i*3,.1f,96));Supply(i%2==0?"metal_toolbox":"vintage_suitcase",new Vector3(-3+i*3,.15f,91));}
            Supply("Safe",new Vector3(-24,.1f,94));Supply("vintage_suitcase",new Vector3(65,.2f,47));
            for(int i=0;i<4;i++) { Supply("tool_cart",new Vector3(-73+i*6,-3.95f,30));Supply(i%2==0?"metal_trash_can":"trashbag",new Vector3(-76+i*6,-3.95f,6)); }
            for(int i=0;i<14;i++)
            {
                var p=i<7?new Vector3(44,0,-16+i*12):new Vector3(-43,0,-14+(i-7)*12);
                Instance("Assets/ThirdParty/PolyHaven/concrete_road_barrier/concrete_road_barrier.prefab",p,i<7?90:0);
            }
            var rifle=UnityEngine.Object.FindFirstObjectByType<Rifle>();FixWeaponAxes(rifle);
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();surface.collectObjects=CollectObjects.All;surface.layerMask=CombatRay.EnvironmentMask;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize=true;surface.voxelSize=.12f;surface.BuildNavMesh();
            AssetDatabase.CreateAsset(surface.navMeshData,Root+"/ExpandedNavigation.asset");
            NavMesh.SamplePosition(rifle.transform.position,out var start,3,NavMesh.AllAreas);
            foreach(var box in UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None))PlaceReachable(box.transform,start.position,14);
            var exit=UnityEngine.Object.FindFirstObjectByType<ExtractionZone>();
            foreach(var spec in new[]{("仓储区撤离",new Vector3(3,0,110),14f),("车库撤离",new Vector3(-76,-4,35),11f)})
            { var e=UnityEngine.Object.Instantiate(exit);e.name=spec.Item1;e.displayName=spec.Item1;e.transform.position=spec.Item2;e.seconds=spec.Item3;PlaceReachable(e.transform,start.position,14); }
            var original=UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None)[0];
            var prefab=PrefabUtility.SaveAsPrefabAsset(original.gameObject,Root+"/PooledWorker.prefab");
            var spawn=new[]{new Vector3(55,0,8),new Vector3(70,0,22),new Vector3(65,0,46),new Vector3(-3,0,87),new Vector3(12,0,96),new Vector3(-25,0,95),new Vector3(-70,-4,18),new Vector3(-56,-4,29),new Vector3(26,0,64),new Vector3(-20,0,67),new Vector3(52,0,60),new Vector3(25,0,-18),new Vector3(-30,0,35),new Vector3(-56,0,49)};
            foreach(var pos in spawn.Take(8)) { var z=UnityEngine.Object.Instantiate(original);z.name="V11 roaming infected";z.transform.position=pos;PlaceReachable(z.transform,start.position,14); }
            var director=new GameObject("Infection encounter director").AddComponent<InfectedDirector>();director.template=prefab.GetComponent<Infected>();director.spawnPoints=spawn;director.maximumAlive=22;director.poolSize=26;
            rifle.motor.GetComponent<CharacterController>().stepOffset=.4f;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
        }
        static void FixWeaponAxes(Rifle rifle)
        {
            foreach(int i in new[]{2,3})
            {
                var root=rifle.weapons[i].model;root.gameObject.SetActive(true);var model=root.GetChild(0);model.localRotation=Quaternion.Euler(0,i==2?90:180,0)*(i==3?model.localRotation:Quaternion.identity);
                var bounds=OpenAssets.BoundsOf(model.gameObject);model.position+=root.position-bounds.center;root.gameObject.SetActive(false);
            }
        }
        static void PlaceReachable(Transform t,Vector3 start,int rings)
        {
            Vector3 original=t.position;
            for(int r=0;r<=rings;r++)for(int i=0;i<12;i++)
            {
                var p=original+new Vector3(Mathf.Sin(i*Mathf.PI/6)*r*.65f,0,Mathf.Cos(i*Mathf.PI/6)*r*.65f);
                if(!NavMesh.SamplePosition(p,out var hit,1.4f,NavMesh.AllAreas) || Mathf.Abs(hit.position.y-original.y)>1.5f)continue;
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(start,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                t.position=hit.position+Vector3.up*.04f;return;
            }
            Debug.LogWarning("Unreachable V11 object "+t.name+" "+original);
        }
        public static void Review()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            ShaderUtil.allowAsyncCompilation=false;
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();NavMesh.SamplePosition(player.transform.position,out var start,2,NavMesh.AllAreas);var report=new System.Text.StringBuilder();
            bool valid=true;
            foreach(var t in UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None).Select(b=>b.transform).Concat(UnityEngine.Object.FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None).Select(e=>e.transform)))
            { var path=new NavMeshPath();bool ok=NavMesh.SamplePosition(t.position,out var hit,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;report.AppendLine(t.name+" "+t.position+" reachable="+ok);valid&=ok; }
            for(int z=-15;z<=35;z+=5) {var p=new Vector3(-63,z<0?0:-4,z);var path=new NavMeshPath();bool sample=NavMesh.SamplePosition(p,out var hit,4,NavMesh.AllAreas);bool route=sample&&NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path);report.AppendLine("RAMP_SAMPLE "+p+" sample="+sample+" hit="+hit.position+" route="+route+" status="+path.status);}
            File.WriteAllText("Logs/v11-routes.txt",report.ToString());
            var cam=new GameObject("Review camera").AddComponent<Camera>();cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cam.fieldOfView=60;cam.farClipPlane=350;
            foreach(var shot in new[]{("clinic",new Vector3(43,12,-5),new Vector3(64,2,15)),("warehouse",new Vector3(-25,12,72),new Vector3(3,1,91)),("garage",new Vector3(-63,3,-12),new Vector3(-63,-2,20)),("district",new Vector3(-110,95,-100),new Vector3(0,0,35))})
            {cam.transform.position=shot.Item2;cam.transform.LookAt(shot.Item3);RaidReview.Shot(cam,"v11-"+shot.Item1);}
            var rifle=player.GetComponent<Rifle>();foreach(int i in new[]{2,3}){rifle.Equip(i,false);foreach(var arms in UnityEngine.Object.FindObjectsByType<ViewArms>(FindObjectsSortMode.None))arms.Pose();RaidReview.Shot(player.view,"v11-final-weapon-"+i);}
            if(!valid)throw new Exception("V11 routes incomplete; see Logs/v11-routes.txt and screenshots");
            Debug.Log("V11_DISTRICT_VERIFIED");
        }
        public static void RepairConnections()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");district=GameObject.Find("V11 District Extensions").transform;
            var boundary=GameObject.Find("Raid perimeter").GetComponentsInChildren<BoxCollider>();
            foreach(var box in boundary)
            {
                var p=box.transform.position;
                if(p.x<-40){box.transform.position=new Vector3(-89,2,40);box.size=new Vector3(1,14,150);}
                else if(p.x>40){box.transform.position=new Vector3(89,2,40);box.size=new Vector3(1,14,150);}
                else if(p.z>50){box.transform.position=new Vector3(0,2,114);box.size=new Vector3(180,14,1);}
                else {box.transform.position=new Vector3(0,2,-33);box.size=new Vector3(180,14,1);}
            }
            foreach(Transform child in district.Cast<Transform>().ToArray())
                if(child.name=="Structures_10") { var pos=child.position;UnityEngine.Object.DestroyImmediate(child.gameObject);Building(pos.x<0?2:8,pos,pos.x<0?0:90); }
            foreach(var sign in district.GetComponentsInChildren<TextMesh>()) {sign.transform.rotation=Quaternion.identity;sign.characterSize=.035f;}
            // Retaining walls close exposed soil edges around the actual lower level.
            asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/PolyHaven/asphalt_02/Asphalt.mat");
            if(!GameObject.Find("Garage retaining walls"))
            {
                var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();
                void Wall(Vector3 a,Vector3 b)
                {int i=vertices.Count;vertices.AddRange(new[]{a,b,b+Vector3.up*4,a+Vector3.up*4});uv.AddRange(new[]{Vector2.zero,new Vector2(Vector3.Distance(a,b)/3,0),new Vector2(Vector3.Distance(a,b)/3,1.3f),new Vector2(0,1.3f)});indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3,i+2,i+1,i,i+3,i+2,i});}
                Wall(new Vector3(-80,-4,2),new Vector3(-80,-4,40));Wall(new Vector3(-80,-4,40),new Vector3(-45,-4,40));Wall(new Vector3(-45,-4,40),new Vector3(-45,-4,2));
                var mesh=new Mesh{name="Garage retaining walls"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Root+"/GarageRetaining.asset");
                var obj=new GameObject("Garage retaining walls");obj.transform.SetParent(district);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(OpenAssets.Root+"/Buildings/ConcreteAtlas.mat");obj.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();surface.BuildNavMesh();AssetDatabase.CreateAsset(surface.navMeshData,Root+"/ExpandedNavigation.asset");
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();NavMesh.SamplePosition(player.transform.position,out var start,2,NavMesh.AllAreas);
            foreach(var loot in UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None))PlaceReachable(loot.transform,start.position,12);
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Infected>(FindObjectsSortMode.None))PlaceReachable(enemy.transform,start.position,12);
            foreach(var exit in UnityEngine.Object.FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None))PlaceReachable(exit.transform,start.position,12);
            // Preserve useful upper-floor loot locations only when stairs provide a complete route.
            if(!GameObject.Find("Upper floor valuables"))
            {
                foreach(var v in NavMesh.CalculateTriangulation().vertices.Where(v=>v.y>4 && v.x>54 && v.x<74).OrderBy(v=>v.y))
                {var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,v,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;var o=Instance(Root+"/vintage_suitcase.prefab",v+Vector3.up*.05f);o.name="Upper floor valuables";break;}
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
        }
        public static void FinalPolish()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            district=GameObject.Find("V11 District Extensions").transform;
            var walls=GameObject.Find("Garage retaining walls");walls.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/PolyHaven/concrete_road_barrier/URP.mat");
            AccessRamp("Clinic access ramp",new Vector3(51,0,14.5f),new Vector3(54.5f,.55f,14.5f),4);
            AccessRamp("Warehouse access ramp",new Vector3(0,0,76),new Vector3(0,.55f,80.5f),4);
            AccessRamp("Garage access ramp",new Vector3(-78,-4,31),new Vector3(-74,-2.95f,31),3);
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);tags.FindProperty("layers").GetArrayElementAtIndex(11).stringValue="SearchContainer";tags.ApplyModifiedProperties();
            var containers=UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
            foreach(var box in containers)foreach(var tr in box.GetComponentsInChildren<Transform>())tr.gameObject.layer=11;
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();surface.layerMask=CombatRay.EnvironmentMask&~(1<<11);surface.BuildNavMesh();AssetDatabase.CreateAsset(surface.navMeshData,Root+"/ExpandedNavigation.asset");
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();NavMesh.SamplePosition(player.transform.position,out var start,3,NavMesh.AllAreas);
            var upperLoot=GameObject.Find("Upper floor valuables");
            var tri=NavMesh.CalculateTriangulation();var landings=new List<Vector3>();
            for(int i=0;i<tri.indices.Length;i+=3)
            {
                var a=tri.vertices[tri.indices[i]];var b=tri.vertices[tri.indices[i+1]];var c=tri.vertices[tri.indices[i+2]];var center=(a+b+c)/3;
                if(center.x<55||center.x>74||center.z<5||center.z>25||center.y<6||center.y>8||Mathf.Abs(a.y-b.y)>.08f||Mathf.Abs(b.y-c.y)>.08f||Vector3.Cross(b-a,c-a).magnitude<3)continue;
                var path=new NavMeshPath();if(NavMesh.CalculatePath(start.position,center,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)landings.Add(center);
            }
            if(landings.Count==0)throw new Exception("No connected clinic second-floor landing");
            var landing=landings.OrderBy(p=>Vector3.Distance(p,new Vector3(63,6.5f,18))).First();
            var waypoint=GameObject.Find("Clinic upper floor waypoint")??new GameObject("Clinic upper floor waypoint");waypoint.transform.position=landing;
            if(upperLoot)
            {
                var spot=landings.Where(p=>Vector3.Distance(p,landing)>2).OrderBy(p=>Vector3.Distance(p,landing)).First();upperLoot.transform.position=spot;
            }
            var occupied=new List<Vector3>();
            foreach(var box in containers)
            {
                var p=box.transform.position;
                if(occupied.Any(v=>Vector3.Distance(v,p)<1.2f))box.transform.position+=Vector3.right*1.5f;
                PlaceReachable(box.transform,start.position,10);
                // Navigation voxels can sit above the actual floor; snap art to the physical surface.
                if(Physics.Raycast(box.transform.position+Vector3.up*.2f,Vector3.down,out var floor,2.1f,CombatRay.EnvironmentMask&~(1<<11),QueryTriggerInteraction.Ignore))box.transform.position=floor.point+Vector3.up*.025f;
                occupied.Add(box.transform.position);
                var obstacle=box.GetComponent<NavMeshObstacle>();if(!obstacle)obstacle=box.gameObject.AddComponent<NavMeshObstacle>();
                var collider=box.GetComponent<BoxCollider>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=collider.center;obstacle.size=collider.size;obstacle.carving=true;obstacle.carveOnlyStationary=true;obstacle.carvingTimeToStationary=.1f;
            }
            // Use the current credits for both scene content validation and downloaded builds.
            var content=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);content.credits=File.ReadAllText("Docs/CREDITS.md");EditorUtility.SetDirty(content);
            var cam=player.view;cam.farClipPlane=220;
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(r.gameObject.layer!=8 && r.gameObject.layer!=11)r.gameObject.isStatic=true;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
        }
        static void AccessRamp(string name,Vector3 low,Vector3 high,float width)
        {
            if(GameObject.Find(name))return;
            var direction=high-low;direction.y=0;Vector3 side=Vector3.Cross(direction.normalized,Vector3.up)*width/2;
            var mesh=new Mesh{name=name};mesh.vertices=new[]{low-side,high-side,high+side,low+side};mesh.triangles=new[]{0,2,1,0,3,2};mesh.uv=new[]{Vector2.zero,new Vector2(0,2),new Vector2(2,2),new Vector2(2,0)};mesh.RecalculateNormals();mesh.RecalculateTangents();
            // Keep the walkable face upward regardless of ramp direction.
            if(mesh.normals[0].y<0){mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();}
            AssetDatabase.CreateAsset(mesh,Root+"/"+name+".asset");var obj=new GameObject(name);obj.transform.SetParent(district);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/PolyHaven/concrete_road_barrier/URP.mat");obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
