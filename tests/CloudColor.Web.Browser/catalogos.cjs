// Escrituras exclusivamente en la base desechable del runner --visual.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict');
const password=process.env.CLOUDCOLOR_QA_PASSWORD;assert.ok(password);
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  await page.goto('http://localhost:5256/Login');await page.locator('#usuario').fill('web.gerente');await page.locator('#password').fill(password);await page.getByRole('button',{name:'Iniciar sesión'}).click();await page.waitForURL('**/Dashboard');
  const set=async values=>{for(const [key,value] of Object.entries(values)){const field=page.locator('#'+key);if(await field.evaluate(e=>e.tagName)==='SELECT')await field.selectOption(value.startsWith('label:')?{label:value.slice(6)}:value==='first'?{index:1}:value);else await field.fill(value);}};
  const go=route=>page.goto('http://localhost:5256/'+route);
  const save=async route=>{await page.getByRole('button',{name:'Guardar cambios'}).click();await page.waitForURL(u=>u.pathname==='/'+route);};
  const run=async(route,marker,values,edit)=>{
   await go(route);await page.locator('.page-heading').getByRole('link',{name:/Crear/}).click();await set(values);await save(route);
   let row=page.locator('tbody tr').filter({hasText:marker});await row.waitFor();assert.equal(await row.count(),1,route);
   await page.locator('#buscar').fill(marker);await page.getByRole('button',{name:'Buscar',exact:true}).click();
   await page.waitForURL(u=>u.pathname==='/'+route&&u.searchParams.get('buscar')===marker);
   assert.equal(await page.locator('tbody tr').filter({hasText:marker}).count(),1,'buscar '+route);
   await page.getByRole('link',{name:'Limpiar búsqueda',exact:true}).click();
   await page.waitForURL(u=>u.pathname==='/'+route&&!u.searchParams.has('buscar'));
   row=page.locator('tbody tr').filter({hasText:marker});
   await row.getByRole('link',{name:'Editar',exact:true}).click();if(edit)await set(edit);await save(route);
   row=page.locator('tbody tr').filter({hasText:marker});
   for(const action of ['Desactivar','Activar']){await row.getByRole('button',{name:action,exact:true}).click();await page.locator('#confirmar[open]').getByRole('button',{name:'Confirmar',exact:true}).click();await page.waitForLoadState('networkidle');row=page.locator('tbody tr').filter({hasText:marker});await row.getByRole('button',{name:action==='Activar'?'Desactivar':'Activar',exact:true}).waitFor();}
   console.log('OK CATÁLOGO: '+route+' crear, buscar, limpiar, editar, desactivar y activar');
  };
  await run('Clientes','Cliente navegador',{NombreRazonSocial:'Cliente navegador',TipoCliente:'Particular'},{Telefono:'55550001'});
  await run('Proveedores','Proveedor navegador',{Nombre:'Proveedor navegador'},{Telefono:'55550002'});
  await run('Productos','Producto navegador',{Nombre:'Producto navegador',CategoriaProductoID:'first',MarcaID:'first'},{Descripcion:'Edición de QA'});
  await run('Bodegas','BOD-NAV',{Codigo:'BOD-NAV',Nombre:'Bodega navegador'},{Direccion:'Dirección QA'});
  await run('Ubicaciones','UBI-NAV',{BodegaID:'label:Bodega navegador',Codigo:'UBI-NAV',Descripcion:'Ubicación QA'},{Descripcion:'Ubicación editada'});
  await run('Variantes','SKU-NAV',{CodigoSKU:'SKU-NAV',ProductoID:'label:Producto navegador',PresentacionID:'first',PrecioVentaActual:'15',StockMinimo:'1'},{PrecioVentaActual:'16'});
  await run('ProveedorProducto','Proveedor navegador',{ProveedorID:'label:Proveedor navegador',ProductoVarianteID:'first'},null);
  await run('Empleados','EMP-NAV',{CodigoEmpleado:'EMP-NAV',Nombres:'Empleado',Apellidos:'Navegador'},{Telefono:'55550003'});
  await run('Usuarios','web.navegador',{NombreUsuario:'web.navegador',EmpleadoID:'label:Empleado Navegador',RolID:'label:Digitador',Password:password},null);
  assert.deepEqual(errors,[]);console.log('OK CATÁLOGOS: nueve recorridos completos sin errores de consola.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
