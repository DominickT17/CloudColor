// Verifica búsqueda y permisos usando únicamente cuentas y datos QA temporales.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict');
const password=process.env.CLOUDCOLOR_QA_PASSWORD;assert.ok(password);
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const errors=[];
  for(const [usuario,esperados] of [
   ['web.gerente',['Clientes','Productos','Variantes','Proveedores','Cotizaciones','Órdenes','Facturas']],
   ['web.digitador',['Clientes','Productos','Variantes','Proveedores','Cotizaciones','Órdenes','Facturas']],
   ['web.cajero',['Facturas']]
  ]){
   const page=await browser.newPage({viewport:{width:1440,height:900}});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await page.goto('http://localhost:5256/Login');await page.locator('#usuario').fill(usuario);await page.locator('#password').fill(password);await page.getByRole('button',{name:'Iniciar sesión'}).click();await page.waitForURL('**/Dashboard');
   await page.locator('#global-query').fill('WEB');
   await page.locator('.search-group h3').first().waitFor();
   assert.deepEqual((await page.locator('.search-group h3').allTextContents()).sort(),esperados.sort(),usuario);
   await page.getByRole('button',{name:'Limpiar búsqueda global'}).click();assert.equal(await page.locator('#global-query').inputValue(),'');
   if(usuario==='web.gerente'){
    await page.goto('http://localhost:5256/Roles');await page.locator('#buscar').fill('Digitador');await page.getByRole('button',{name:'Buscar',exact:true}).click();
    await page.waitForURL(u=>u.pathname==='/Roles'&&u.searchParams.get('buscar')==='Digitador');
    assert.equal(await page.locator('tbody tr').count(),1);assert.match(await page.locator('tbody').innerText(),/Digitador/);
    await page.getByRole('link',{name:'Limpiar búsqueda',exact:true}).click();await page.waitForURL(u=>u.pathname==='/Roles'&&!u.searchParams.has('buscar'));
    for(const activo of [false,true]){
     await page.locator('tbody tr').filter({hasText:'Digitador'}).getByRole('link',{name:'Editar',exact:true}).click();
     await page.locator('#Activo').setChecked(activo);await page.getByRole('button',{name:'Guardar cambios'}).click();
     await page.waitForURL(u=>u.pathname==='/Roles');
     assert.match(await page.locator('tbody tr').filter({hasText:'Digitador'}).innerText(),activo?/\bACTIVO\b/:/\bINACTIVO\b/);
    }
    await page.locator('tbody tr').filter({hasText:'Gerente'}).getByRole('link',{name:'Editar',exact:true}).click();
    await page.locator('#Activo').uncheck();await page.getByRole('button',{name:'Guardar cambios'}).click();
    assert.match(await page.locator('main').innerText(),/rol Gerente debe permanecer activo/i);
    await page.goto('http://localhost:5256/Reportes/Ventas');
    await page.locator('input[name="desde"]').fill('2000-01-01');await page.locator('input[name="hasta"]').fill('2000-01-31');
    await page.getByRole('button',{name:'Aplicar fechas'}).click();
    await page.waitForURL(u=>u.searchParams.get('desde')==='2000-01-01'&&u.searchParams.get('hasta')==='2000-01-31');
    assert.match(await page.locator('.report-note').innerText(),/0 facturas/);
    await page.goto('http://localhost:5256/Reportes/Productos?vista=mas-vendidos');
    for(const [label,value] of [['Top 5','5'],['Top 10','10'],['Todos','0']]){
     await page.locator('[data-report-limit]').selectOption({label});
     await page.waitForURL(u=>u.searchParams.get('limite')===value);
    }
    await page.goto('http://localhost:5256/Inventario');
    await page.locator('#buscar').fill('WEB-QA-1');await page.locator('#variante').selectOption({index:1});
    await page.locator('#bodega').selectOption({index:1});await page.locator('#ubicacion').selectOption({index:1});await page.locator('#estado').selectOption({label:'Disponible'});
    await page.getByRole('button',{name:'Filtrar',exact:true}).click();
    await page.waitForURL(u=>u.pathname==='/Inventario'&&u.searchParams.get('buscar')==='WEB-QA-1'&&u.searchParams.get('estado')==='Disponible');
    assert.match(await page.locator('main').innerText(),/WEB-QA-1/);
   }
   await page.close();console.log('OK BÚSQUEDA: '+usuario);
  }
  assert.deepEqual(errors,[]);console.log('OK BÚSQUEDA: siete grupos por perfil, roles y restricciones, fechas, límites de reportes y filtros de inventario en Edge.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
