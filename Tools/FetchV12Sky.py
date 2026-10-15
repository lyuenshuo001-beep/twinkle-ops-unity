import urllib.request,json,hashlib
from pathlib import Path
asset='kloofendal_48d_partly_cloudy_puresky'
root=Path(r'D:\GameProjects\TwinkleOps\SourceAssets\V12\Sky');root.mkdir(parents=True,exist_ok=True)
def get(url):
 return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'TwinkleOps-independent-game-development'}),timeout=90).read()
files=json.loads(get('https://api.polyhaven.com/files/'+asset));info=json.loads(get('https://api.polyhaven.com/info/'+asset));f=files['hdri']['2k']['hdr'];data=get(f['url']);assert hashlib.md5(data).hexdigest()==f['md5'];(root/(asset+'.hdr')).write_bytes(data)
record={'asset':asset,'authors':info['authors'],'source':'https://polyhaven.com/a/'+asset,'license':'CC0','download':f,'sha256':hashlib.sha256(data).hexdigest()};(root/'SOURCE.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps({'bytes':len(data),'authors':info['authors']}))
