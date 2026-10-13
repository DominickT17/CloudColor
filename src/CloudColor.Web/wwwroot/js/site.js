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
const dialog=document.querySelector('#confirmar');
document.addEventListener('submit',e=>{const form=e.target;if(!(form instanceof HTMLFormElement)||form.method.toLowerCase()!=='post'||form.closest('dialog'))return;
if(form.dataset.confirm&&!form.dataset.confirmed&&dialog){e.preventDefault();document.querySelector('#confirmar-texto').textContent=form.dataset.confirm;dialog.showModal();dialog.addEventListener('close',()=>{if(dialog.returnValue==='confirmar'){form.dataset.confirmed='true';form.requestSubmit();}},{once:true});return;}
if(e.submitter?.name){const h=document.createElement('input');h.type='hidden';h.name=e.submitter.name;h.value=e.submitter.value;h.dataset.submitterCopy='true';form.append(h);}
form.setAttribute('aria-busy','true');form.querySelectorAll('button[type=submit]').forEach(b=>{pendingButtons.set(b,{html:b.innerHTML,disabled:b.disabled});b.disabled=true;b.textContent='Procesando…';});});

document.querySelector('#NuevaVariante')?.addEventListener('change',e=>{if(e.target.dataset.suggestPrice==='true'){const price=e.target.selectedOptions[0]?.dataset.precio;if(price)document.querySelector('#NuevoPrecio').value=price;}});
document.querySelector('#imprimir')?.addEventListener('click',()=>window.print());
