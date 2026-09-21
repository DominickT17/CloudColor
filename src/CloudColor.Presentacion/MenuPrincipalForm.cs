using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class MenuPrincipalForm : Form
{
    public bool VolverLogin { get; private set; }
    public MenuPrincipalForm(Aplicacion app)
    {
        var sesion=app.Seguridad.Actual();
        Text="CloudColor · Fase I"; Name="MenuPrincipal"; Size=new Size(620,440); MinimumSize=new Size(580,420);
        StartPosition=FormStartPosition.CenterScreen; Font=new Font("Segoe UI",11);
        var panel=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(24),WrapContents=false };
        Controls.Add(panel);
        panel.Controls.Add(new Label { Text=$"{sesion.NombreUsuario} · {sesion.Rol}",AutoSize=true,Margin=new Padding(8) });
        void Boton(string texto,bool habilitado,Func<Form> crear,bool gerente=false)
        {
            var boton=new Button { Name=texto,Text=texto,Enabled=habilitado,Width=260,Height=34 };
            boton.Click+=(_,_)=>
            {
                try
                {
                    if(gerente) app.Seguridad.ExigirGerente(); else app.Seguridad.ExigirMantenimiento();
                    using var form=crear(); form.ShowDialog(this);
                }
                catch(InvalidOperationException ex) { MessageBox.Show(this,ex.Message,"Acceso",MessageBoxButtons.OK,MessageBoxIcon.Information); }
            };
            panel.Controls.Add(boton);
        }
        bool mantenimiento=sesion.Rol is "Gerente" or "Digitador";
        Boton("Cliente",mantenimiento,()=>new ClienteForm(app)); Boton("Proveedor",mantenimiento,()=>new ProveedorForm(app));
        Boton("Producto",mantenimiento,()=>new ProductoForm(app)); Boton("Empleado",mantenimiento,()=>new EmpleadoForm(app));
        Boton("Usuarios",sesion.Rol=="Gerente",()=>new UsuariosForm(app),true); Boton("Roles",sesion.Rol=="Gerente",()=>new RolesForm(app),true);
        if(sesion.Rol=="Cajero") panel.Controls.Add(new Label { Text="Cobros disponibles en una fase posterior.",AutoSize=true });
        var salir=new Button { Text="Cerrar sesión",AutoSize=true };
        salir.Click+=(_,_)=> { app.Seguridad.CerrarSesion(); VolverLogin=true; Close(); }; panel.Controls.Add(salir);
        ClientSize=new Size(600,480);
    }
}
