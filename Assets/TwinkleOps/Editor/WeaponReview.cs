using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace TwinkleOps.Editor
{
 public static class WeaponReview
 {
  public static void Capture()
  {
   EditorSceneManager.OpenScene("Assets/TwinkleOps/Scenes/InfectionZone.unity");
   GraphicsSettings.useScriptableRenderPipelineBatching=false; ShaderUtil.allowAsyncCompilation=false;
   var p=Object.FindFirstObjectByType<PlayerMotor>(); var r=p.GetComponent<Rifle>();
   r.weapons[0].model.gameObject.SetActive(false); var ak=r.weapons[1].model; ak.gameObject.SetActive(true);
   foreach(var a in Object.FindObjectsByType<ViewArms>(FindObjectsSortMode.None)) a.gameObject.SetActive(false);
   var rotations=new[]{new Vector3(0,90,180),new Vector3(0,270,180),new Vector3(0,90,0),new Vector3(0,270,0),new Vector3(90,180,0),new Vector3(270,180,0)};
   for(int i=0;i<rotations.Length;i++)
   {
    ak.localEulerAngles=rotations[i];
    var rt=new RenderTexture(960,600,24);rt.Create();
    for(int n=0;n<3;n++) RenderPipeline.SubmitRenderRequest(p.view,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
    RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
    File.WriteAllBytes("Logs/ak-orientation-"+i+".png",tex.EncodeToPNG());RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
   }
   Debug.Log("AK_ORIENTATION_REVIEW_READY");
  }
 }
}
