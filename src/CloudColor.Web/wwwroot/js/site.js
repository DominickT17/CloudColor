const toggle=document.querySelector('.menu-toggle'),backdrop=document.querySelector('.sidebar-backdrop');
const shell=document.querySelector('.app-shell'),sidebar=document.querySelector('.sidebar');
const mobile=window.matchMedia('(max-width:900px)');
function menu(open,restoreFocus=true){
 document.body.classList.toggle('menu-open',open);
 toggle?.setAttribute('aria-expanded',String(open));
 if(backdrop)backdrop.hidden=!open;
 if(shell)shell.inert=open;
 if(open)sidebar?.querySelector('a')?.focus();
 else if(restoreFocus&&mobile.matches)toggle?.focus();
}
toggle?.addEventListener('click',()=>menu(!document.body.classList.contains('menu-open')));
backdrop?.addEventListener('click',()=>menu(false));
mobile.addEventListener('change',()=>menu(false,false));
document.addEventListener('keydown',e=>{
 if(!document.body.classList.contains('menu-open'))return;
 if(e.key==='Escape'){e.preventDefault();menu(false);}
 if(e.key==='Tab'){
  const links=sidebar.querySelectorAll('a[href],button:not([disabled])');
  const first=links[0],last=links[links.length-1];
  if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}
  else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}
 }
});
document.querySelectorAll('.nav-link.active').forEach(link=>link.setAttribute('aria-current','page'));
// El historial puede restaurar el DOM con botones todavia deshabilitados.
const pendingButtons=new Map();
window.addEventListener('pageshow',()=>{
 pendingButtons.forEach((state,button)=>{button.innerHTML=state.html;button.disabled=state.disabled;});
 pendingButtons.clear();
 document.querySelectorAll('form[aria-busy]').forEach(form=>{form.removeAttribute('aria-busy');delete form.dataset.confirmed;});
 document.querySelectorAll('[data-submitter-copy]').forEach(input=>input.remove());
 menu(false,false);
});

