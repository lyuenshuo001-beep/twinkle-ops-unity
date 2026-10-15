using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;
namespace TwinkleOps.Editor {
public static class V12Sky {
 public static void Apply(){
  const string folder="Assets/ThirdParty/V12/Sky";const string file=folder+"/kloofendal_48d_partly_cloudy_puresky.hdr";
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Compressed;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
  EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");var sky=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Daylight.mat");if(!sky){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,folder+"/Daylight.mat");}sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(file));sky.SetFloat("_Exposure",.8f);sky.SetFloat("_Rotation",140);EditorUtility.SetDirty(sky);RenderSettings.skybox=sky;
  foreach(var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))cam.clearFlags=CameraClearFlags.Skybox;
  PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Brotli;PlayerSettings.WebGL.decompressionFallback=true;
  var content=AssetDatabase.LoadAssetAtPath<ContentManifest>(ProjectSetup.ManifestPath);content.credits=File.ReadAllText("Docs/CREDITS.md");EditorUtility.SetDirty(content);
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();V12Map.Review();Debug.Log("V12_SKY_READY");
 }
}}
