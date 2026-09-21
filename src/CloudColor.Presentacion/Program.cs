using CloudColor.Negocio;
namespace CloudColor.Presentacion;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            using var app=new Aplicacion();
            if(app.Seguridad.InicialPendiente())
            {
                using var inicial=new ConfiguracionInicialForm(app);
                if(inicial.ShowDialog()!=DialogResult.OK) return;
            }
            while(true)
            {
                using var login=new LoginForm(app);
                if(login.ShowDialog()!=DialogResult.OK) break;
                using var menu=new MenuPrincipalForm(app); menu.ShowDialog();
                if(!menu.VolverLogin) break;
            }
        }
        catch(Exception)
        {
            MessageBox.Show("No fue posible iniciar CloudColor. Revise conexion.json, el servicio SQL Server y los scripts 00 a 05.",
                "CloudColor",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }
}
