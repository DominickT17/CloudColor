const toggle=document.querySelector('.menu-toggle'),backdrop=document.querySelector('.sidebar-backdrop');
function menu(open){document.body.classList.toggle('menu-open',open);toggle?.setAttribute('aria-expanded',String(open));if(backdrop)backdrop.hidden=!open;if(open)document.querySelector('.sidebar a')?.focus();else if(document.body.contains(toggle))toggle?.focus();}
toggle?.addEventListener('click',()=>menu(!document.body.classList.contains('menu-open')));backdrop?.addEventListener('click',()=>menu(false));document.addEventListener('keydown',e=>{if(e.key==='Escape')menu(false);});
const dialog=document.querySelector('#confirmar');
document.addEventListener('submit',e=>{const form=e.target;if(!(form instanceof HTMLFormElement)||form.method.toLowerCase()!=='post'||form.closest('dialog'))return;
if(form.dataset.confirm&&!form.dataset.confirmed&&dialog){e.preventDefault();document.querySelector('#confirmar-texto').textContent=form.dataset.confirm;dialog.showModal();dialog.addEventListener('close',()=>{if(dialog.returnValue==='confirmar'){form.dataset.confirmed='true';form.requestSubmit();}},{once:true});return;}
if(e.submitter?.name){const h=document.createElement('input');h.type='hidden';h.name=e.submitter.name;h.value=e.submitter.value;form.append(h);}
form.setAttribute('aria-busy','true');form.querySelectorAll('button[type=submit]').forEach(b=>{b.disabled=true;b.textContent='Procesando…';});});

document.querySelector('#NuevaVariante')?.addEventListener('change',e=>{if(e.target.dataset.suggestPrice==='true'){const price=e.target.selectedOptions[0]?.dataset.precio;if(price)document.querySelector('#NuevoPrecio').value=price;}});
document.querySelector('#imprimir')?.addEventListener('click',()=>window.print());
