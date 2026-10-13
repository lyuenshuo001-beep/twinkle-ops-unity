using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwinkleOps
{
    public sealed class GameApp : MonoBehaviour
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
        LootContainer container, searchingContainer;
        ExtractionZone extraction;
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
        void OnDestroy() { if(Instance==this) { SceneManager.sceneLoaded-=Loaded; Instance=null; } }
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            loading=false; Player=FindFirstObjectByType<PlayerMotor>(); rifle=FindFirstObjectByType<Rifle>(); extraction=FindFirstObjectByType<ExtractionZone>();
            if(Player)
            {
                health=100;
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
            try { Session.Die(); Return("行动失败。本局物资已遗失，仓库物资保留。"); } catch(Exception e) { notice=e.Message; }
        }
        public void Extract()
        {
            if(loading) return;
            try { int value=Session.Bag.TotalValue; if(Session.Extract()) Return("撤离成功，战利品价值 ¥"+value.ToString("N0")+"，已保存至仓库。"); }
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
            if(Player.BagPressed) { bagOpen=!bagOpen; container=null; SetInput(!bagOpen&&!paused); }
            if(!Player.InputEnabled && !bagOpen && !paused) { paused=true; SetInput(false); }
            if(!Player.InputEnabled) { interactionHint=""; return; }
            var ray=new Ray(Player.view.transform.position,Player.view.transform.forward);
            bool hit=Physics.Raycast(ray,out var info,2.8f,~(1<<8),QueryTriggerInteraction.Ignore);
            var aimed=hit?info.collider.GetComponentInParent<LootContainer>():null;
            interactionHint=aimed?"[ E ] 搜索 "+aimed.displayName:hit&&info.collider.GetComponentInParent<DroppedItem>()?"[ E ] 拾取掉落物资":"";
            if(searchingContainer)
            {
                if(aimed!=searchingContainer) { searchingContainer=null; searching=0; }
                else
                {
                    searching+=Time.deltaTime;
                    if(searching>=searchingContainer.searchSeconds)
                    { container=searchingContainer; container.Reveal(); searchingContainer=null; bagOpen=true; SetInput(false); }
                }
            }
            if(Player.InteractPressed && hit)
            {
                if(aimed) { searchingContainer=aimed; searching=0; }
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
            GUI.color=new Color(.55f,.78f,.78f); GUI.Label(new Rect(60,119,1080,34),CanDeploy?"隔离区 01 · 废弃工地    /    搜索 · 生存 · 撤离":"第一阶段开发中 · 战局等待验证"); GUI.color=Color.white;
            GUI.Label(new Rect(60,146,1110,60),string.IsNullOrEmpty(fatal)?notice:fatal);
            var missing=content?content.Missing():new System.Collections.Generic.List<string>{"资源清单"};
            GUI.Label(new Rect(60,218,550,40),CanDeploy?"感染区已就绪":"等待资源导入与场景验收");
            int y=263; foreach(var m in missing) { GUI.Label(new Rect(60,y,570,30),"· "+m); y+=30; }
            if(CanDeploy)
            {
                GUI.Label(new Rect(60,270,540,55),"携带装备   M4A1 / AKM · 各 30 + 120 发");
                GUI.Label(new Rect(60,328,550,65),"探索 10 个物资箱，收集六档品质战利品。\n前往北侧绿色撤离区，坚守 8 秒。");
                GUI.Label(new Rect(60,420,550,75),"成功撤离：战利品进入仓库。\n行动失败或中途退出：失去本局物资。");
                GUI.color=new Color(.6f,.68f,.7f); GUI.Label(new Rect(60,524,560,50),"首次进入需载入场景。PC 推荐键鼠与全屏。\n当前为单人早期试玩，武器动作仍在打磨。"); GUI.color=Color.white;
            }
            GUI.enabled=CanDeploy && !loading;
            if(GUI.Button(new Rect(60,602,510,54),"进入感染区")) Begin(); GUI.enabled=true;
            GUI.Label(new Rect(680,218,500,30),"仓库   /   STASH");
            if(Session==null) return;
            GUI.Label(new Rect(680,253,500,30),"成功撤离 "+Session.Progress.successfulRaids+"    失败 "+Session.Progress.deaths);
            stashScroll=GUI.BeginScrollView(new Rect(670,300,510,340),stashScroll,new Rect(0,0,480,Mathf.Max(330,Session.Progress.stash.Count*34)));
            for(int i=0;i<Session.Progress.stash.Count;i++) ItemLabel(Items.Get(Session.Progress.stash[i]),new Rect(10,i*34,470,30));
            if(Session.Progress.stash.Count==0) GUI.Label(new Rect(10,0,470,50),"仓库为空。撤离后的战利品将在这里保存。");
            GUI.EndScrollView();
        }
        void ItemLabel(ItemSpec item,Rect rect)
        {
            var old=GUI.color; GUI.color=RarityColors[(int)item.rarity];
            GUI.Label(rect,item.name+"  ¥"+item.value.ToString("N0")+"  "+item.width+"×"+item.height); GUI.color=old;
        }
        void RaidHUD()
        {
            GUI.Label(new Rect(28,25,600,35),"生命 "+Mathf.CeilToInt(health)+"  |  背包 ¥"+Session.Bag.TotalValue.ToString("N0"));
            if(rifle) GUI.Label(new Rect(980,650,270,35),rifle.Reloading?"换弹中…":rifle.WeaponName+"  "+rifle.ammo+" / "+rifle.reserve);
            GUI.Label(new Rect(28,670,940,30),"WASD 移动 / 右键瞄准 / R 换弹 / E 搜索 / Tab 背包 / 1·2·Q 切枪 / Esc 菜单");
            if(searchingContainer) GUI.Label(new Rect(520,410,500,40),"搜索中 "+searching.ToString("F1")+" / "+searchingContainer.searchSeconds+" 秒");
            if(extraction && extraction.Remaining<extraction.seconds) GUI.Label(new Rect(495,120,600,40),"撤离倒计时 "+extraction.Remaining.ToString("F1")+" 秒 · 离开区域会重置");
            GUI.Label(new Rect(30,75,1180,45),notice);
            if(!bagOpen && !paused) GUI.Label(new Rect(430,435,610,35),interactionHint);
            if(extraction && Player)
            {
                var d=extraction.transform.position-Player.transform.position; float angle=Vector3.SignedAngle(Player.transform.forward,d,Vector3.up);
                string arrow=Mathf.Abs(angle)<18?"↑":angle>0?"→":"←";
                GUI.color=new Color(.4f,.95f,.75f); GUI.Label(new Rect(930,28,320,35),arrow+" 北侧撤离  "+d.magnitude.ToString("F0")+" m"); GUI.color=Color.white;
            }
            if(!bagOpen && !paused && rifle)
            {
                float gap=5+rifle.Spread*4; GUI.color=Time.time<rifle.HitUntil?Color.red:Color.white;
                GUI.DrawTexture(new Rect(640-gap-6,360,6,2),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(640+gap,360,6,2),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(640,360-gap-6,2,6),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(640,360+gap,2,6),Texture2D.whiteTexture); GUI.color=Color.white;
            }
            if(paused)
            { GUI.Box(new Rect(415,260,450,170),"菜单（战局继续运行）"); if(GUI.Button(new Rect(460,320,360,60),"返回行动")) { paused=false; SetInput(true); } }
            if(bagOpen) InventoryUI();
        }
        void InventoryUI()
        {
            GUI.Box(new Rect(170,130,940,500),"背包 · 战局继续运行");
            for(int y=0;y<Session.Bag.Rows;y++) for(int x=0;x<Session.Bag.Columns;x++) GUI.Box(new Rect(195+x*59,185+y*59,57,57),"");
            foreach(var item in Session.Bag.Entries.ToArray())
            {
                var r=new Rect(195+item.x*59,185+item.y*59,item.Width*59-2,item.Height*59-2);
                GUI.color=RarityColors[(int)item.Spec.rarity];
                if(GUI.Button(r,item.Spec.name)) notice=item.Spec.name+" · ¥"+item.Spec.value+" · 点击下方列表可丢弃";
                GUI.color=Color.white;
                int iconIndex=Array.IndexOf(Items.All,item.Spec);
                if(content.itemIcons!=null && iconIndex>=0 && iconIndex<content.itemIcons.Length && content.itemIcons[iconIndex])
                { var icon=content.itemIcons[iconIndex]; var uv=icon.textureRect; uv=new Rect(uv.x/icon.texture.width,uv.y/icon.texture.height,uv.width/icon.texture.width,uv.height/icon.texture.height); GUI.DrawTextureWithTexCoords(new Rect(r.x+4,r.y+3,25,25),icon.texture,uv); }
            }
            if(GUI.Button(new Rect(195,500,165,45),"整理背包") && !Session.Bag.Sort()) notice="当前布局已保留，未找到更紧凑布局。";
            if(GUI.Button(new Rect(375,500,165,45),"关闭")) { bagOpen=false; container=null; SetInput(!paused); }
            var rows=container?container.Contents:null;
            GUI.Label(new Rect(600,172,450,32),container?container.displayName:"携带物资 · 点击丢弃");
            int count=rows?.Count ?? Session.Bag.Entries.Count;
            lootScroll=GUI.BeginScrollView(new Rect(590,215,490,335),lootScroll,new Rect(0,0,465,Mathf.Max(325,count*55)));
            for(int i=0;i<count;i++)
            {
                var item=Items.Get(rows!=null?rows[i]:Session.Bag.Entries[i].itemId); ItemLabel(item,new Rect(5,i*55,345,48));
                if(GUI.Button(new Rect(353,i*55,105,44),rows!=null?"拾取":"丢弃"))
                { if(rows!=null) { if(!container.Take(i,Session.Bag)) notice="背包空间不足"; } else Drop(Session.Bag.Entries[i]); break; }
            }
            GUI.EndScrollView();
        }
    }
}
