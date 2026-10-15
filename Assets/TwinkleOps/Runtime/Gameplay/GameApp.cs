using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwinkleOps
{
    public sealed partial class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }
        public static Func<IProgressStore> ProgressStoreFactory;
        public ContentManifest content;
        public Font uiFont;
        public Texture2D lobbyBackdrop;
        public string saveNamespace="TwinkleOps.progress.v1.";
        public RaidSession Session { get; private set; }
        public PlayerMotor Player { get; private set; }
        Rifle rifle;
        float health=100, searching;
        public float RaidStartedAt {get;private set;}
        public float Health => health;
        public bool UseMedicine(string instanceId)
        {
            if(!Player || Session==null || !Session.InRaid || health>=100)return false;
            var item=Session.Bag.Entries.FirstOrDefault(x=>x.instanceId==instanceId);
            if(item==null)return false;
            float heal=item.itemId=="bandage"?25:item.itemId=="medicine"?55:0;
            if(heal<=0 || !Session.Bag.Remove(instanceId))return false;
            health=Mathf.Min(100,health+heal);notice="使用 "+item.Spec.name+"，恢复生命";return true;
        }
        LootContainer container, searchingContainer;
        ExtractionZone extraction;
        ExtractionZone[] exits=Array.Empty<ExtractionZone>();
        bool bagOpen, paused, loading;
        string notice="", fatal="";
        Vector2 stashScroll,lootScroll;
        string interactionHint="";
        static readonly Color[] RarityColors={new(.8f,.82f,.84f),new(.35f,.8f,.43f),new(.3f,.65f,1),new(.72f,.42f,1),new(1,.76f,.22f),new(1,.3f,.3f)};
        void Awake()
        {
            if(Instance && Instance!=this) { Destroy(gameObject); return; }
            Instance=this; DontDestroyOnLoad(gameObject); Application.targetFrameRate=60;
            try { Session=new RaidSession(ProgressStoreFactory!=null?ProgressStoreFactory():new PlayerPrefsStore(saveNamespace)); } catch(Exception e) { fatal=e.Message; }
            SceneManager.sceneLoaded+=Loaded;
            Debug.Log("TWINKLE_OPS_BOOT_OK");
        }
        void OnDestroy() { if(revealClips!=null)foreach(var clip in revealClips)if(clip)Destroy(clip);if(Instance==this) { SceneManager.sceneLoaded-=Loaded; Instance=null; } }
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            loading=false; Player=FindFirstObjectByType<PlayerMotor>(); rifle=FindFirstObjectByType<Rifle>(); extraction=FindFirstObjectByType<ExtractionZone>();
            exits=FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None);
            if(Player)
            {
                health=100;RaidStartedAt=Time.time;
                foreach(var z in FindObjectsByType<Infected>(FindObjectsSortMode.None)) z.target=Player.transform;
                SetInput(true);
                Debug.Log("TWINKLE_OPS_RAID_READY");
            }
            else { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        }
        void SetInput(bool active)
        {
            if(Player) Player.InputEnabled=active;
            Cursor.lockState=active && !Application.isMobilePlatform?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!active;
        }
        bool CanDeploy => !fatal.Any() && content && content.Missing().Count==0 && Application.CanStreamedLevelBeLoaded("InfectionZone");
        void Begin()
        {
            if(!CanDeploy || loading) return;
            try { Session.Begin(); loading=true; notice=""; SceneManager.LoadScene("InfectionZone"); }
            catch(Exception e) { notice="无法进入战局："+e.Message; loading=false; }
        }
        public void Damage(float amount)
        {
            if(loading || Session==null || !Session.InRaid) return;
            health=Mathf.Max(0,health-amount); if(health>0) return;
            try { Session.Die(); Return("行动失败。普通背包已遗失，安全箱内容已送回仓库。"); } catch(Exception e) { notice=e.Message; }
        }
        public void Extract()
        {
            if(loading) return;
            try { int value=Session.Bag.TotalValue+Session.Safe.TotalValue; if(Session.Extract()) Return("撤离成功，战利品价值 ¥"+value.ToString("N0")+"，已保存至仓库。"); }
            catch(Exception e) { notice="保存失败，仍保留本局物资："+e.Message; }
        }
        void Return(string text)
        {
            notice=text; bagOpen=paused=false; container=searchingContainer=null; searching=0; SetInput(false);
            loading=true; SceneManager.LoadScene("Lobby");
        }
        void Update()
        {
            if(!Player || Session==null || !Session.InRaid || loading) return;
            foreach(var exit in exits)if(exit && (!extraction || (exit.transform.position-Player.transform.position).sqrMagnitude<(extraction.transform.position-Player.transform.position).sqrMagnitude))extraction=exit;
            if(Player.BagPressed) { bagOpen=!bagOpen; container=null; SetInput(!bagOpen&&!paused); }
            if(!Player.InputEnabled && !bagOpen && !paused) { paused=true; SetInput(false); }
            if(bagOpen && container)
            {
                if(Vector3.Distance(Player.transform.position,container.transform.position)>4.3f)CloseInventory();
                else if(container.TickSearch(Time.deltaTime)){var item=Items.Get(container.Contents[container.RevealedCount-1]);PlayReveal(item.rarity);if(item.rarity>=Rarity.Purple){rareUntil=Time.time+2;rareItem=item.name;}}
            }
            if(!Player.InputEnabled) { interactionHint=""; return; }
            var ray=new Ray(Player.view.transform.position,Player.view.transform.forward);
            bool hit=Physics.Raycast(ray,out var info,2.8f,~(1<<8),QueryTriggerInteraction.Ignore);
            var aimed=hit?info.collider.GetComponentInParent<LootContainer>():null;
            interactionHint=aimed?"[ E ] 搜索 "+aimed.displayName:hit&&info.collider.GetComponentInParent<DroppedItem>()?"[ E ] 拾取掉落物资":"";
            if(Player.InteractPressed && hit)
            {
                if(aimed) OpenContainer(aimed);
                else { var drop=info.collider.GetComponentInParent<DroppedItem>(); if(drop && !drop.Take(Session.Bag)) notice="背包空间不足"; }
            }
        }
        void Drop(PlacedItem item)
        {
            if(!content.droppedLoot) return;
            var pos=Player.transform.position+Player.transform.forward*1.2f+Vector3.up;
            if(Physics.Raycast(pos,Vector3.down,out var ground,3,~(1<<8))) pos=ground.point+Vector3.up*.1f;
            var obj=Instantiate(content.droppedLoot,pos,Quaternion.identity);
            var dropped=obj.GetComponent<DroppedItem>(); if(!dropped) dropped=obj.AddComponent<DroppedItem>(); dropped.itemId=item.itemId; dropped.instanceId=item.instanceId;
            if(!Session.Bag.Remove(item.instanceId)) Destroy(obj);
        }
        void OnGUI()
        {
            if(uiFont) GUI.skin.font=uiFont;
            var old=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-720*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
            GUI.skin.label.fontSize=18; GUI.skin.button.fontSize=18; GUI.skin.box.fontSize=18;
            if(!Player) Lobby(); else RaidHUD();
            GUI.matrix=old; Player?.DrawTouch();
        }
        void Lobby()
        {
            if(lobbyBackdrop) { GUI.DrawTexture(new Rect(0,0,1280,720),lobbyBackdrop,ScaleMode.ScaleAndCrop); GUI.color=new Color(0,0,0,.68f); GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture); GUI.color=Color.white; }
            GUI.backgroundColor=new Color(.075f,.13f,.15f,.94f);
            GUI.Box(new Rect(24,24,1232,672),"");
            GUI.skin.label.fontSize=38; GUI.Label(new Rect(60,48,1000,64),"闪烁行动   /   TWINKLE OPS"); GUI.skin.label.fontSize=18;
            GUI.color=new Color(.55f,.78f,.78f); GUI.Label(new Rect(60,119,1080,34),CanDeploy?"隔离区 01 · 联通工业园    /    搜索 · 生存 · 撤离":"第一阶段开发中 · 战局等待验证"); GUI.color=Color.white;
            GUI.Label(new Rect(60,146,1110,60),string.IsNullOrEmpty(fatal)?notice:fatal);
            var missing=content?content.Missing():new System.Collections.Generic.List<string>{"资源清单"};
            GUI.Label(new Rect(60,218,550,40),CanDeploy?"感染区已就绪":"等待资源导入与场景验收");
            int y=263; foreach(var m in missing) { GUI.Label(new Rect(60,y,570,30),"· "+m); y+=30; }
            if(CanDeploy)
            {
                GUI.Label(new Rect(60,270,540,55),"携带装备   M4A1 / AKM / M9 / M870");
                GUI.Label(new Rect(60,328,550,65),"探索工地、临时医疗站、仓储区与地下车库。\n搜索不同容器；使用药品；选择绿色撤离点。");
                GUI.Label(new Rect(60,409,550,28),"补给站 · 买入价格为回收价的两倍");
                if(GUI.Button(new Rect(60,445,235,42),"绷带 ¥240"))Do(()=>Session.BuyMedicine("bandage"));
                if(GUI.Button(new Rect(310,445,235,42),"急救药品 ¥2,200"))Do(()=>Session.BuyMedicine("medicine"));
                GUI.Label(new Rect(60,497,550,28),"待带入物资："+Session.Progress.prepared.Count+" 件 · 死亡会失去");
                if(GUI.Button(new Rect(60,535,235,40),"退回整备物资"))Do(()=>{Session.UnpackAll();return true;});
                if(GUI.Button(new Rect(310,535,235,40),"开镜："+(PlayerPrefs.GetInt("TwinkleOps.controls.toggleAds",1)==1?"点击切换":"按住"))){PlayerPrefs.SetInt("TwinkleOps.controls.toggleAds",PlayerPrefs.GetInt("TwinkleOps.controls.toggleAds",1)==1?0:1);PlayerPrefs.Save();}

            }
            GUI.enabled=CanDeploy && !loading;
            if(GUI.Button(new Rect(60,602,510,54),"进入感染区")) Begin(); GUI.enabled=true;
            GUI.Label(new Rect(680,218,500,30),"仓库   /   STASH");
            if(Session==null) return;
            GUI.Label(new Rect(680,251,500,30),"余额 ¥"+Session.Progress.credits.ToString("N0")+" · 带回 ¥"+Session.Progress.lastRaidValue.ToString("N0"));
            GUI.Label(new Rect(680,282,500,30),"撤离 "+Session.Progress.successfulRaids+" / 失败 "+Session.Progress.deaths+" · 损失 ¥"+Session.Progress.lastLostValue.ToString("N0"));
            stashScroll=GUI.BeginScrollView(new Rect(670,320,550,285),stashScroll,new Rect(0,0,515,Mathf.Max(280,Session.Progress.stash.Count*78)));
            for(int i=0;i<Session.Progress.stash.Count;i++){
                var item=Items.Get(Session.Progress.stash[i]);DrawIcon(item,new Rect(5,i*78+6,55,55));GUI.color=RarityColors[(int)item.rarity];GUI.Label(new Rect(70,i*78,240,32),item.name);GUI.color=Color.white;GUI.Label(new Rect(70,i*78+32,235,30),"¥"+item.value.ToString("N0")+" / "+item.width+"×"+item.height);
                int index=i;if(GUI.Button(new Rect(310,i*78+12,85,46),"出售")){Do(()=>Session.Sell(index));break;}if(GUI.Button(new Rect(405,i*78+12,85,46),"装包")){Do(()=>Session.Pack(index));break;}
            }
            if(Session.Progress.stash.Count==0)GUI.Label(new Rect(10,0,500,60),"仓库为空。搜索撤离，或从补给站购买药品。");GUI.EndScrollView();
            if(GUI.Button(new Rect(680,621,510,38),"按价值整理仓库"))Do(()=>{Session.SortStash();return true;});
        }
        void ItemLabel(ItemSpec item,Rect rect)
        {
            var old=GUI.color; GUI.color=RarityColors[(int)item.rarity];
            GUI.Label(rect,item.name+"  ¥"+item.value.ToString("N0")+"  "+item.width+"×"+item.height); GUI.color=old;
        }
        void RaidHUD()
        {
            GUI.Label(new Rect(28,25,600,35),"生命 "+Mathf.CeilToInt(health)+"  |  ¥"+Session.Bag.TotalValue.ToString("N0")+"  |  "+Session.Bag.Weight.ToString("F1")+" kg");
            if(rifle) GUI.Label(new Rect(980,650,270,35),rifle.Reloading?"换弹中…":rifle.IsMelee?"M9 匕首 · 近战":rifle.WeaponName+"  "+rifle.ammo+" / "+rifle.reserve);
            GUI.Label(new Rect(28,670,940,30),"WASD 移动 / 右键切换瞄准 / R 换弹 / E 搜索 / Tab 背包 / 1–4 切枪 / V 匕首 / F3 调试");
            if(searchingContainer) GUI.Label(new Rect(520,410,500,40),"搜索中 "+searching.ToString("F1")+" / "+searchingContainer.searchSeconds+" 秒");
            if(extraction && extraction.Remaining<extraction.seconds) GUI.Label(new Rect(495,120,600,40),"撤离倒计时 "+extraction.Remaining.ToString("F1")+" 秒 · 离开区域会重置");
            GUI.Label(new Rect(30,75,1180,45),notice);
            if(extraction)GUI.Label(new Rect(900,65,350,35),extraction.Requirement(this));
            if(!bagOpen && !paused) GUI.Label(new Rect(430,435,610,35),interactionHint);
            if(extraction && Player)
            {
                var d=extraction.transform.position-Player.transform.position; float angle=Vector3.SignedAngle(Player.transform.forward,d,Vector3.up);
                string arrow=Mathf.Abs(angle)<18?"↑":angle>0?"→":"←";
                GUI.color=new Color(.4f,.95f,.75f); GUI.Label(new Rect(900,28,360,35),arrow+" "+extraction.displayName+"  "+d.magnitude.ToString("F0")+" m"); GUI.color=Color.white;
            }
            if(!bagOpen && !paused && rifle)
            {
                float gap=5+rifle.Spread*4; GUI.color=Time.time<rifle.HitUntil?Color.red:Color.white;
                GUI.DrawTexture(new Rect(640-gap-6,360,6,2),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(640+gap,360,6,2),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(640,360-gap-6,2,6),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(640,360+gap,2,6),Texture2D.whiteTexture); GUI.color=Color.white;
            }
            if(paused)
            { GUI.Box(new Rect(415,240,450,250),"菜单（战局继续运行）");if(GUI.Button(new Rect(460,300,360,55),"开镜模式："+(Player.ToggleAds?"点击切换":"按住")))Player.SetAdsMode(!Player.ToggleAds); if(GUI.Button(new Rect(460,385,360,60),"返回行动")) { paused=false; SetInput(true); } }
            if(bagOpen) InventoryUI();
        }
    }
}
