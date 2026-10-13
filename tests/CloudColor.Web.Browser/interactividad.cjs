// Solo contra CloudColor.Web.Pruebas --visual, nunca contra datos reales.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict');
const path=require('node:path');const fs=require('node:fs');
const output=path.join(process.env.TEMP||'.','CloudColor-Web-Interactividad');fs.mkdirSync(output,{recursive:true});
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  const go=async route=>assert.equal((await page.goto('http://localhost:5256'+route)).status(),200);
  await go('/Login');await page.getByLabel('Contraseña',{exact:true}).fill('Prueba-Web-Temporal-2026!');
  await page.getByRole('button',{name:'Mostrar contraseña',exact:true}).click();assert.equal(await page.locator('#password').getAttribute('type'),'text');
  await page.getByRole('button',{name:'Ocultar contraseña',exact:true}).click();assert.equal(await page.locator('#password').getAttribute('type'),'password');
  await page.getByLabel('Usuario',{exact:true}).fill('web.gerente');await page.getByRole('button',{name:'Iniciar sesión'}).click();await page.waitForURL('**/Dashboard');
  await go('/Usuarios');const rows=page.locator('tbody tr:not([hidden])'),total=await rows.count();assert.ok(total>=3);
  await page.getByLabel('Filtrar resultados visibles').fill('web.temporal');assert.equal(await rows.count(),1);
  await page.getByLabel('Filtrar resultados visibles').fill('sin coincidencias');assert.equal(await rows.count(),0);assert.match(await page.locator('.table-count').innerText(),/Sin coincidencias/);
  await page.getByRole('button',{name:'Restablecer filtros'}).click();assert.equal(await rows.count(),total);
  await page.getByRole('button',{name:'Ordenar por Usuario',exact:true}).click();
  const names=await rows.locator('td:first-child').allTextContents();assert.deepEqual(names,[...names].sort(new Intl.Collator('es',{numeric:true,sensitivity:'base'}).compare));
  assert.equal(await page.locator('th[aria-sort="ascending"]').count(),1);
  await page.getByRole('button',{name:'Ordenar por Usuario descendente',exact:true}).click();assert.deepEqual(await rows.locator('td:first-child').allTextContents(),[...names].reverse());
  await go('/Cotizaciones/Nueva');await page.locator('#NuevaCantidad').fill('2.5');await page.locator('#NuevoPrecio').fill('12.40');assert.match(await page.locator('.line-preview').innerText(),/31\.00/);
  await page.locator('#NuevaCantidad').fill('');assert.match(await page.locator('.line-preview').innerText(),/válidos/);
  await page.locator('#NuevaCantidad').fill('3');await page.locator('#NuevaVariante').selectOption({index:1});assert.match(await page.locator('.line-preview').innerText(),/87\.00/);
  for(const [width,height] of [[1920,1080],[1440,900],[1366,768],[768,1024],[390,844]]){
   await page.setViewportSize({width,height});
   for(const route of ['/Clientes','/Usuarios','/Inventario','/Cotizaciones/Nueva']){
    await go(route);assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,route+' '+width);
    await page.screenshot({path:path.join(output,width+'-'+route.replaceAll('/','')+'.png'),fullPage:true});
   }
  }
  assert.deepEqual(errors,[]);console.log('OK: contraseña, filtros, restablecer, orden ascendente/descendente, subtotal reactivo y 20 vistas responsive; sin escrituras de negocio.');console.log(output);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
