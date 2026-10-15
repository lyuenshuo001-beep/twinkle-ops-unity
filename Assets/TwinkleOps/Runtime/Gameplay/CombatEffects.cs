using UnityEngine;
namespace TwinkleOps {
public sealed class CombatEffects:MonoBehaviour {
 public Material dustMaterial,decalMaterial;static CombatEffects instance;
 sealed class ImpactSlot{public ParticleSystem particles;public Transform mark;public Renderer renderer;public float expires;public MaterialPropertyBlock block;}
 ImpactSlot[] pool;int cursor;Mesh quad;
 public int Impacts {get;private set;}
 void Awake(){instance=this;pool=new ImpactSlot[32];quad=new Mesh{name="Bullet impact quad"};quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};quad.triangles=new[]{0,1,2,0,2,3};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.RecalculateNormals();
  for(int i=0;i<pool.Length;i++){var root=new GameObject("Pooled impact "+i);root.transform.SetParent(transform);root.layer=2;var ps=root.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=.5f;main.maxParticles=14;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.12f,.4f);main.gravityModifier=.6f;var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=35;shape.radius=.025f;root.GetComponent<ParticleSystemRenderer>().sharedMaterial=dustMaterial;
   var mark=new GameObject("Impact mark");mark.layer=2;mark.transform.SetParent(transform);mark.AddComponent<MeshFilter>().sharedMesh=quad;var renderer=mark.AddComponent<MeshRenderer>();renderer.sharedMaterial=decalMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;pool[i]=new ImpactSlot{particles=ps,mark=mark.transform,renderer=renderer,block=new MaterialPropertyBlock()};}
 }
 public static void Hit(RaycastHit hit,bool infected){if(instance)instance.Spawn(hit,infected);}
 void Spawn(RaycastHit hit,bool infected){if(!dustMaterial||!decalMaterial)return;Impacts++;var slot=pool[cursor++%pool.Length];slot.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);slot.particles.transform.SetPositionAndRotation(hit.point+hit.normal*.025f,Quaternion.LookRotation(hit.normal));var main=slot.particles.main;main.startSpeed=new ParticleSystem.MinMaxCurve(.5f,infected?1.4f:2.6f);main.startSize=new ParticleSystem.MinMaxCurve(.025f,infected?.055f:.13f);main.startColor=infected?new Color(.38f,.09f,.06f,.7f):new Color(.62f,.57f,.47f,.7f);slot.particles.Emit(infected?5:9);
  slot.renderer.enabled=!infected;if(!infected){slot.mark.SetPositionAndRotation(hit.point+hit.normal*.012f,Quaternion.LookRotation(hit.normal)*Quaternion.Euler(0,0,Random.Range(0,360)));slot.mark.localScale=Vector3.one*Random.Range(.07f,.13f);slot.expires=Time.time+12;slot.block.SetColor("_BaseColor",new Color(.12f,.10f,.08f,.75f));slot.renderer.SetPropertyBlock(slot.block);}
 }
 void Update(){foreach(var slot in pool)if(slot.renderer.enabled){float fade=Mathf.Clamp01((slot.expires-Time.time)/2);if(fade<=0)slot.renderer.enabled=false;else{slot.block.SetColor("_BaseColor",new Color(.12f,.10f,.08f,.75f*fade));slot.renderer.SetPropertyBlock(slot.block);}}}
 void OnDestroy(){if(instance==this)instance=null;if(quad)Destroy(quad);}
}}
