import urllib.request,json,pathlib,hashlib,time
root=pathlib.Path(r'D:\GameProjects\TwinkleOps');src=root/'SourceAssets/V12';dst=root/'Assets/ThirdParty/V12';src.mkdir(parents=True,exist_ok=True);dst.mkdir(parents=True,exist_ok=True)
def get(url):
 for i in range(3):
  try:return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'TwinkleOps-independent-game-development'}),timeout=90).read()
  except Exception:
   if i==2:raise
   time.sleep(2)
records=[]
for asset in ['modular_factory_facade','plastered_wall_02','concrete_floor']:
 data=json.loads(get('https://api.polyhaven.com/files/'+asset));info=json.loads(get('https://api.polyhaven.com/info/'+asset));(src/(asset+'-files.json')).write_text(json.dumps(data));(src/(asset+'-info.json')).write_text(json.dumps(info));out=dst/asset;out.mkdir(exist_ok=True);files=[]
 if 'fbx' in data:files.append((src/'factory.fbx',data['fbx']['1k']['fbx']))
 for key in data:
  aliases={'Diffuse':'diff','Rough':'rough','Metal':'metal'}
  channel=aliases.get(key,key)
  if any(channel.endswith(term) for term in ['diff','nor_gl','rough','metal']) and '1k' in data[key]:
   ext='jpg' if 'jpg' in data[key]['1k'] else 'png';files.append((out/(channel+'.'+ext),data[key]['1k'][ext]))
 for p,m in files:
  if not p.exists() or hashlib.md5(p.read_bytes()).hexdigest()!=m['md5']:
   b=get(m['url']);assert hashlib.md5(b).hexdigest()==m['md5'];p.write_bytes(b)
 records.append(dict(asset=asset,license='CC0',source='https://polyhaven.com/a/'+asset,authors=info.get('authors'),files=[dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p,m in files]));print('READY',asset,flush=True)
(dst/'SOURCES.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
