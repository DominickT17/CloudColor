// Borradores en memoria de la página. El POST final conserva los contratos de Negocio.
(() => {
 const money=n=>new Intl.NumberFormat('es-GT',{style:'currency',currency:'GTQ'}).format(n);
 const clean=s=>s.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase();
 const node=(tag,text,cls)=>{const e=document.createElement(tag);if(text!=null)e.textContent=text;if(cls)e.className=cls;return e;};
 const input=(name,value,type='hidden',label)=>{const e=node('input');e.name=name;e.value=value;e.type=type;if(label)e.setAttribute('aria-label',label);return e;};
 const cart=document.querySelector('[data-cart=editable]');
 if(cart){
  const tbody=cart.querySelector('.cart-lines tbody'),select=cart.querySelector('#NuevaVariante'),search=cart.querySelector('#buscar-variante');
  const options=[...select.options].slice(1).map(o=>o.cloneNode(true));
  const stock=()=>{const label=cart.querySelector('.availability');if(label)label.textContent=select.value==='0'?'Selecciona una variante para consultar disponibilidad.':'Disponible: '+Number(select.selectedOptions[0]?.dataset.disponible||0).toLocaleString('es-GT')+' unidades · se verifica al emitir.';};
  select.addEventListener('change',stock);stock();
  search.addEventListener('input',()=>{const current=select.value,term=clean(search.value);const filtered=options.filter(o=>clean(o.textContent).includes(term));select.replaceChildren(new Option('Selecciona una variante','0'),...filtered.map(o=>o.cloneNode(true)));if(filtered.some(o=>o.value===current))select.value=current;cart.querySelector('[data-variant-count]').textContent=filtered.length+' variantes coinciden';select.dispatchEvent(new Event('change'));});
  function refresh(){
   let cents=0;const rows=[...tbody.rows];rows.forEach((row,index)=>{
    row.querySelectorAll('input[name^="Detalle["]').forEach(field=>field.name=field.name.replace(/Detalle\[\d+\]/,'Detalle['+index+']'));
    const q=Number(row.querySelector('.cart-quantity').value),p=Number(row.querySelector('.cart-price').value),subtotal=Number.isFinite(q*p)&&q>=0&&p>=0?Math.round((q*p+Number.EPSILON)*100):0;cents+=subtotal;
    row.querySelector('.cart-subtotal').textContent=money(subtotal/100);row.querySelector('button[value^="Quitar:"]').value='Quitar:'+index;
   });
   cart.querySelectorAll('[data-cart-count]').forEach(e=>e.textContent=rows.length);cart.querySelectorAll('[data-cart-total]').forEach(e=>e.textContent=money(cents/100));cart.querySelector('.cart-empty').hidden=rows.length>0;cart.querySelector('[value=Guardar]').disabled=rows.length===0;
  }
  tbody.addEventListener('input',()=>{cart.dataset.dirty='true';refresh();});
  cart.addEventListener('submit',event=>{
   const action=event.submitter?.value;if(action!=='Agregar'&&!action?.startsWith('Quitar:'))return;event.preventDefault();
   if(action.startsWith('Quitar:')){tbody.rows[Number(action.split(':')[1])]?.remove();cart.dataset.dirty='true';refresh();return;}
   const option=select.selectedOptions[0],q=Number(cart.querySelector('#NuevaCantidad').value),p=Number(cart.querySelector('#NuevoPrecio').value);
   if(!option||select.value==='0'){ccToast('Selecciona una variante disponible.','error');select.focus();return;}
   if(!Number.isFinite(q*p)||q<=0||p<0){ccToast('La cantidad debe ser positiva y el precio no negativo.','error');return;}
   if([...tbody.querySelectorAll('[name$="ProductoVarianteID"]')].some(i=>i.value===select.value)){ccToast('Esta variante ya está agregada. Edita su cantidad en el detalle.','warning');return;}
   if(tbody.rows.length>=200){ccToast('Máximo 200 líneas por documento.','warning');return;}
   const row=node('tr'),description=node('td');description.dataset.label='Producto / variante';description.append(node('strong',option.dataset.producto),node('small',option.textContent),input('Detalle[0].ProductoVarianteID',select.value));row.append(description);
   for(const [name,value,cls,min,step,label] of [['Cantidad',q,'cart-quantity','0.0001','0.0001','Cantidad'],['PrecioUnitario',p,'cart-price','0','0.01','Precio']]){
    const cell=node('td');cell.dataset.label=label;const field=input('Detalle[0].'+name,value,'number',label+' '+option.dataset.sku);field.className=cls;field.min=min;field.step=step;field.required=true;cell.append(field);row.append(cell);
   }
   const subtotal=node('td','', 'cart-subtotal');subtotal.dataset.label='Subtotal';row.append(subtotal);
   const actionCell=node('td');actionCell.dataset.label='Acción';const remove=node('button','Quitar','text-button');remove.type='submit';remove.name='accion';remove.value='Quitar:0';remove.formNoValidate=true;remove.setAttribute('aria-label','Quitar '+option.dataset.sku);actionCell.append(remove);row.append(actionCell);tbody.append(row);cart.dataset.dirty='true';refresh();ccToast('Variante agregada al borrador.','info');
  });
  cart.addEventListener('keydown',e=>{if(e.key==='Enter'&&e.target.matches('#NuevaCantidad,#NuevoPrecio,#buscar-variante')){e.preventDefault();cart.requestSubmit(cart.querySelector('[value=Agregar]'));}});refresh();
 }
 const payment=document.querySelector('[data-payment]');
 if(payment){
  const body=payment.querySelector('.payment-lines tbody'),remaining=Math.round(Number(payment.dataset.pending)*100),medium=payment.querySelector('#NuevoMedio');
  const paid=payment.dataset.payment==='PAGADA';
  if(paid){payment.querySelectorAll('input:not([type=hidden]),select').forEach(i=>i.disabled=true);payment.querySelector('.payment-empty p').textContent='El pago ya está registrado. Consulta los medios en el detalle de la factura.';}
  function refresh(){let cents=0;[...body.rows].forEach((row,index)=>{row.querySelectorAll('input[name^="Pagos["]').forEach(i=>i.name=i.name.replace(/Pagos\[\d+\]/,'Pagos['+index+']'));const amount=Number(row.querySelector('.payment-amount').value);if(Number.isFinite(amount))cents+=Math.round(amount*100);row.querySelector('button').value='Quitar:'+index;});
   payment.querySelector('[data-distributed]').textContent=money(cents/100);payment.querySelector('[data-remaining]').textContent=money((remaining-cents)/100);
   const state=payment.querySelector('[data-payment-state]');state.textContent=paid?'Factura pagada · no tiene saldo pendiente.':cents>remaining?'El importe supera el pendiente.':cents===remaining&&body.rows.length?'Listo para cobrar':'Pendiente de distribuir '+money((remaining-cents)/100);state.className='alert '+(cents>remaining?'error':cents===remaining?'success':'info');payment.querySelector('[value=Cobrar]').disabled=payment.dataset.payment!=='PENDIENTE_PAGO'||cents!==remaining||!body.rows.length;payment.querySelector('.payment-empty').hidden=body.rows.length>0;
   const draft=Number(payment.querySelector('#NuevoMonto').value);payment.querySelector('[data-payment-preview]').textContent=paid?'No se permiten nuevos cobros para esta factura.':draft>0?'Con este medio quedaría '+money((remaining-cents)/100-draft)+(Math.round(draft*100)+cents>remaining?' · El monto excede el pendiente.':' por distribuir antes de confirmar.'):'Agrega los medios necesarios para cubrir el total.';
  }
  body.addEventListener('input',()=>{payment.dataset.dirty='true';refresh();});payment.querySelector('#NuevoMonto').addEventListener('input',refresh);
  payment.addEventListener('submit',e=>{const action=e.submitter?.value;if(action==='Cobrar'){const invalid=[...body.querySelectorAll('input')].find(i=>!i.validity.valid);if(invalid){e.preventDefault();invalid.reportValidity();ccToast('Revisa los montos y las referencias de los medios agregados.','error');}return;}if(action!=='Agregar'&&!action?.startsWith('Quitar:'))return;e.preventDefault();
   if(action.startsWith('Quitar:')){body.rows[Number(action.split(':')[1])]?.remove();payment.dataset.dirty='true';refresh();return;}
   const name=medium.selectedOptions[0]?.textContent,amount=Number(payment.querySelector('#NuevoMonto').value),reference=payment.querySelector('#NuevaReferencia').value.trim();
   if(medium.value==='0'||!Number.isFinite(amount)||amount<=0){ccToast('Selecciona un medio e ingresa un monto positivo.','error');return;}
   if(['Cheque','Tarjeta'].includes(name)&&!reference){ccToast('Este medio necesita una referencia.','error');payment.querySelector('#NuevaReferencia').focus();return;}
   if([...body.querySelectorAll('[name$="MedioPagoID"]')].some(i=>i.value===medium.value)){ccToast('El medio ya está agregado. Edita su monto.','warning');return;}
   const row=node('tr'),method=node('td',name);method.dataset.label='Medio';method.append(input('Pagos[0].MedioPagoID',medium.value));row.append(method);
   const refCell=node('td');refCell.dataset.label='Referencia';const ref=input('Pagos[0].Referencia',reference,'text','Referencia '+name);ref.maxLength=100;ref.required=['Cheque','Tarjeta'].includes(name);refCell.append(ref);row.append(refCell);
   const amountCell=node('td');amountCell.dataset.label='Monto';const value=input('Pagos[0].Monto',amount,'number','Monto '+name);value.className='payment-amount';value.min='.01';value.step='.01';value.required=true;amountCell.append(value);row.append(amountCell);
   const actionCell=node('td');actionCell.dataset.label='Acción';const remove=node('button','Quitar','text-button');remove.type='submit';remove.name='accion';remove.value='Quitar:0';remove.formNoValidate=true;actionCell.append(remove);row.append(actionCell);body.append(row);payment.dataset.dirty='true';payment.querySelector('#NuevoMonto').value='0';payment.querySelector('#NuevaReferencia').value='';refresh();
  });refresh();
 }
 document.querySelectorAll('.receipt-line').forEach((line,index)=>{
  const location=line.querySelector('select'),groups=[...location.querySelectorAll('optgroup')],selected=location.value;
  const field=node('div',null,'field'),label=node('label','Bodega'),warehouse=node('select');warehouse.id='bodega-recepcion-'+index;label.htmlFor=warehouse.id;warehouse.add(new Option('Selecciona una bodega',''));groups.forEach(g=>warehouse.add(new Option(g.label,g.label)));field.append(label,warehouse);location.closest('.field').before(field);line.querySelector('label[for="'+location.id+'"]').textContent='Ubicación';
  const refill=()=>{const previous=location.value;location.replaceChildren(new Option('Selecciona dónde almacenar','0'));const group=groups.find(g=>g.label===warehouse.value);if(group)[...group.querySelectorAll('option')].forEach(o=>location.add(o.cloneNode(true)));if([...location.options].some(o=>o.value===previous))location.value=previous;};
  warehouse.value=groups.find(g=>[...g.querySelectorAll('option')].some(o=>o.value===selected))?.label||'';warehouse.addEventListener('change',refill);refill();
  const quantity=line.querySelector('[name$="CantidadRecibida"]'),check=line.querySelector('[type=checkbox]'),status=node('p','', 'receipt-preview helper-note');status.setAttribute('aria-live','polite');line.append(status);
  const refresh=()=>{const remaining=Number(quantity.max),amount=Number(quantity.value);status.textContent=!check.checked?'Esta línea no se incluirá.':amount>remaining?'La cantidad supera lo pendiente.':amount===remaining?'Completa lo pendiente de esta línea.':'Recepción parcial: quedarán '+(remaining-amount).toLocaleString('es-GT')+' unidades de esta línea.';};quantity.addEventListener('input',refresh);check.addEventListener('change',refresh);refresh();
 });
})();
