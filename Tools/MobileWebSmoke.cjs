// Desktop Chromium touch emulation only; this does not certify Android/iPhone hardware.
const fs=require('fs'),path=require('path');let pw;
try{pw=require('playwright')}catch{pw=require(path.join(require('os').homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'))}
const logs=path.resolve(__dirname,'../Logs'),url=process.argv[2]||'http://127.0.0.1:8080/';let browser;
(async()=>{
 browser=await pw.chromium.launch({channel:'msedge',headless:true,args:['--enable-unsafe-swiftshader','--use-angle=swiftshader']});
 const ctx=await browser.newContext({...pw.devices['Pixel 7'],viewport:{width:915,height:412},deviceScaleFactor:1});
 const page=await ctx.newPage();const errors=[],messages=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{messages.push(m.text());if(m.text().includes('TWINKLE_OPS'))console.log(m.text())});
 await page.goto(url);await page.waitForFunction(()=>window.twinkleReady===true,{},{timeout:240000});
 for(let i=0;i<90&&!messages.some(m=>m.includes('TWINKLE_OPS_BOOT_OK'));i++)await page.waitForTimeout(1000);
 await page.waitForTimeout(2500);const c=await page.locator('canvas').boundingBox();const s=Math.min(c.width/1280,c.height/720);
 console.log(JSON.stringify({bounds:c,canvas:await page.locator('canvas').evaluate(c=>({width:c.width,height:c.height}))}));
 await page.screenshot({path:path.join(logs,'web-mobile-lobby.png')});
 await page.touchscreen.tap(c.x+(c.width-1280*s)/2+310*s,c.y+(c.height-720*s)/2+628*s);
 for(let i=0;i<60&&!messages.some(m=>m.includes('TWINKLE_OPS_RAID_READY'));i++)await page.waitForTimeout(1000);
 if(!messages.some(m=>m.includes('TWINKLE_OPS_RAID_READY'))) { await page.screenshot({path:path.join(logs,'web-mobile-failure.png')});fs.writeFileSync(path.join(logs,'web-mobile-emulated.json'),JSON.stringify({url,raid:false,errors,messages},null,2));throw Error('Touch Deploy did not enter raid'); }
 await page.waitForTimeout(1500);
 await page.touchscreen.tap(c.x+c.width*.74,c.y+c.height*.08); // touch switch weapon
 await page.waitForTimeout(500);
 await page.touchscreen.tap(c.x+c.width*.92,c.y+c.height*.61); // fire
 await page.waitForTimeout(500);
 await page.screenshot({path:path.join(logs,'web-mobile-emulated.png')});
 fs.writeFileSync(path.join(logs,'web-mobile-emulated.json'),JSON.stringify({url,scope:'Desktop Edge Pixel 7 landscape touch emulation; no physical mobile device tested',boot:true,raid:true,errors,messages},null,2));
 if(errors.length)throw Error('Mobile emulation has browser errors');
 console.log('MOBILE_EMULATION_RAID_OK');
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{if(browser)await browser.close()});
