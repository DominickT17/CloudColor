// Ejecutar solo contra el servidor temporal que inicia CloudColor.Web.Pruebas --visual.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');const path=require('node:path');
const origin='http://localhost:5256';
const output=path.join(process.env.TEMP||process.env.TMPDIR||'.','CloudColor-Web-Capturas');fs.mkdirSync(output,{recursive:true});
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try {
 const context=await browser.newContext({viewport:{width:1440,height:900}});const p=await context.newPage();const errors=[];
 p.on('dialog',dialog=>dialog.accept());
 p.on('pageerror',e=>errors.push(e.message));
 p.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
 const go=async route=>{const response=await p.goto(origin+route);assert.equal(response.status(),200,route);};
 const shot=async name=>{await p.screenshot({path:path.join(output,name+'.png'),fullPage:true});};
 const postClick=async name=>{console.log('Accion: '+name);await p.getByRole('button',{name,exact:true}).click();if(await p.locator('#confirmar[open]').count())await p.locator('#confirmar').getByRole('button',{name:'Confirmar',exact:true}).click();for(let i=0;i<30&&!new URL(p.url()).pathname.includes('/Ver/');i++)await new Promise(r=>setTimeout(r,100));if(!new URL(p.url()).pathname.includes('/Ver/')){console.log(await p.locator('main').innerText());await shot('error-navegacion');throw new Error('No se guardo: '+name+' '+p.url());}};
 await go('/Login');await shot('01-login');await p.getByLabel('Usuario',{exact:true}).fill('web.gerente');await p.getByLabel('Contraseña',{exact:true}).fill('Prueba-Web-Temporal-2026!');await p.getByRole('button',{name:'Iniciar sesión'}).click();await p.waitForURL('**/Dashboard');
 await go('/Clientes?buscar=Cliente');await p.getByRole('link',{name:'Limpiar búsqueda',exact:true}).click();await p.waitForURL('**/Clientes');assert.equal(await p.locator('#buscar').inputValue(),'');
 await go('/Variantes');assert.ok((await p.locator('tbody').innerText()).includes('Q 29.00'));
 await go('/Clientes/Editar');
 assert.equal(await p.evaluate(()=>{
  const form=document.querySelector('form.editor'),button=form.querySelector('button[type=submit]');
  const label=button.textContent;document.addEventListener('submit',e=>e.preventDefault(),{once:true});
  form.dispatchEvent(new SubmitEvent('submit',{bubbles:true,cancelable:true,submitter:button}));
  const busy=button.disabled&&form.getAttribute('aria-busy')==='true';
  window.dispatchEvent(new PageTransitionEvent('pageshow',{persisted:true}));
  return busy&&!button.disabled&&button.textContent===label&&!form.hasAttribute('aria-busy');
 }),true,'restaurar formulario desde historial');
 await go('/Cotizaciones/Nueva');await p.locator('#PersonaId').selectOption({index:1});await p.locator('#NuevaVariante').selectOption({index:1});await p.locator('#NuevaCantidad').fill('0');await p.getByRole('button',{name:'+ Agregar',exact:true}).click();await p.getByRole('alert').filter({hasText:'La cantidad debe ser positiva'}).waitFor();
 await p.locator('#NuevaCantidad').fill('2');await p.getByRole('button',{name:'+ Agregar',exact:true}).click();await p.locator('input[name="Detalle[0].ProductoVarianteID"]').waitFor({state:'attached'});await shot('02-cotizacion-editor');await postClick('Guardar documento');const cotizacion=p.url();await shot('03-cotizacion-detalle');
 await p.getByRole('button',{name:'Confirmar cotización',exact:true}).click();await p.locator('dialog[open]').waitFor();await p.locator('dialog').getByRole('button',{name:'Confirmar',exact:true}).click();await p.getByRole('link',{name:'Facturar cotización'}).waitFor();await p.getByRole('link',{name:'Facturar cotización'}).click();await p.getByRole('button',{name:'Emitir factura',exact:true}).waitFor();await shot('04-facturacion-editor');await postClick('Emitir factura');const factura=p.url();await p.getByRole('link',{name:'Registrar cobro'}).click();const cobro=p.url();await shot('05-cobro');
 await p.locator('#NuevoMedio').selectOption({label:'Efectivo'});await p.locator('#NuevoMonto').fill('20');await p.getByRole('button',{name:'+ Agregar medio de pago',exact:true}).click();await p.locator('input[name="Pagos[0].Monto"]').waitFor({state:'attached'});await p.locator('#NuevoMedio').selectOption({label:'Tarjeta'});await p.locator('#NuevoMonto').fill('38');await p.locator('#NuevaReferencia').fill('QA-NAVEGADOR');await p.getByRole('button',{name:'+ Agregar medio de pago',exact:true}).click();await p.locator('input[name="Pagos[1].Monto"]').waitFor({state:'attached'});await shot('06-cobro-distribuido');await postClick('Confirmar cobro');assert.ok((await p.locator('main').innerText()).includes('PAGADA'));
 await go('/Compras/Nueva');await p.locator('#PersonaId').selectOption({index:1});await p.getByRole('button',{name:'Cargar variantes del proveedor'}).click();if(await p.locator('#confirmar[open]').count())await p.locator('#confirmar').getByRole('button',{name:'Confirmar',exact:true}).click();await p.locator('#NuevaVariante option').nth(1).waitFor({state:'attached'});await p.locator('#NuevaVariante').selectOption({index:1});await p.locator('#NuevaCantidad').fill('100');await p.locator('#NuevoPrecio').fill('9.50');await p.getByRole('button',{name:'+ Agregar',exact:true}).click();await p.locator('input[name="Detalle[0].Cantidad"]').waitFor({state:'attached'});await shot('07-compra-editor');await postClick('Guardar documento');const compra=p.url();await p.getByRole('link',{name:'+ Registrar recepción'}).click();await p.locator('input[name="Lineas[0].Incluir"]').check();await p.locator('#cantidad-0').fill('75');await p.locator('#bodega-recepcion-0').selectOption({index:1});await p.locator('#ubicacion-0').selectOption({index:1});await p.locator('#lote-0').fill('QA-VISUAL-75');await shot('08-recepcion-editor');await postClick('Registrar recepción');await shot('09-compra-parcial');assert.ok((await p.locator('main').innerText()).includes('75 / 100 recibidos'));
 const targets=[['dashboard','/Dashboard'],['clientes','/Clientes'],['variantes','/Variantes'],['bodegas','/Bodegas'],['cotizacion',new URL(cotizacion).pathname],['compra',new URL(compra).pathname],['inventario','/Inventario'],['facturas','/Facturas'],['cobro',new URL(cobro).pathname]];
 for(const [width,height] of [[1920,1080],[1440,900],[1366,768],[768,1024],[390,844]]){
  await p.setViewportSize({width,height});
  for(const [name,route] of targets){await go(route);await p.waitForTimeout(220);assert.equal(await p.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,`overflow ${name} ${width}`);await shot(`${width}-${name}`);}
  if(width<=768){
   await p.getByRole('button',{name:'Abrir navegación',exact:true}).click();await p.waitForTimeout(220);
   assert.equal(await p.locator('.menu-toggle').getAttribute('aria-expanded'),'true');
   assert.equal(await p.locator('.app-shell').evaluate(e=>e.inert),true);
   await p.keyboard.press('Shift+Tab');assert.equal(await p.evaluate(()=>document.activeElement===document.querySelector('.sidebar a:last-of-type.nav-link')),true);
   await p.keyboard.press('Tab');assert.equal(await p.evaluate(()=>document.activeElement===document.querySelector('.sidebar a')),true);
   await p.keyboard.press('Escape');assert.equal(await p.locator('.menu-toggle').getAttribute('aria-expanded'),'false');
   assert.equal(await p.locator('.app-shell').evaluate(e=>e.inert),false);
   assert.equal(await p.evaluate(()=>document.activeElement===document.querySelector('.menu-toggle')),true);
  }
 }
 await p.setViewportSize({width:1440,height:900});await go('/Clientes');await p.getByRole('button',{name:'Desactivar',exact:true}).first().click();await p.locator('dialog[open]').waitFor();await p.locator('dialog').getByRole('button',{name:'Cancelar',exact:true}).click();assert.equal(await p.locator('dialog[open]').count(),0);
 await go(new URL(factura).pathname.replace('/Ver/','/Imprimir/'));await shot('10-factura-impresion');await p.pdf({path:path.join(output,'factura-qa.pdf'),format:'A4',printBackground:true});
 assert.deepEqual(errors,[]);console.log('OK NAVEGADOR: cotizacion, confirmacion, factura, cobro multiple, compra, recepcion parcial, 45 vistas responsive, menu movil, confirmacion y PDF.');console.log('Capturas: '+output);
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