document.querySelector('#NuevaVariante')?.addEventListener('change',e=>{if(e.target.dataset.suggestPrice==='true'){const price=e.target.selectedOptions[0]?.dataset.precio;if(price)document.querySelector('#NuevoPrecio').value=price;}});
document.querySelector('#imprimir')?.addEventListener('click',()=>window.print());
document.querySelector('[data-export-report]')?.addEventListener('click',event=>{
 const button=event.currentTarget,table=document.querySelector('.report-table-card table');
 const heading=document.querySelector('.page-heading h1')?.textContent.trim()||'Reporte CloudColor';
 const rows=[['Reporte',heading],['Desde',button.dataset.reportFrom],['Hasta',button.dataset.reportTo],[]];
 if(table){
  rows.push([...table.tHead.rows[0].cells].map(cell=>cell.textContent.replace('↕','').trim()));
  [...table.tBodies[0].rows].filter(row=>!row.hidden).forEach(row=>rows.push([...row.cells].map(cell=>cell.innerText.replace(/\s+/g,' ').trim())));
 }else{
  rows.push(['Indicador','Monto']);
  document.querySelectorAll('.report-metrics .metric').forEach(card=>rows.push([card.querySelector('span')?.textContent.trim()||'',card.querySelector('.metric-number')?.textContent.trim()||'']));
 }
 const csv=rows.map(row=>row.map(value=>{
  const text=String(value??'');
  const safe=/^\s*[=+@-]/.test(text)?"'"+text:text;
  return '"'+safe.replace(/"/g,'""')+'"';
 }).join(',')).join('\r\n');
 const blob=new Blob(['\uFEFF',csv],{type:'text/csv;charset=utf-8'}),url=URL.createObjectURL(blob),link=document.createElement('a');
 link.href=url;link.download=`cloudcolor-${button.dataset.reportName}-${button.dataset.reportFrom}-${button.dataset.reportTo}.csv`;document.body.append(link);link.click();link.remove();
 setTimeout(()=>URL.revokeObjectURL(url),1000);
 const status=document.querySelector('[data-export-status]');if(status)status.textContent='Reporte descargado como archivo CSV.';
});

// Mejoras progresivas: los formularios conservan su funcionamiento sin JavaScript.
document.querySelectorAll('input[type="password"]').forEach(input=>{
 const wrap=document.createElement('div');wrap.className='password-control';input.before(wrap);wrap.append(input);
 const button=document.createElement('button');button.type='button';button.className='text-button';
 button.textContent='Mostrar';button.setAttribute('aria-label','Mostrar contraseña');button.setAttribute('aria-pressed','false');button.setAttribute('aria-controls',input.id);wrap.append(button);
 const hide=()=>{input.type='password';button.textContent='Mostrar';button.setAttribute('aria-label','Mostrar contraseña');button.setAttribute('aria-pressed','false');};
 button.addEventListener('click',()=>{
  if(input.type==='text'){hide();return;}
  input.type='text';button.textContent='Ocultar';button.setAttribute('aria-label','Ocultar contraseña');button.setAttribute('aria-pressed','true');
 });
 const warning=document.createElement('small');warning.className='caps-warning';warning.hidden=true;warning.textContent='Bloq Mayús está activado.';warning.setAttribute('role','status');wrap.after(warning);
 input.addEventListener('keyup',event=>{warning.hidden=!event.getModifierState('CapsLock');});
 input.addEventListener('blur',()=>{warning.hidden=true;});
 window.addEventListener('pageshow',hide);
});

const quantity=document.querySelector('#NuevaCantidad'),price=document.querySelector('#NuevoPrecio');
if(quantity&&price){
 const preview=document.createElement('div');preview.className='line-preview';preview.setAttribute('role','status');preview.setAttribute('aria-live','polite');
 document.querySelector('.line-entry').after(preview);
 const currency=new Intl.NumberFormat('es-GT',{style:'currency',currency:'GTQ'});
 const refresh=()=>{
  const q=Number(quantity.value),p=Number(price.value),total=q*p;
  preview.textContent=quantity.value!==''&&price.value!==''&&q>0&&p>=0&&Number.isFinite(total)
   ?'Subtotal de la línea: '+currency.format(total)+' · Pulsa Agregar para incorporarla al documento.'
   :'Ingresa cantidad y precio válidos para calcular esta línea.';
 };
 quantity.addEventListener('input',refresh);price.addEventListener('input',refresh);
 document.querySelector('#NuevaVariante')?.addEventListener('change',refresh);refresh();
}

const normalize=text=>text.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLocaleLowerCase('es').trim();
document.querySelectorAll('.table-wrap table').forEach((table,index)=>{
 const card=table.closest('.card');
 if(!card?.querySelector('form.search,form.inventory-filters'))return;
 const rows=[...table.tBodies[0].rows];if(!rows.length)return;
 const bar=document.createElement('div');bar.className='table-tools';
 const field=document.createElement('div');field.className='field';
 const label=document.createElement('label');label.htmlFor='filter-visible-'+index;label.textContent='Filtrar resultados visibles';
 const input=document.createElement('input');input.type='search';input.id=label.htmlFor;input.placeholder='Escribe cualquier dato de la tabla…';
 field.append(label,input);bar.append(field);
 const states=[...new Set(rows.map(row=>row.querySelector('.badge')?.textContent.replace('•','').trim()).filter(Boolean))];
 let state;
 if(states.length){
  const stateField=document.createElement('div');stateField.className='field';
  const stateLabel=document.createElement('label');stateLabel.htmlFor='filter-state-'+index;stateLabel.textContent='Estado visible';
  state=document.createElement('select');state.id=stateLabel.htmlFor;state.add(new Option('Todos los estados',''));
  states.forEach(value=>state.add(new Option(value,value)));stateField.append(stateLabel,state);bar.append(stateField);
 }
 const reset=document.createElement('button');reset.type='button';reset.className='text-button';reset.textContent='Restablecer filtros';bar.append(reset);
 const counter=document.createElement('span');counter.className='table-count';counter.setAttribute('role','status');counter.setAttribute('aria-live','polite');bar.append(counter);
 table.closest('.table-wrap').before(bar);
 const chips=document.createElement('div');chips.className='filter-chips local-chips';bar.append(chips);
 const data=rows.map(row=>({row,text:normalize(row.textContent),state:row.querySelector('.badge')?.textContent.replace('•','').trim()}));
 const filter=()=>{
  const term=normalize(input.value);let count=0;
  data.forEach(item=>{item.row.hidden=!item.text.includes(term)||Boolean(state?.value&&item.state!==state.value);if(!item.row.hidden)count++;});
  if(window.ccPulse)ccPulse(table.tBodies[0]);
  counter.textContent=count===0?'Sin coincidencias en los resultados cargados.':`${count} de ${rows.length} registros visibles`;
  reset.disabled=!input.value&&!state?.value;
  chips.replaceChildren();
  for(const [label,clear] of [[input.value,()=>input.value=''],[state?.value,()=>state.value='']]){if(!label)continue;const chip=document.createElement('button');chip.type='button';chip.className='filter-chip';chip.textContent=label+' ×';chip.setAttribute('aria-label','Quitar filtro visible '+label);chip.addEventListener('click',()=>{clear();filter();});chips.append(chip);}
 };
 input.addEventListener('input',filter);state?.addEventListener('change',filter);
 reset.addEventListener('click',()=>{input.value='';if(state)state.value='';filter();input.focus();});filter();
 const collator=new Intl.Collator('es',{numeric:true,sensitivity:'base'});
 [...table.tHead.rows[0].cells].forEach((header,column)=>{
  // Las fechas formateadas no se ordenan como texto: conservan el orden del servidor.
  if(['acciones','accion','fecha','ingreso'].includes(normalize(header.textContent)))return;
  const title=header.textContent.trim(),button=document.createElement('button');button.type='button';button.className='sort-button';button.textContent=title+' ↕';button.setAttribute('aria-label','Ordenar por '+title);
  header.textContent='';header.append(button);header.setAttribute('aria-sort','none');
  button.addEventListener('click',()=>{
   const direction=header.getAttribute('aria-sort')==='ascending'?-1:1;
   table.querySelectorAll('th[aria-sort]').forEach(th=>th.setAttribute('aria-sort','none'));
   header.setAttribute('aria-sort',direction===1?'ascending':'descending');
   const value=row=>row.cells[column].textContent.trim();
   const numeric=text=>/^(?:Q\s*)?\d+(?:,\d{3})*(?:\.\d+)?$/.test(text)?Number(text.replace(/[^\d.]/g,'')):null;
   rows.sort((a,b)=>{const x=value(a),y=value(b),nx=numeric(x),ny=numeric(y);return direction*(nx!==null&&ny!==null?nx-ny:collator.compare(x,y));});
   rows.forEach(row=>table.tBodies[0].append(row));
   button.setAttribute('aria-label','Ordenar por '+title+(direction===1?' descendente':' ascendente'));
  });
 });
});
