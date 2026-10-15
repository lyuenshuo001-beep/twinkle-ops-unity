// Actual Web build smoke test. Pass a deployed URL to test that exact release.
const fs=require('fs'),path=require('path');
let pw;try{pw=require('playwright')}catch{pw=require(process.env.TWINKLE_PLAYWRIGHT_PATH||path.join(require('os').homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'))}
const url=process.argv[2]||'http://127.0.0.1:8080/';
const logs=path.resolve(__dirname,'../Logs');fs.mkdirSync(logs,{recursive:true});
const prefix=process.argv[3]||'game-web';
const messages=[],errors=[],requests=[];let browser;
function save(extra){fs.writeFileSync(path.join(logs,prefix+'.json'),JSON.stringify({url,scope:'Desktop Edge, headless software WebGL. Browser UI interactions plus screenshots; full raid mechanics separately verified in Unity PlayMode.',...extra,errors,requests,messages},null,2))}
(async()=>{
 browser=await pw.chromium.launch({channel:'msedge',headless:true,args:['--enable-unsafe-swiftshader','--use-angle=swiftshader']});
 const context=await browser.newContext({viewport:{width:1280,height:806}});
 const page=await context.newPage();
 page.on('console',m=>{messages.push({type:m.type(),text:m.text()});if(m.text().includes('TWINKLE_OPS'))console.log(m.text())});
 page.on('pageerror',e=>errors.push(e.message));
 page.on('requestfailed',r=>requests.push({url:r.url(),failure:r.failure()}));
 page.on('response',r=>{if(r.status()>=400)requests.push({url:r.url(),status:r.status()})});
 await page.goto(url,{waitUntil:'domcontentloaded',timeout:60000});
 await page.waitForFunction(()=>window.twinkleReady===true,{},{timeout:240000});
 await page.waitForTimeout(2500);
 await page.screenshot({path:path.join(logs,prefix+'-lobby.png')});
 const canvas=page.locator('canvas');const box=await canvas.boundingBox();
 const point=(x,y)=>({x:box.x+(box.width-1280*Math.min(box.width/1280,box.height/720))/2+x*Math.min(box.width/1280,box.height/720),y:box.y+(box.height-720*Math.min(box.width/1280,box.height/720))/2+y*Math.min(box.width/1280,box.height/720)});
 const buy=point(175,463);await page.mouse.click(buy.x,buy.y);await page.waitForTimeout(400);const pack=point(1117,354);await page.mouse.click(pack.x,pack.y);await page.waitForTimeout(400);await page.screenshot({path:path.join(logs,prefix+'-loadout.png')});
 const enter=point(310,628);await page.mouse.click(enter.x,enter.y);
 for(let n=0;n<90&&!messages.some(m=>m.text.includes('TWINKLE_OPS_RAID_READY'));n++)await page.waitForTimeout(1000);
 if(!messages.some(m=>m.text.includes('TWINKLE_OPS_RAID_READY')))throw Error('Clicking Deploy did not load the real raid');
 await page.waitForTimeout(2500);
 await page.screenshot({path:path.join(logs,prefix+'-raid.png')});
 await page.mouse.click(enter.x,enter.y,{button:'right'});await page.waitForTimeout(700);await page.screenshot({path:path.join(logs,prefix+'-ads-on.png')});if(!messages.some(m=>m.text.includes('TWINKLE_OPS_ADS_TOGGLE:on')))throw Error('First right click did not enter toggle ADS');
 await page.mouse.click(enter.x,enter.y,{button:'right'});await page.waitForTimeout(400);if(!messages.some(m=>m.text.includes('TWINKLE_OPS_ADS_TOGGLE:off')))throw Error('Second right click did not exit ADS');
 // Keep the pointer stationary after deployment; moving it while locked would rotate the view.
 await page.keyboard.press('2');await page.waitForTimeout(350);
 await page.mouse.down();await page.waitForTimeout(650);await page.mouse.up();
 await page.screenshot({path:path.join(logs,prefix+'-ak-fire.png')});
 await page.keyboard.press('r');await page.waitForTimeout(3500);
 await page.keyboard.press('3');await page.waitForTimeout(500);await page.mouse.down();await page.waitForTimeout(220);await page.mouse.up();await page.screenshot({path:path.join(logs,prefix+'-knife.png')});
 await page.keyboard.press('4');await page.waitForTimeout(500);await page.mouse.down();await page.waitForTimeout(100);await page.mouse.up();await page.screenshot({path:path.join(logs,prefix+'-shotgun.png')});
 await page.keyboard.down('w');await page.waitForTimeout(1000);await page.keyboard.up('w');
 await page.keyboard.press('Tab');await page.waitForTimeout(700);
 await page.screenshot({path:path.join(logs,prefix+'-inventory.png')});
 // Inventory uses full viewport-width scaling, independent of the letterboxed HUD.
 const inv=(x,y)=>({x:box.x+x*box.width/1280,y:box.y+y*box.width/1280});const slot=inv(510,136),protect=inv(745,720-41);await page.mouse.click(slot.x,slot.y);await page.mouse.click(protect.x,protect.y);await page.waitForTimeout(500);await page.screenshot({path:path.join(logs,prefix+'-safe.png')});
 const geometry=await canvas.evaluate(c=>({width:c.width,height:c.height,cssWidth:c.clientWidth,cssHeight:c.clientHeight}));
 const runtimeErrors=messages.filter(m=>/Exception:|RuntimeError:|Aborting\(|Error while loading|NullReferenceException/.test(m.text));
 save({boot:true,raid:true,uiInputsSent:['buy medicine','pack','deploy','toggle ADS on/off','protect item','weapon2','fire','reload','knife','melee','weapon4','shotgun','move','inventory'],geometry,runtimeErrors});
 if(errors.length||requests.length||runtimeErrors.length)throw Error('Browser errors detected; inspect '+prefix+'.json');
 console.log(JSON.stringify({url,boot:true,raid:true,errors:0,failedRequests:0}));
})().catch(e=>{save({failure:e.message});console.error(e);process.exitCode=1}).finally(async()=>{if(browser)await browser.close()});
