// Recorre el menú de Gerente únicamente en la base temporal del runner --visual.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const password=process.env.CLOUDCOLOR_QA_PASSWORD;
assert.ok(password,'Defina CLOUDCOLOR_QA_PASSWORD para QA.');
const groups={
  'CATÁLOGOS':['/Clientes','/Proveedores','/Productos','/Variantes'],
  'VENTAS':['/Cotizaciones','/Facturas','/Cobros','/Devoluciones'],
  'REPORTES':['/Reportes/Ventas','/Reportes/Productos?vista=mas-vendidos','/Reportes/Productos?vista=mayores-ingresos','/Reportes/Productos?vista=menos-vendidos','/Reportes/Inventario','/Reportes/Factura'],
  'COMPRAS E INVENTARIO':['/ProveedorProducto','/Compras','/Recepciones','/Inventario','/Bodegas','/Ubicaciones'],
  'ADMINISTRACIÓN':['/Empleados','/Usuarios','/Roles']
};
(async()=>{
  const browser=await chromium.launch({channel:'msedge',headless:true});
  try {
    const page=await browser.newPage({viewport:{width:1440,height:900}}), errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
    await page.goto('http://localhost:5256/Login');
    await page.getByLabel('Usuario',{exact:true}).fill('web.gerente');
    await page.getByLabel('Contraseña',{exact:true}).fill(password);
    await page.getByRole('button',{name:'Iniciar sesión'}).click();
    await page.waitForURL('**/Dashboard');
    let checked=0;
    for(const [group,routes] of Object.entries(groups)) for(const route of routes) {
      const section=page.locator('.nav-group').filter({has:page.getByText(group,{exact:true})});
      if(await section.getAttribute('open')===null) await section.locator('summary').click();
      const target=section.locator(`a[href="${route}"]`);
      const destination=route==='/Reportes/Factura'?'/Facturas?consulta=1':route;
      const response=page.waitForResponse(r=>r.request().isNavigationRequest()&&r.url()===new URL(destination,'http://localhost:5256').href);
      await target.click();
      assert.equal((await response).status(),200,route);
      assert.equal(await page.locator('main h1').count(),1,route);
      assert.equal(await page.getByRole('heading',{name:'No se pudo continuar'}).count(),0,route);
      assert.equal(await page.locator('.nav-link.active').getAttribute('href'),route,'menú activo '+route);
      for(const [width,height] of [[1920,1080],[1440,900],[1366,768],[768,1024],[390,844]]){
        await page.setViewportSize({width,height});
        assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,`overflow ${route} ${width}`);
      }
      await page.setViewportSize({width:1440,height:900});
      if(route==='/Reportes/Factura') {
        await page.locator('#buscar').fill('WEB-F-COT');
        await page.getByRole('button',{name:'Buscar',exact:true}).click();
        assert.match(page.url(),/consulta=1/);
        assert.equal(await page.locator('.nav-link.active').getAttribute('href'),route,'consulta conserva sección al buscar');
      }
      checked++;
    }
    // La venta consume dos lotes; devolver uno no debe invalidar el campo sin seleccionar del otro.
    await page.goto('http://localhost:5256/Facturas/Nueva');
    await page.locator('#PersonaId').selectOption({index:1});
    await page.locator('#NuevaVariante').selectOption({index:1});
    await page.locator('#NuevaCantidad').fill('70');
    await page.getByRole('button',{name:'+ Agregar',exact:true}).click();
    await page.getByRole('button',{name:'Emitir factura',exact:true}).click();
    await page.locator('#confirmar[open]').getByRole('button',{name:'Confirmar',exact:true}).click();
    await page.waitForURL('**/Facturas/Ver/*');
    const facturaId=new URL(page.url()).pathname.split('/').pop();
    const facturaNumero=(await page.locator('main h1').innerText()).trim();
    await page.goto('http://localhost:5256/Devoluciones/Nueva');
    const factura=page.locator('select[name="factura"]');
    assert.ok(facturaId);
    await factura.selectOption(facturaId);
    await page.getByRole('button',{name:'Cargar detalle'}).click();
    assert.ok(await page.locator('input[name$=".CantidadDevuelta"]').count()>=2,'venta asignada a dos lotes');
    await page.locator('textarea[name="Motivo"]').fill('Devolución parcial de prueba en navegador');
    await page.locator('input[name="Lineas[0].Incluir"]').check();
    await page.locator('input[name="Lineas[0].CantidadDevuelta"]').fill('1');
    await page.getByRole('button',{name:'Registrar devolución',exact:true}).click();
    await page.waitForURL('**/Devoluciones');
    const devolucion=page.locator('tbody tr').filter({hasText:'Devolución parcial de prueba en navegador'});
    assert.equal(await devolucion.count(),1);
    await devolucion.getByRole('link',{name:'Ver detalle →'}).click();
    await page.waitForURL('**/Devoluciones/Ver/*');
    assert.ok((await page.locator('main').innerText()).includes(facturaNumero));
    if(process.env.CLOUDCOLOR_QA_EVIDENCE_DIR){fs.mkdirSync(process.env.CLOUDCOLOR_QA_EVIDENCE_DIR,{recursive:true});await page.screenshot({path:path.join(process.env.CLOUDCOLOR_QA_EVIDENCE_DIR,'devolucion-registrada.png'),fullPage:true});}
    await page.goto('http://localhost:5256/Facturas/Ver/'+facturaId);
    await page.getByRole('button',{name:'Anular factura',exact:true}).click();
    await page.locator('#anular-factura textarea[name="motivo"]').fill('Anulación posterior a devolución en navegador');
    const annul=page.waitForResponse(r=>r.request().method()==='POST'&&r.url().includes('/Facturas/Anular/'));
    await page.locator('#anular-factura').getByRole('button',{name:'Confirmar anulación'}).click();
    assert.equal((await annul).status(),302);
    await page.waitForURL('**/Facturas/Ver/'+facturaId);
    await page.getByLabel('Factura anulada').waitFor();
    assert.match(await page.locator('main').innerText(),/Total original conservado/);
    assert.match(await page.locator('main').innerText(),/Q 0\.00/);
    assert.equal(await page.getByRole('button',{name:'Anular factura',exact:true}).count(),0);
    if(process.env.CLOUDCOLOR_QA_EVIDENCE_DIR)await page.screenshot({path:path.join(process.env.CLOUDCOLOR_QA_EVIDENCE_DIR,'factura-anulada.png'),fullPage:true});
    await page.goto('http://localhost:5256/Roles');
    await page.locator('tbody tr').filter({hasText:'Digitador'}).getByRole('link',{name:'Editar',exact:true}).click();
    await page.locator('#Descripcion').fill('Descripción modificada en QA de navegador');
    await page.getByRole('button',{name:'Guardar cambios'}).click();
    await page.waitForURL(u=>u.pathname==='/Roles');
    await page.locator('tbody tr').filter({hasText:'Digitador'}).getByRole('link',{name:'Editar',exact:true}).click();
    assert.equal(await page.locator('#Descripcion').inputValue(),'Descripción modificada en QA de navegador');
    assert.deepEqual(errors,[]);
    console.log(`OK MENÚ: ${checked} enlaces accionados y comprobados a cinco tamaños; devolución parcial de dos lotes, anulación y edición de rol en QA, sin errores de consola.`);
  } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exitCode=1;});
