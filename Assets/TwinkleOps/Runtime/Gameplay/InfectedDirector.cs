using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace TwinkleOps
{
    public sealed class InfectedDirector : MonoBehaviour
    {
        public Infected template;
        public Vector3[] spawnPoints;
        public int maximumAlive=22,poolSize=26;
        readonly List<Infected> pool=new();
        readonly Plane[] planes=new Plane[6];
        PlayerMotor player;float nextWave=20;int wave;
        public int Spawned {get;private set;}
        public int ActiveCount {get { int count=0;foreach(var z in pool)if(z.gameObject.activeSelf&&!z.Dead)count++;return count; }}
        void Start()
        {
            player=FindAnyObjectByType<PlayerMotor>();if(!template||!player){enabled=false;return;}
            foreach(var z in FindObjectsByType<Infected>(FindObjectsSortMode.None))pool.Add(z);
            while(pool.Count<poolSize)
            {
                var z=Instantiate(template,transform);z.gameObject.SetActive(false);pool.Add(z);
            }
            for(int i=0;i<pool.Count;i++)
            {
                var z=pool[i];z.recycled=Recycle;z.target=player.transform;
                // Preserve the textured character; vary stature without distorting skeleton proportions.
                z.transform.localScale=Vector3.one*(.94f+(i%4)*.035f);
                z.GetComponent<InfectedAppearance>()?.Apply(i);
            }
            nextWave=Time.time+20;
        }
        void Recycle(Infected z)=>z.gameObject.SetActive(false);
        void Update()
        {
            if(!player||Time.time<nextWave)return;nextWave=Time.time+Random.Range(14,23);
            int budget=Mathf.Min((++wave%3==0?5:2),maximumAlive-ActiveCount);if(budget<=0)return;
            GeometryUtility.CalculateFrustumPlanes(player.view,planes);
            int start=Random.Range(0,spawnPoints.Length);
            for(int i=0;i<spawnPoints.Length && budget>0;i++)
            {
                Vector3 p=spawnPoints[(start+i)%spawnPoints.Length];float distance=Vector3.Distance(p,player.transform.position);
                if(distance<18 || distance>62)continue;
                bool inView=GeometryUtility.TestPlanesAABB(planes,new Bounds(p+Vector3.up,Vector3.one*2));
                if(inView&&!Physics.Linecast(player.view.transform.position,p+Vector3.up*1.2f,CombatRay.EnvironmentMask,QueryTriggerInteraction.Ignore))continue;
                if(!NavMesh.SamplePosition(p,out var hit,1.5f,NavMesh.AllAreas))continue;
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(hit.position,player.transform.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                var zombie=pool.Find(z=>!z.gameObject.activeSelf);if(!zombie)break;
                zombie.ResetForSpawn(hit.position,player.transform);Spawned++;budget--;
            }
        }
    }
}
