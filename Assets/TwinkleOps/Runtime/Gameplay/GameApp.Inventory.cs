using System;using System.Linq;using UnityEngine;
namespace TwinkleOps {
public sealed partial class GameApp {
 string selected="",dragged="",rareItem="";bool dragSafe,dragRotated,dragMoved;Vector2 dragStart;float rareUntil;AudioSource uiAudio;AudioClip[] revealClips;
 public bool SearchOpen=>bagOpen&&container;
 public void OpenContainer(LootContainer box){if(!box||!Player||!Session.InRaid)return;container=box;box.Reveal();Debug.Log("TWINKLE_OPS_SEARCH_OPEN:"+box.displayName);bagOpen=true;paused=false;selected="";SetInput(false);}
 public void CloseInventory(){bagOpen=false;container=null;dragged="";SetInput(!paused);}
 void Do(Func<bool> action){try{if(!action())notice="操作未完成：请检查余额、容量或物品状态。";else notice="";}catch(Exception e){notice="未保存操作，物品保留："+e.Message;}}
 void DrawIcon(ItemSpec item,Rect r){int i=Array.IndexOf(Items.All,item);if(content&&content.itemIcons!=null&&i>=0&&i<content.itemIcons.Length&&content.itemIcons[i]){var icon=content.itemIcons[i];var uv=icon.textureRect;GUI.DrawTextureWithTexCoords(r,icon.texture,new Rect(uv.x/icon.texture.width,uv.y/icon.texture.height,uv.width/icon.texture.width,uv.height/icon.texture.height));}else GUI.Label(r,item.name);}
 void PlayReveal(Rarity rarity){if(!uiAudio){uiAudio=gameObject.AddComponent<AudioSource>();uiAudio.playOnAwake=false;uiAudio.spatialBlend=0;uiAudio.volume=.18f;}
  revealClips??=new AudioClip[6];int n=(int)rarity;if(!revealClips[n]){int size=12000+n*1800;var data=new float[size];for(int i=0;i<size;i++){float t=i/48000f;float env=Mathf.Exp(-t*13)*Mathf.Min(1,t*180);data[i]=env*(Mathf.Sin(t*(420+n*95)*2*Mathf.PI)+.25f*Mathf.Sin(t*(630+n*125)*2*Mathf.PI))*.35f;}var clip=AudioClip.Create("Original loot chime "+n,size,1,48000,false);clip.SetData(data,0);revealClips[n]=clip;}uiAudio.PlayOneShot(revealClips[n]);
 }
 void InventoryUI(){
  var matrix=GUI.matrix;float scale=Screen.width/1280f,height=Screen.height/scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  GUI.skin.label.fontSize=23;GUI.skin.button.fontSize=22;GUI.skin.box.fontSize=23;
  var panel=new Rect(18,16,1244,height-32);GUI.color=new Color(.045f,.075f,.085f,.97f);GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(38,28,850,34),container?"搜索 / "+container.displayName:"随身物资 / 背包");if(GUI.Button(new Rect(1110,25,125,42),"关闭"))CloseInventory();
  float cell=Mathf.Clamp((height-190)/5,48,72);var bagRect=new Rect(478,102,cell*6,cell*5);var safeRect=new Rect(1000,102,cell*2,cell*2);
  GUI.Label(new Rect(478,65,510,32),"背包  "+Session.Bag.Weight.ToString("F1")+" kg / ¥"+Session.Bag.TotalValue.ToString("N0"));GUI.Label(new Rect(995,65,240,32),"安全箱 · 2×2");
  DrawGrid(Session.Bag,bagRect,cell,false);DrawGrid(Session.Safe,safeRect,cell,true);
  GUI.skin.label.fontSize=18;GUI.Label(new Rect(995,safeRect.yMax+10,250,52),"安全箱内物品死亡保留\n14 kg 以上逐渐影响移速");GUI.skin.label.fontSize=23;
  if(container){
   int count=container.Contents.Count;lootScroll=GUI.BeginScrollView(new Rect(38,85,408,height-175),lootScroll,new Rect(0,0,382,Mathf.Max(height-180,count*100)));
   for(int i=0;i<count;i++){
    var r=new Rect(0,i*100,380,94);GUI.color=new Color(.11f,.15f,.17f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;var spec=Items.Get(container.Contents[i]);
    if(container.IsKnown(i)){GUI.color=RarityColors[(int)spec.rarity];GUI.DrawTexture(new Rect(0,r.y,4,94),Texture2D.whiteTexture);GUI.Label(new Rect(84,r.y+5,280,31),spec.name);GUI.color=Color.white;DrawIcon(spec,new Rect(11,r.y+12,64,64));GUI.skin.label.fontSize=20;GUI.Label(new Rect(84,r.y+38,290,28),"¥"+spec.value.ToString("N0")+"   "+spec.width+"×"+spec.height+" 格");GUI.skin.label.fontSize=23;
     if(GUI.Button(new Rect(280,r.y+60,90,29),"拾取")){if(!container.Take(i,Session.Bag))notice="背包空间不足";break;}
    }else{GUI.Label(new Rect(84,r.y+12,280,32),i==container.RevealedCount?"正在鉴定物资…":"未知物资");GUI.skin.label.fontSize=20;GUI.Label(new Rect(84,r.y+50,275,30),spec.width+"×"+spec.height+" 格 · 搜索后可拾取");GUI.skin.label.fontSize=23;if(i==container.RevealedCount)SearchRing(new Vector2(43,r.y+44),container.SearchProgress/Mathf.Max(.01f,container.ItemSearchSeconds));else GUI.Label(new Rect(28,r.y+20,55,50),"?");}
   }GUI.EndScrollView();
  }else{
   GUI.skin.label.wordWrap=true;GUI.Label(new Rect(38,100,385,100),"先选中背包中的物品，再使用、丢弃或放入安全箱。\n拖动物品调整位置，拖动时按 R 旋转。");GUI.skin.label.wordWrap=false;
   var entry=Session.Bag.Entries.Concat(Session.Safe.Entries).FirstOrDefault(e=>e.instanceId==selected);
   if(entry!=null){DrawIcon(entry.Spec,new Rect(44,215,96,96));GUI.color=RarityColors[(int)entry.Spec.rarity];GUI.Label(new Rect(155,212,275,36),entry.Spec.name);GUI.color=Color.white;GUI.Label(new Rect(155,253,285,64),"¥"+entry.Spec.value.ToString("N0")+"\n"+entry.Width+"×"+entry.Height+" 格 / "+entry.Spec.Weight.ToString("F1")+" kg");}
  }
  var item=Session.Bag.Entries.Concat(Session.Safe.Entries).FirstOrDefault(e=>e.instanceId==selected);float infoY=safeRect.yMax+70;
  if(item!=null){GUI.color=RarityColors[(int)item.Spec.rarity];GUI.Label(new Rect(995,infoY,245,34),item.Spec.name);GUI.color=Color.white;GUI.Label(new Rect(995,infoY+34,245,34),"¥"+item.Spec.value.ToString("N0"));}
  if(GUI.Button(new Rect(478,height-62,170,42),"整理背包"))Do(()=>Session.Bag.Sort());
  if(item!=null){bool protectedItem=Session.Safe.Entries.Any(e=>e.instanceId==item.instanceId);
   if(GUI.Button(new Rect(662,height-62,178,42),protectedItem?"移回背包":"放入安全箱"))Do(()=>protectedItem?Session.Unprotect(item.instanceId):Session.Protect(item.instanceId));
   if(!protectedItem){if((item.itemId=="bandage"||item.itemId=="medicine")&&GUI.Button(new Rect(851,height-62,155,42),"使用药品"))Do(()=>UseMedicine(item.instanceId));if(GUI.Button(new Rect(1018,height-62,155,42),"丢弃物品")){Drop(item);selected="";}}
  }
  GUI.skin.label.fontSize=18;GUI.color=Time.time<rareUntil?new Color(1,.8f,.35f):new Color(.7f,.8f,.8f);GUI.Label(new Rect(38,height-103,1160,34),Time.time<rareUntil?"发现贵重物资："+rareItem:string.IsNullOrEmpty(notice)?"搜索时战局仍在进行。关闭界面暂停搜索，重开继续；物资不会重新生成。":notice);GUI.color=Color.white;
  HandleDrag(bagRect,safeRect,cell);GUI.matrix=matrix;
 }
 void DrawGrid(Inventory inv,Rect area,float cell,bool safe){
  for(int y=0;y<inv.Rows;y++)for(int x=0;x<inv.Columns;x++){GUI.color=new Color(.16f,.21f,.23f);GUI.DrawTexture(new Rect(area.x+x*cell,area.y+y*cell,cell-2,cell-2),Texture2D.whiteTexture);}GUI.color=Color.white;
  foreach(var item in inv.Entries){var r=new Rect(area.x+item.x*cell,area.y+item.y*cell,item.Width*cell-3,item.Height*cell-3);var color=RarityColors[(int)item.Spec.rarity];GUI.color=new Color(color.r*.32f,color.g*.32f,color.b*.32f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(new Rect(r.x,r.y,r.width,3),Texture2D.whiteTexture);GUI.color=Color.white;float size=Mathf.Min(r.width,r.height)-10;DrawIcon(item.Spec,new Rect(r.center.x-size/2,r.center.y-size/2,size,size));if(selected==item.instanceId){GUI.color=Color.white;GUI.DrawTexture(new Rect(r.x,r.yMax-3,r.width,3),Texture2D.whiteTexture);}
   if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&r.Contains(Event.current.mousePosition)){selected=dragged=item.instanceId;dragSafe=safe;dragRotated=item.rotated;dragStart=Event.current.mousePosition;dragMoved=false;Event.current.Use();}
  }
 }
 void HandleDrag(Rect bagRect,Rect safeRect,float cell){if(string.IsNullOrEmpty(dragged))return;var ev=Event.current;var source=dragSafe?Session.Safe:Session.Bag;var entry=source.Entries.FirstOrDefault(e=>e.instanceId==dragged);if(entry==null){dragged="";return;}
  if(ev.type==EventType.MouseDrag){dragMoved|=Vector2.Distance(ev.mousePosition,dragStart)>5;ev.Use();}if(ev.type==EventType.KeyDown&&ev.keyCode==KeyCode.R){dragRotated=!dragRotated;ev.Use();}
  if(dragMoved){var p=ev.mousePosition;GUI.color=new Color(1,1,1,.7f);DrawIcon(entry.Spec,new Rect(p.x-25,p.y-25,50,50));GUI.color=Color.white;}
  if(ev.type==EventType.MouseUp){if(dragMoved){bool toSafe=safeRect.Contains(ev.mousePosition),toBag=bagRect.Contains(ev.mousePosition);if((toSafe&&dragSafe)||(toBag&&!dragSafe)){var a=dragSafe?safeRect:bagRect;if(!source.Move(dragged,(int)((ev.mousePosition.x-a.x)/cell),(int)((ev.mousePosition.y-a.y)/cell),dragRotated))notice="此位置没有足够空间，物品已留在原位。";}else if(toSafe)Do(()=>Session.Protect(dragged));else if(toBag)Do(()=>Session.Unprotect(dragged));}dragged="";ev.Use();}
 }
 static void SearchRing(Vector2 center,float progress){var matrix=GUI.matrix;for(int i=0;i<16;i++){GUI.color=i/16f<progress?new Color(.8f,.95f,.9f):new Color(.3f,.4f,.42f);GUIUtility.RotateAroundPivot(i*22.5f,center);GUI.DrawTexture(new Rect(center.x-2,center.y-27,4,8),Texture2D.whiteTexture);GUI.matrix=matrix;}GUI.color=Color.white;GUIUtility.RotateAroundPivot(Time.time*160,center);GUI.DrawTexture(new Rect(center.x+10,center.y+10,4,13),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.skin.label.fontSize=16;GUI.Label(new Rect(center.x-17,center.y-11,48,24),Mathf.RoundToInt(progress*100)+"%");GUI.skin.label.fontSize=23;}
}}
