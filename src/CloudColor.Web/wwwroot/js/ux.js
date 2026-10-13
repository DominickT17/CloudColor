/* Interacciones compartidas; sin datos sensibles en almacenamiento del navegador. */
(() => {
 const $=(s,root=document)=>root.querySelector(s);
 const modal=$('#confirmar');let confirming=false,leaving=false;
 window.ccConfirm=(message,title='Confirmar operación',action='Confirmar',danger=false)=>new Promise(resolve=>{
  if(!modal||confirming){resolve(false);return;}confirming=true;
  $('#confirmar-titulo').textContent=title;$('#confirmar-texto').textContent=message;
  const button=$('[value=confirmar]',modal);button.textContent=action;button.className='button '+(danger?'danger':'primary');
  const opener=document.activeElement;modal.returnValue='';modal.showModal();modal.addEventListener('close',()=>{confirming=false;if(opener?.isConnected)opener.focus();resolve(modal.returnValue==='confirmar');},{once:true});
 });
 function prepareToast(toast){
  let timer;const close=()=>toast.remove(),pause=()=>clearTimeout(timer),resume=()=>{pause();if(!toast.matches(':hover,:focus-within'))timer=setTimeout(close,4000);};
  $('button',toast)?.addEventListener('click',close);toast.addEventListener('mouseenter',pause);toast.addEventListener('mouseleave',resume);toast.addEventListener('focusin',pause);toast.addEventListener('focusout',()=>setTimeout(resume));resume();
 }
 document.querySelectorAll('.toast').forEach(prepareToast);
 window.ccToast=(text,state='info')=>{const stack=$('.toast-stack');if(!stack)return;const toast=document.createElement('div');toast.className='toast '+(['success','info','warning','error'].includes(state)?state:'info');toast.setAttribute('role',state==='error'?'alert':'status');const label=document.createElement('span');label.textContent=text;const close=document.createElement('button');close.type='button';close.className='text-button';close.textContent='×';close.setAttribute('aria-label','Cerrar notificación');toast.append(label,close);stack.append(toast);prepareToast(toast);};
 const tracked=[...document.querySelectorAll('main form[method=post]')].filter(f=>!f.closest('dialog')&&!f.dataset.confirm&&!/\/Estado\//.test(f.action)&&new URL(f.action).pathname!=='/Login');
 const dirty=()=>tracked.some(f=>f.dataset.dirty==='true');
 tracked.forEach(form=>{if(form.querySelector('[name^="Detalle["],[name^="Pagos["]'))form.dataset.dirty='true';form.addEventListener('input',()=>{form.dataset.dirty='true';});form.addEventListener('change',()=>{form.dataset.dirty='true';});});
 window.addEventListener('beforeunload',e=>{if(dirty()&&!leaving){e.preventDefault();e.returnValue='';}});
 window.addEventListener('pageshow',()=>{leaving=false;});
 document.addEventListener('click',async e=>{
  const link=e.target.closest('a[href]');if(!link||link.target==='_blank'||link.hash||e.ctrlKey||e.metaKey||e.shiftKey||e.button!==0||!dirty())return;
  e.preventDefault();if(await ccConfirm('Los cambios de este formulario todavía no están guardados.','Salir del formulario','Salir sin guardar',true)){leaving=true;location.assign(link.href);}
 });
 document.addEventListener('submit',e=>{
  const form=e.target;if(e.defaultPrevented||!(form instanceof HTMLFormElement)||form.method.toLowerCase()!=='post'||form.closest('dialog'))return;
  const submitter=e.submitter,logout=new URL(form.action).pathname==='/Salir';
  const message=submitter?.dataset.confirm||form.dataset.confirm||(logout&&dirty()?'Hay cambios sin guardar. ¿Deseas cerrar la sesión?':null);
  if(message&&!form.dataset.confirmed&&modal){e.preventDefault();ccConfirm(message,submitter?.dataset.confirmTitle||'Confirmar operación',submitter?.dataset.confirmAction||'Confirmar',logout||/desactivar/i.test(message)).then(ok=>{if(ok){form.dataset.confirmed='true';form.requestSubmit(submitter);}});return;}
  if(submitter?.name){const h=document.createElement('input');h.type='hidden';h.name=submitter.name;h.value=submitter.value;h.dataset.submitterCopy='true';form.append(h);}
  leaving=true;form.setAttribute('aria-busy','true');form.querySelectorAll('button[type=submit]').forEach(button=>{pendingButtons.set(button,{html:button.innerHTML,disabled:button.disabled});button.disabled=true;button.textContent=/cobro|cobrar/i.test(button.textContent)?'Registrando cobro…':/factura/i.test(button.textContent)?'Emitiendo…':/confirmar/i.test(button.textContent)?'Confirmando…':'Guardando…';button.classList.add('is-loading');});
 });
 window.addEventListener('pageshow',()=>document.querySelectorAll('.is-loading').forEach(b=>b.classList.remove('is-loading')));
 // La validación nativa y los mensajes del servidor se complementan al salir del campo.
 tracked.forEach(form=>form.querySelectorAll('input:not([type=hidden]),select,textarea').forEach((input,i)=>{
  if(!input.id)input.id='campo-'+tracked.indexOf(form)+'-'+i;
  const hint=document.createElement('small');hint.className='field-error live-validation';hint.id=input.id+'-ayuda';hint.hidden=true;input.after(hint);
  const originalDescription=input.getAttribute('aria-describedby')||'';
  const validate=()=>{if(input.disabled)return;const v=input.validity,valid=v.valid;hint.hidden=valid;hint.textContent=valid?'':v.valueMissing?'Completa este campo.':v.typeMismatch?'Revisa el formato de este campo.':v.rangeUnderflow?'El mínimo permitido es '+input.min+'.':v.rangeOverflow?'El máximo permitido es '+input.max+'.':v.stepMismatch?'Revisa la precisión del importe o cantidad.':'Revisa este valor antes de guardar.';input.setAttribute('aria-invalid',String(!valid));input.setAttribute('aria-describedby',(originalDescription+' '+hint.id).trim());};
  input.addEventListener('blur',validate);input.addEventListener('input',validate);
 }));
 // Búsqueda en la cabecera: no hay atajo para abrirla.
 const search=$('.global-search'),query=$('#global-query'),results=$('#global-results');
 const clear=$('.search-clear'),status=$('#search-status');let delay,request;
 const closeSearch=()=>{if(!results)return;results.hidden=true;query.setAttribute('aria-expanded','false');clearTimeout(delay);request?.abort();};
 const openResults=()=>{results.hidden=false;query.setAttribute('aria-expanded','true');};
 const message=text=>{results.textContent=text;status.textContent=text;};
 async function runSearch(term){
  request=new AbortController();
  try{
   const response=await fetch('/Buscar?q='+encodeURIComponent(term),{signal:request.signal,headers:{Accept:'application/json'}});
   if(!response.ok||response.redirected)throw new Error();
   const groups=await response.json();if(query.value.trim()!==term)return;
   results.replaceChildren();
   if(!groups.length){message('No encontramos resultados para "'+term+'".');return;}
   let count=0;
   groups.forEach(group=>{
    const section=document.createElement('section');section.className='search-group';
    const heading=document.createElement('h3');heading.textContent=group.nombre;section.append(heading);
    group.resultados.forEach(result=>{
     const link=document.createElement('a');link.href=result.ruta;
     const title=document.createElement('span');title.textContent=result.texto;
     const detail=document.createElement('small');detail.textContent=group.nombre+' · Abrir registro';
     link.append(title,detail);section.append(link);count++;
    });results.append(section);
   });status.textContent=count+' resultados encontrados.';
  }catch(error){if(error.name!=='AbortError')message('No pudimos buscar. Revisa tu conexión o vuelve a iniciar sesión.');}
 }
 const scheduleSearch=()=>{
  clearTimeout(delay);request?.abort();const term=query.value.trim();clear.hidden=!query.value;
  if(term.length<2){closeSearch();status.textContent=term?'Escribe al menos 2 caracteres.':'';return;}
  openResults();message('Buscando…');delay=setTimeout(()=>runSearch(term),300);
 };
 query?.addEventListener('input',scheduleSearch);
 query?.addEventListener('focus',()=>{if(query.value.trim().length>=2&&results.hidden)scheduleSearch();});
 clear?.addEventListener('click',()=>{query.value='';closeSearch();clear.hidden=true;status.textContent='Búsqueda limpia.';query.focus();});
 search?.addEventListener('keydown',e=>{
  if(e.key==='Escape'&&!results.hidden){e.preventDefault();e.stopPropagation();query.focus();closeSearch();return;}
  if(results.hidden||!['ArrowDown','ArrowUp'].includes(e.key))return;
  const links=[...results.querySelectorAll('a')];if(!links.length)return;e.preventDefault();
  const current=links.indexOf(document.activeElement),next=e.key==='ArrowDown'?(current+1)%links.length:(current<=0?links.length-1:current-1);links[next].focus();
 });
 const session=$('.session-menu');
 document.addEventListener('click',e=>{if(search&&!search.contains(e.target))closeSearch();if(session&&!session.contains(e.target))session.open=false;});
 search?.addEventListener('focusout',()=>setTimeout(()=>{if(!search.contains(document.activeElement))closeSearch();},0));
 document.querySelectorAll('[data-close-dialog]').forEach(b=>b.addEventListener('click',()=>b.closest('dialog').close()));
 document.addEventListener('keydown',e=>{
  if(e.altKey&&e.key.toLowerCase()==='n'&&!e.target.matches('input,textarea,select')&&!$('dialog[open]')){const link=$('.page-heading a[href*="/Nueva"],.page-heading a[href*="/Editar"]');if(link){e.preventDefault();link.click();}}
  if(e.key==='Tab'){
   const dialog=[...document.querySelectorAll('dialog[open]')].at(-1);
   if(dialog){const controls=[...dialog.querySelectorAll('button:not([disabled]),a[href],input:not([disabled]):not([type=hidden]),select:not([disabled]),textarea:not([disabled])')].filter(c=>c.getClientRects().length);const first=controls[0],last=controls.at(-1);
    if(first&&((e.shiftKey&&document.activeElement===first)||(!e.shiftKey&&document.activeElement===last)||!dialog.contains(document.activeElement))){e.preventDefault();(e.shiftKey?last:first).focus();}
   }
  }
  if(e.key==='Escape'){const dialogs=[...document.querySelectorAll('dialog[open]')];if(dialogs.length){e.preventDefault();dialogs.at(-1).close('cancelar');}if(session?.open){session.open=false;$('summary',session).focus();}}
 });
 const newLink=$('.page-heading a[href*="/Nueva"],.page-heading a[href*="/Editar"]');if(newLink)newLink.title='Nuevo registro · Alt N';
 // Animaciones breves de presentación; el valor accesible conserva siempre el total real.
 const reduced=window.matchMedia('(prefers-reduced-motion: reduce)');
 window.ccPulse=element=>{if(element&&!reduced.matches)element.animate([{backgroundColor:'#eeedf8'},{backgroundColor:'transparent'}],{duration:300});};
 document.querySelectorAll('.metric-number').forEach(element=>{
  const original=element.textContent,value=Number(original.replace(/,/g,''));if(reduced.matches||!Number.isFinite(value)||value===0)return;
  const decimals=original.includes('.')?original.split('.')[1].length:0,formatter=new Intl.NumberFormat('es-GT',{minimumFractionDigits:decimals,maximumFractionDigits:decimals});
  element.setAttribute('aria-label',original);const started=performance.now();
  const step=now=>{const part=Math.min((now-started)/400,1);element.textContent=part===1?original:formatter.format(value*(1-Math.pow(1-part,3)));if(part<1)requestAnimationFrame(step);};requestAnimationFrame(step);
 });
 const progressCard=$('[data-progress-key]');
 if(progressCard){
  const progress=$('progress',progressCard),target=progress.value,key=progressCard.dataset.progressKey;
  // Solo progreso visual por pestaña; no se persisten borradores ni datos personales.
  let previous=0;try{previous=Number(sessionStorage.getItem(key)||0);sessionStorage.setItem(key,String(target));}catch{}
  if(!reduced.matches&&previous!==target){const from=Math.min(previous,target),started=performance.now();progress.setAttribute('aria-label','Recibidos '+target+' de '+progress.max);const step=now=>{const part=Math.min((now-started)/400,1);progress.value=from+(target-from)*part;if(part<1)requestAnimationFrame(step);else if(target===progress.max)ccPulse(progressCard);};requestAnimationFrame(step);}
 }
 // Chips para filtros que ya se aplicaron en el servidor.
 const params=new URLSearchParams(location.search),filterForm=$('form.inventory-filters,form.search');
 if(filterForm&&[...params].some(([k,v])=>['buscar','variante','bodega','ubicacion','estado'].includes(k)&&v)){
  const chips=document.createElement('nav');chips.className='filter-chips';chips.setAttribute('aria-label','Filtros aplicados');
  for(const [key,value] of params){if(!value||!['buscar','variante','bodega','ubicacion','estado'].includes(key))continue;const field=filterForm.elements.namedItem(key);const label=field?.selectedOptions?.[0]?.textContent||value;const link=document.createElement('a');const next=new URL(location.href);next.searchParams.delete(key);link.href=next.pathname+next.search;link.className='filter-chip';link.textContent=key+': '+label+' ×';link.setAttribute('aria-label','Quitar filtro '+key+': '+label);chips.append(link);}
  const clear=document.createElement('a');clear.href=location.pathname;clear.textContent='Limpiar filtros';chips.append(clear);filterForm.closest('.card')?.before(chips);
 }
 // Presentación móvil legible y vista rápida sin otra consulta ni escritura.
 document.querySelectorAll('main .table-wrap table').forEach(table=>{
  const headers=[...table.tHead?.rows[0]?.cells||[]].map(th=>th.textContent.replace('↕','').trim());table.classList.add('mobile-cards');
  [...table.tBodies[0]?.rows||[]].forEach(row=>{[...row.cells].forEach((cell,i)=>cell.dataset.label=headers[i]||'Detalle');
   const link=row.querySelector('a[href*="/Ver/"],a[href*="/Editar/"],a[href*="/Recepciones/Detalle/"]');if(!link)return;
   row.classList.add('clickable-row');row.addEventListener('click',e=>{if(e.target.closest('a,button,input,select,textarea,label,form')||window.getSelection()?.toString())return;link.click();});
   const button=document.createElement('button');button.type='button';button.className='text-button quick-view';button.textContent='Vista rápida';button.title='Consultar sin salir del listado';link.parentElement.append(button);
   button.addEventListener('click',()=>{const panel=$('#detalle-rapido'),content=$('.quick-content',panel);content.replaceChildren();const dl=document.createElement('dl');[...row.cells].forEach((cell,i)=>{if(cell.contains(link))return;const dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=headers[i];dd.textContent=cell.textContent;dl.append(dt,dd);});const full=document.createElement('a');full.className='button primary';full.href=link.href;full.textContent='Abrir registro';content.append(dl,full);panel.showModal();});
  });
 });
})();
