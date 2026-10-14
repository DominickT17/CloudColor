// Requiere la base temporal preparada por CloudColor.Web.Pruebas --visual.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict');const fs=require('node:fs');const path=require('node:path');
const output=path.join(process.env.TEMP||'.','CloudColor-Web-UX','despues');fs.mkdirSync(output,{recursive:true});
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('dialog',d=>d.accept()); // Solo la protección nativa de recarga/navegación directa de QA.
  const go=async route=>assert.equal((await page.goto('http://localhost:5256'+route)).status(),200,route);
  const capture=async name=>page.screenshot({path:path.join(output,name+'.png'),fullPage:true});
  const confirm=async()=>page.locator('#confirmar').getByRole('button',{name:'Confirmar',exact:true}).click();
  await go('/Login');await page.locator('#usuario').fill('web.gerente');await page.locator('#password').fill('Prueba-Web-Temporal-2026!');await page.getByRole('button',{name:'Iniciar sesión'}).click();await page.waitForURL('**/Dashboard');
  assert.ok(await page.locator('.chart-card').count()>=1);await capture('1440-dashboard');
  await page.keyboard.press('Alt+k');await page.locator('#global-query').fill('WEB');await page.locator('.search-group').first().waitFor();assert.ok((await page.locator('#global-results').innerText()).includes('Productos'));await page.keyboard.press('Escape');assert.equal(await page.locator('#busqueda-global[open]').count(),0);
  await go('/Clientes');await page.getByRole('button',{name:'Vista rápida',exact:true}).first().click();await page.locator('#detalle-rapido[open]').waitFor();assert.match(await page.locator('.quick-content').innerText(),/Cliente/);await page.keyboard.press('Escape');
  await page.getByRole('link',{name:'+ Crear cliente',exact:true}).click();await page.locator('#NombreRazonSocial').fill('No persistir');await page.getByRole('link',{name:'Cancelar',exact:true}).click();await page.locator('#confirmar[open]').waitFor();await page.locator('#confirmar').getByRole('button',{name:'Cancelar',exact:true}).click();assert.equal(await page.locator('#NombreRazonSocial').inputValue(),'No persistir');
  await page.getByRole('link',{name:'Cancelar',exact:true}).click();await page.locator('#confirmar').getByRole('button',{name:'Salir sin guardar'}).click();await page.waitForURL(u=>u.pathname==='/Clientes');
  await go('/Inventario?estado=Disponible');assert.ok(await page.locator('.filter-chip').count()>0);await capture('1440-inventario');await page.getByRole('link',{name:'Quitar filtro estado: Disponible'}).click();await page.waitForURL('**/Inventario');
  await go('/Cotizaciones/Nueva');const url=page.url();await page.locator('#PersonaId').selectOption({index:1});await page.locator('#buscar-variante').fill('WEB-QA-1');await page.locator('#NuevaVariante').selectOption({index:1});await page.locator('#NuevaCantidad').fill('2');await page.getByRole('button',{name:'+ Agregar',exact:true}).click();assert.equal(page.url(),url);assert.equal(await page.locator('.cart-lines tbody tr').count(),1);assert.match(await page.locator('[data-cart-total]').first().innerText(),/58\.00/);
  await page.locator('.cart-quantity').fill('3');await page.locator('.cart-price').fill('10.50');assert.match(await page.locator('[data-cart-total]').first().innerText(),/31\.50/);await capture('1440-cotizacion');
  await page.getByRole('button',{name:'Guardar documento',exact:true}).click();await page.waitForURL('**/Cotizaciones/Ver/*');await page.locator('.toast.success').waitFor();await page.locator('.toast.success button').click();assert.equal(await page.locator('.toast.success').count(),0);
  await page.getByRole('button',{name:'Confirmar cotización',exact:true}).click();await confirm();await page.getByRole('link',{name:'Facturar cotización'}).click();assert.equal(await page.locator('[data-cart=readonly]').count(),1);assert.equal(await page.locator('.cart-price[type=number]').count(),0);
  await page.getByRole('button',{name:'Emitir factura',exact:true}).click();await page.locator('#confirmar[open]').waitFor();await page.locator('#confirmar').getByRole('button',{name:'Cancelar',exact:true}).click();assert.ok(page.url().includes('/Facturas/Nueva'));await page.getByRole('button',{name:'Emitir factura',exact:true}).click();await confirm();await page.waitForURL('**/Facturas/Ver/*');const factura=new URL(page.url()).pathname;
  await page.getByRole('link',{name:'Registrar cobro'}).click();const cobro=new URL(page.url()).pathname;
  await page.locator('#NuevoMedio').selectOption({label:'Efectivo'});await page.locator('#NuevoMonto').fill('40');assert.match(await page.locator('[data-payment-preview]').innerText(),/excede/);await page.getByRole('button',{name:'+ Agregar medio de pago'}).click();assert.match(await page.locator('[data-payment-state]').innerText(),/supera/);assert.equal(await page.getByRole('button',{name:'Confirmar cobro',exact:true}).isDisabled(),true);
  await page.locator('.payment-amount').fill('31.50');assert.match(await page.locator('[data-payment-state]').innerText(),/Listo para cobrar/);await capture('1440-cobro');await page.getByRole('button',{name:'Confirmar cobro',exact:true}).click();await confirm();await page.waitForURL('**/Facturas/Ver/*');
  await go('/Compras');await page.locator('a[href^="/Compras/Ver/"]').first().click();const compra=new URL(page.url()).pathname;await capture('1440-compra');
  const receipt=page.locator('a[href^="/Recepciones/Detalle/"]').first();await receipt.click();const receiptDetail=new URL(page.url()).pathname+new URL(page.url()).search;assert.match(await page.locator('main').innerText(),/Mercadería recibida/);
  const targets=[['Dashboard','/Dashboard'],['Clientes','/Clientes'],['Productos','/Productos'],['Variantes','/Variantes'],['Inventario','/Inventario'],['Cotizacion','/Cotizaciones/Nueva'],['Compra',compra],['Recepcion',receiptDetail],['Factura',factura],['Cobro',cobro]];
  const anon=await browser.newPage();
  for(const [width,height] of [[1920,1080],[1440,900],[1366,768],[768,1024],[390,844]]){
   await page.setViewportSize({width,height});
   await anon.setViewportSize({width,height});await anon.goto('http://localhost:5256/Login');await anon.screenshot({path:path.join(output,width+'-Login.png'),fullPage:true});
   for(const [name,route] of targets){await go(route);assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,name+' '+width);await capture(width+'-'+name);}
   assert.match(await page.locator('[data-payment-state]').innerText(),/Factura pagada/);assert.equal(await page.locator('#NuevoMonto').isDisabled(),true);
   if(width===390){assert.equal(await page.locator('.mobile-cards').first().evaluate(e=>getComputedStyle(e).display),'block');}
  }
  await anon.close();
  assert.deepEqual(errors,[]);console.log('OK UX: busqueda, vista rapida, cambios sin guardar, chips, carrito sin recargas, confirmaciones, cobro editable, recepcion y 55 vistas responsive.');console.log(output);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
