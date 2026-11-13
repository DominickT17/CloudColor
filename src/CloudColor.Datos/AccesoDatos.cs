using System.Data;
using Microsoft.Data.SqlClient;
namespace CloudColor.Datos;

// Una conexion por instancia. Solo esta capa conoce SqlClient.
public sealed class AccesoDatos : IDisposable
{
    private readonly SqlConnection conexion;
    private SqlTransaction? transaccion;
    public AccesoDatos(string cadena) { conexion = new SqlConnection(cadena); conexion.Open(); }
    public static SqlParameter Texto(string nombre, int longitud, string? valor) => new(nombre, SqlDbType.VarChar, longitud) { Value = (object?)valor ?? DBNull.Value };
    public static SqlParameter Entero(string nombre, int valor) => new(nombre, SqlDbType.Int) { Value = valor };
    public static SqlParameter Bit(string nombre, bool? valor) => new(nombre, SqlDbType.Bit) { Value = (object?)valor ?? DBNull.Value };
    private SqlCommand Comando(string procedimiento, SqlParameter[] parametros)
    {
        var cmd = new SqlCommand(procedimiento, conexion, transaccion) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(parametros);
        return cmd;
    }
    public List<T> Consultar<T>(string procedimiento, Func<SqlDataReader, T> convertir, params SqlParameter[] parametros)
    {
        try
        {
            using var cmd = Comando(procedimiento, parametros);
            using var reader = cmd.ExecuteReader();
            var lista = new List<T>();
            while (reader.Read()) lista.Add(convertir(reader));
            return lista;
        }
        catch (SqlException ex) { throw ErrorSeguro(ex); }
    }
    public int Escalar(string procedimiento, params SqlParameter[] parametros)
    {
        try { using var cmd = Comando(procedimiento, parametros); return Convert.ToInt32(cmd.ExecuteScalar()); }
        catch (SqlException ex) { throw ErrorSeguro(ex); }
    }
    public void Ejecutar(string procedimiento, params SqlParameter[] parametros)
    {
        try { using var cmd = Comando(procedimiento, parametros); cmd.ExecuteNonQuery(); }
        catch (SqlException ex) { throw ErrorSeguro(ex); }
    }
    public void EnTransaccion(Action accion)
    {
        if (transaccion != null) { accion(); return; }
        using var tx = conexion.BeginTransaction(); transaccion = tx;
        try { accion(); tx.Commit(); }
        catch { try { tx.Rollback(); } catch (InvalidOperationException) { } throw; }
        finally { transaccion = null; }
    }
    private static Exception ErrorSeguro(SqlException ex) => new InvalidOperationException(ex.Number switch
    {
        2601 or 2627 => "Ya existe un registro con ese codigo, nombre de usuario o empleado asignado.",
        547 => "La operacion no respeta una relacion o restriccion de la base de datos.",
        >= 52000 and <= 52199 => ex.Message,
        _ => "No se pudo completar la operacion en SQL Server. Revise la conexion y los scripts instalados."
    }, ex);
    public void Dispose() => conexion.Dispose();
}
