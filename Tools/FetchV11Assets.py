import urllib.request,json,pathlib,hashlib,re,zipfile,time,shutil
base=pathlib.Path(r'D:\GameProjects\TwinkleOps');src=base/'SourceAssets/V11';dst=base/'Assets/ThirdParty/V11';dst.mkdir(parents=True,exist_ok=True)
def get(url):
 for attempt in range(3):
  try:return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'TwinkleOps-independent-game-development'}),timeout=90).read()
  except Exception:
   if attempt==2:raise
   time.sleep(2)
catalog=json.loads((src/'polyhaven-catalog.json').read_text())
records=[]
for asset in ['metal_trash_can','metal_toolbox','vintage_suitcase','trashbag','old_bed_frame','Shelf_01','tool_cart','wooden_pallet']:
 if asset not in catalog:continue
 data=json.loads(get('https://api.polyhaven.com/files/'+asset));(src/(asset+'-files.json')).write_text(json.dumps(data));out=dst/asset;out.mkdir(exist_ok=True)
 fbx=data['fbx']['1k']['fbx'];files=[('model.fbx',fbx)]
 for channel,term in [('diff','diff'),('normal','nor_gl'),('rough','rough'),('metal','metal')]:
  key=next((k for k in data if term in k.lower() and 'rust' not in k.lower()),None)
  if key and '1k' in data[key]:
   formats=data[key]['1k'];ext='jpg' if 'jpg' in formats else 'png' if 'png' in formats else 'exr';files.append((channel+'.'+ext,formats[ext]))
 for filename,meta in files:
  p=out/filename
  if not p.exists() or hashlib.md5(p.read_bytes()).hexdigest()!=meta['md5']:
   b=get(meta['url']);assert hashlib.md5(b).hexdigest()==meta['md5'];p.write_bytes(b)
 records.append(dict(asset=asset,source='https://polyhaven.com/a/'+asset,license='CC0',authors=catalog[asset]['authors'],files=[dict(name=n,sha256=hashlib.sha256((out/n).read_bytes()).hexdigest()) for n,_ in files]));print('READY',asset,flush=True)
for asset,page in [('Safe','metal-safe-with-combination-lock'),('M870','shotgun-1')]:
 html=get('https://opengameart.org/content/'+page).decode();(src/(asset+'-source.html')).write_text(html,encoding='utf-8')
 urls=re.findall(r'href="(https://opengameart.org/sites/default/files/[^"?]+\.(?:blend|zip))"',html);assert urls,(asset,'archive link missing')
 url=urls[-1];p=src/(asset+pathlib.Path(url).suffix);p.write_bytes(get(url))
 out=src/asset;out.mkdir(exist_ok=True)
 if p.suffix=='.zip':
  with zipfile.ZipFile(p) as z:z.extractall(out)
 else:shutil.copy2(p,out/p.name)
 records.append(dict(asset=asset,source='https://opengameart.org/content/'+page,download=url,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),license='CC BY-SA 3.0' if asset=='Safe' else 'CC0',author='johndh' if asset=='Safe' else 'MikeMoon'));print('READY',asset,flush=True)
records.append(dict(asset='M9 Knife',author='khairul169',license='CC0',source='https://opengameart.org/content/m9-knife-pbr',sha256=hashlib.sha256((src/'m9_knife.zip').read_bytes()).hexdigest()))
(dst/'SOURCES.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
out=dst/'Knife';out.mkdir(exist_ok=True)
for p in (src/'Knife/Textures').glob('*.png'):shutil.copy2(p,out/p.name)
shutil.copy2(src/'Knife/m9_knife.obj',out/'M9.obj');shutil.copy2(src/'Knife/m9_knife.mtl',out/'m9_knife.mtl')
print('ALL_V11_DOWNLOADS_DONE',flush=True)
