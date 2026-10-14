using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace TwinkleOps
{
    public sealed class CombatDebug : MonoBehaviour
    {
        public static bool Visible;
        public static string LastHit="";
        static readonly List<(Vector3 a,Vector3 b,float expiry)> shots=new();
        readonly List<LineRenderer> lines=new();
        Material material;float refresh;
        MaterialPropertyBlock properties;
        public static void Trace(Vector3 a,Vector3 b) { if(Visible) { if(shots.Count>24)shots.RemoveAt(0);shots.Add((a,b,Time.time+2)); } }
        void Update()
        {
            if(Keyboard.current?.f3Key.wasPressedThisFrame==true) Visible=!Visible;
            if(!Visible) { foreach(var l in lines) if(l)l.enabled=false; return; }
            if(Time.unscaledTime<refresh)return;refresh=Time.unscaledTime+.05f;
            if(!material)material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            int index=0;
            foreach(var h in FindObjectsByType<InfectedHitbox>(FindObjectsSortMode.None))
            {
                var c=h.GetComponent<CapsuleCollider>();if(!c.enabled)continue;
                var b=c.bounds;var a=b.min;var z=b.max;
                var p=new[]{new Vector3(a.x,a.y,a.z),new Vector3(z.x,a.y,a.z),new Vector3(z.x,z.y,a.z),new Vector3(a.x,z.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,z.z),new Vector3(z.x,a.y,z.z),new Vector3(z.x,z.y,z.z),new Vector3(a.x,z.y,z.z),new Vector3(a.x,a.y,z.z),new Vector3(a.x,z.y,z.z),new Vector3(a.x,z.y,a.z),new Vector3(z.x,z.y,a.z),new Vector3(z.x,z.y,z.z),new Vector3(z.x,a.y,z.z),new Vector3(z.x,a.y,a.z)};
                Draw(index++,p,h.region==HitRegion.Head?Color.red:h.region==HitRegion.Body?Color.green:Color.cyan);
            }
            shots.RemoveAll(s=>s.expiry<Time.time);foreach(var s in shots)Draw(index++,new[]{s.a,s.b},Color.yellow);
            for(int i=index;i<lines.Count;i++)lines[i].enabled=false;
        }
        void Draw(int i,Vector3[] p,Color color)
        {
            if(i==lines.Count) { var go=new GameObject("Debug hitbox wire");go.transform.SetParent(transform);go.layer=2;var l=go.AddComponent<LineRenderer>();l.sharedMaterial=material;l.startWidth=l.endWidth=.009f;l.useWorldSpace=true;lines.Add(l); }
            var line=lines[i];line.enabled=true;line.startColor=line.endColor=color;line.positionCount=p.Length;line.SetPositions(p);
            properties??=new MaterialPropertyBlock();properties.SetColor("_BaseColor",color);line.SetPropertyBlock(properties);
        }
        void OnGUI() { if(Visible)GUI.Label(new Rect(20,140,600,40),"F3 HITBOX DEBUG  Head x2.25 / Body x1 / Limbs x0.65 | "+LastHit); }
        void OnDestroy(){if(material)Destroy(material);shots.Clear();}
    }
}
