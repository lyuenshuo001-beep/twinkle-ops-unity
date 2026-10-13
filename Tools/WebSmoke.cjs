// Local validation only. No deployment and no external asset requests are required.
const fs=require('fs');
const path=require('path');
const http=require('http');
let playwright;
try { playwright=require('playwright'); }
catch { playwright=require(process.env.TWINKLE_PLAYWRIGHT_PATH || path.join(require('os').homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright')); }
const {chromium}=playwright;
const project=path.resolve(__dirname,'..');
const root=path.join(project,'Builds','Validation','Web');
const logs=path.join(project,'Logs');
const mime={'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json','.png':'image/png','.css':'text/css','.txt':'text/plain'};
const messages=[],errors=[],failedRequests=[];
const server=http.createServer((req,res)=>{
  let decoded;
  try { decoded=decodeURIComponent(new URL(req.url,'http://localhost').pathname); } catch { res.writeHead(400).end(); return; }
  const file=path.resolve(root,'.'+(decoded==='/'?'/index.html':decoded));
  if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}
  if(!fs.existsSync(file)||!fs.statSync(file).isFile()){res.writeHead(404).end();return;}
  res.setHeader('Content-Type',mime[path.extname(file)]||'application/octet-stream');
  fs.createReadStream(file).pipe(res);
});
(async()=>{
  if(!fs.existsSync(path.join(root,'index.html'))) throw Error('Web build not found');
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const url=`http://127.0.0.1:${server.address().port}/`;
  const browser=await chromium.launch({channel:'msedge',headless:true,args:['--enable-unsafe-swiftshader','--use-angle=swiftshader']});
  try {
    const context=await browser.newContext({viewport:{width:1365,height:900}});
    const page=await context.newPage();
    let readyResolve;
    const ready=new Promise(resolve=>readyResolve=resolve);
    page.on('console',msg=>{messages.push({type:msg.type(),text:msg.text()});if(msg.text().includes('TWINKLE_OPS_BOOT_OK')) readyResolve();});
    page.on('pageerror',e=>errors.push(e.message));
    page.on('requestfailed',r=>failedRequests.push({url:r.url(),failure:r.failure()}));
    await page.goto(url,{waitUntil:'domcontentloaded'});
    let timer;
    try { await Promise.race([ready,new Promise((_,reject)=>timer=setTimeout(()=>reject(Error('Unity did not boot within 120 seconds')),120000))]); }
    finally { clearTimeout(timer); }
    await page.waitForTimeout(2000);
    await page.screenshot({path:path.join(logs,'web-lobby-desktop.png'),fullPage:true});
    const canvas=await page.locator('canvas').evaluate(c=>({width:c.width,height:c.height,cssWidth:c.clientWidth,cssHeight:c.clientHeight}));
    const report={url,scope:'Engineering lobby only. Not a playable raid. Desktop Edge headless with software WebGL.',boot:true,canvas,errors,failedRequests,messages};
    fs.writeFileSync(path.join(logs,'web-smoke.json'),JSON.stringify(report,null,2));
    if(errors.length||failedRequests.length) throw Error('Browser reported errors; see Logs/web-smoke.json');
    console.log(JSON.stringify({boot:true,canvas,errors:errors.length,failedRequests:failedRequests.length,screenshot:'Logs/web-lobby-desktop.png'}));
  } finally { await browser.close(); server.close(); }
})().catch(e=>{fs.mkdirSync(logs,{recursive:true});fs.writeFileSync(path.join(logs,'web-smoke-failure.json'),JSON.stringify({error:e.message,errors,failedRequests,messages},null,2));console.error(e.message);server.close();process.exitCode=1;});
