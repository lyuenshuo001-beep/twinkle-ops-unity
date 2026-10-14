using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TwinkleOps.Editor
{
    public static class V11Variants
    {
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
            var infected=Object.FindObjectsByType<Infected>(FindObjectsSortMode.None);var source=infected[0].GetComponentInChildren<SkinnedMeshRenderer>();var original=source.sharedMesh;
            var modelPath=AssetDatabase.GetAssetPath(original);var importer=AssetImporter.GetAtPath(modelPath) as ModelImporter;if(importer&&!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var meshes=new Mesh[3];var materials=new Material[3];
            var head=infected[0].animator.GetBoneTransform(HumanBodyBones.Head);int headIndex=System.Array.IndexOf(source.bones,head);
            for(int i=0;i<3;i++)
            {
                string path="Assets/TwinkleOps/Content/V11/WorkerSilhouette"+i+".asset";meshes[i]=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!meshes[i])
                {
                    var mesh=Object.Instantiate(original);mesh.name="Worker silhouette "+i;var vertices=mesh.vertices;var weights=mesh.boneWeights;
                    if(headIndex>=0 && i>0)
                    {
                        var bind=mesh.bindposes[headIndex];var inverse=bind.inverse;
                        for(int v=0;v<vertices.Length;v++)
                        {
                            var w=weights[v];float influence=(w.boneIndex0==headIndex?w.weight0:0)+(w.boneIndex1==headIndex?w.weight1:0)+(w.boneIndex2==headIndex?w.weight2:0)+(w.boneIndex3==headIndex?w.weight3:0);
                            if(influence<.01f)continue;var bone=bind.MultiplyPoint3x4(vertices[v]);bone=Vector3.Scale(bone,i==1?new Vector3(.91f,1.035f,.95f):new Vector3(1.10f,.97f,1.05f));vertices[v]=Vector3.Lerp(vertices[v],inverse.MultiplyPoint3x4(bone),influence);
                        }
                    }
                    mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);meshes[i]=mesh;
                }
                string matPath="Assets/TwinkleOps/Content/V11/WorkerOutfit"+i+".mat";materials[i]=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(!materials[i]){materials[i]=new Material(source.sharedMaterial);AssetDatabase.CreateAsset(materials[i],matPath);}
                materials[i].SetColor("_BaseColor",i==0?Color.white:i==1?new Color(.8f,.9f,1):new Color(.95f,.86f,.72f));EditorUtility.SetDirty(materials[i]);
            }
            for(int i=0;i<infected.Length;i++)
            {
                var z=infected[i];var appearance=z.GetComponent<InfectedAppearance>()??z.gameObject.AddComponent<InfectedAppearance>();appearance.skin=z.GetComponentInChildren<SkinnedMeshRenderer>();appearance.silhouettes=meshes;appearance.outfits=materials;appearance.Apply(i);
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(infected[0].gameObject,"Assets/TwinkleOps/Content/V11/PooledWorker.prefab");Object.FindAnyObjectByType<InfectedDirector>().template=prefab.GetComponent<Infected>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();V11Daylight.Apply();Debug.Log("V11_VARIANTS_AND_DAYLIGHT_READY");
        }
    }
}
