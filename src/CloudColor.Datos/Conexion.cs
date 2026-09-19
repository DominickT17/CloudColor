using System.Text.Json;
using Microsoft.Data.SqlClient;
namespace CloudColor.Datos;
public static class Conexion
{
    public static string LeerCadena()
    {
        string local = Path.Combine(AppContext.BaseDirectory, "conexion.local.json");
        string archivo = File.Exists(local) ? local : Path.Combine(AppContext.BaseDirectory, "conexion.json");
        using var json = JsonDocument.Parse(File.ReadAllText(archivo));
        string cadena = json.RootElement.GetProperty("ConnectionString").GetString()
            ?? throw new InvalidOperationException("Falta ConnectionString.");
        var builder = new SqlConnectionStringBuilder(cadena);
        if (!builder.IntegratedSecurity) throw new InvalidOperationException("Use autenticacion integrada de Windows.");
        return builder.ConnectionString;
    }
}
