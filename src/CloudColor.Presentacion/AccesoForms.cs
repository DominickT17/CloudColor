using CloudColor.Negocio;
namespace CloudColor.Presentacion;
public sealed class ConfiguracionInicialForm : DialogoBase
{
    public ConfiguracionInicialForm(Aplicacion app) : base("Primera cuenta Gerente")
    {
        var codigo=new TextBox { Name="CodigoEmpleado",MaxLength=30 };
        var nombres=new TextBox { Name="Nombres",MaxLength=100 };
        var apellidos=new TextBox { Name="Apellidos",MaxLength=100 };
        var usuario=new TextBox { Name="NombreUsuario",MaxLength=50 };
        var password=new TextBox { Name="Password",UseSystemPasswordChar=true,MaxLength=128 };
        var confirmar=new TextBox { Name="Confirmar",UseSystemPasswordChar=true,MaxLength=128 };
        Campo("Código empleado",codigo); Campo("Nombres",nombres); Campo("Apellidos",apellidos);
        Campo("Usuario",usuario); Campo("Contraseña (12+)",password); Campo("Confirmar",confirmar);
        ClientSize=new Size(560,480);
        Terminar("Crear mi cuenta",()=>
        {
            if(password.Text!=confirmar.Text) throw new ArgumentException("Las contraseñas no coinciden.");
            app.Seguridad.Configurar(codigo.Text,nombres.Text,apellidos.Text,usuario.Text,password.Text);
            password.Clear(); confirmar.Clear(); DialogResult=DialogResult.OK; Close();
        });
        FormClosed+=(_,_)=> { password.Clear(); confirmar.Clear(); };
    }
}
public sealed class LoginForm : DialogoBase
{
    public LoginForm(Aplicacion app) : base("Iniciar sesión")
    {
        ClientSize=new Size(540,320); MinimumSize=new Size(540,320);
        var usuario=new TextBox { Name="NombreUsuario",MaxLength=50 };
        var password=new TextBox { Name="Password",UseSystemPasswordChar=true,MaxLength=128 };
        Campo("Usuario",usuario); Campo("Contraseña",password);
        Terminar("Iniciar sesión",()=>
        {
            try { app.Seguridad.Login(usuario.Text,password.Text); DialogResult=DialogResult.OK; Close(); }
            finally { password.Clear(); }
        });
        FormClosed+=(_,_)=>password.Clear();
    }
}
public sealed class MarcaForm : DialogoBase
{
    public MarcaForm(Aplicacion app) : base("Nueva marca")
    {
        var nombre=new TextBox { Name="Nombre",MaxLength=100 };
        Campo("Nombre real",nombre);
        Terminar("Guardar marca",()=> { app.Catalogos.CrearMarca(nombre.Text); DialogResult=DialogResult.OK; Close(); });
    }
}
