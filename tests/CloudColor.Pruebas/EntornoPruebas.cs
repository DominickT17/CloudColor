using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

// Instala y prueba en una base desechable; la conexión configurada solo se lee.
internal sealed class EntornoPruebas : IDisposable
{
    private readonly string original;
    private readonly string nombre="CloudColor_QA_"+Guid.NewGuid().ToString("N");
    private readonly string master;
    private readonly Dictionary<string,Huella> antes;
    private bool creada;
    public string Cadena { get; }
    private sealed record Huella(long Filas,string Contenido);

    public EntornoPruebas(string conexion)
    {
        original=new SqlConnectionStringBuilder(conexion) { Pooling=false }.ConnectionString;
        master=new SqlConnectionStringBuilder(conexion) { InitialCatalog="master",Pooling=false }.ConnectionString;
        Cadena=new SqlConnectionStringBuilder(conexion) { InitialCatalog=nombre,Pooling=false }.ConnectionString;
        antes=LeerHuellas();
        try
        {
            using(var c=new SqlConnection(master))
            {
                c.Open(); using var cmd=new SqlCommand($"CREATE DATABASE [{nombre}]",c); cmd.ExecuteNonQuery(); creada=true;
            }
            var directorio=new DirectoryInfo(AppContext.BaseDirectory);
            while(directorio!=null && !Directory.Exists(Path.Combine(directorio.FullName,"database","scripts"))) directorio=directorio.Parent;
            if(directorio==null) throw new InvalidOperationException("No se encontro database/scripts.");
            using var db=new SqlConnection(Cadena); db.Open();
            foreach(string carpeta in new[] { "scripts","tests" })
            foreach(string archivo in Directory.GetFiles(Path.Combine(directorio.FullName,"database",carpeta),"*.sql").Order())
            {
                // Reemplazo solo en memoria: los scripts del repositorio siguen apuntando a CloudColorDB.
                string sql=File.ReadAllText(archivo).Replace("CloudColorDB",nombre,StringComparison.Ordinal);
                foreach(string lote in Regex.Split(sql,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
                {
                    if(string.IsNullOrWhiteSpace(lote)) continue;
                    using var cmd=new SqlCommand(lote,db) { CommandTimeout=60 }; cmd.ExecuteNonQuery();
                }
                Console.WriteLine("OK SQL aislado: "+Path.GetFileName(archivo));
            }
        }
        catch { Dispose(); throw; }
    }

    private Dictionary<string,Huella> LeerHuellas()
    {
        using var c=new SqlConnection(original); c.Open();
        var tablas=new List<string>();
        using(var cmd=new SqlCommand("SELECT QUOTENAME(SCHEMA_NAME(schema_id))+'.'+QUOTENAME(name) FROM sys.tables WHERE is_ms_shipped=0 ORDER BY schema_id,name",c))
        using(var r=cmd.ExecuteReader()) { while(r.Read()) tablas.Add(r.GetString(0)); }
        var resultado=new Dictionary<string,Huella>();
        foreach(string tabla in tablas)
        {
            // Todas las tablas aprobadas tienen PK identity en la primera columna.
            using var cmd=new SqlCommand("SELECT * FROM "+tabla+" ORDER BY 1",c);
            using var r=cmd.ExecuteReader(); using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long filas=0;
            while(r.Read())
            {
                var valores=new object[r.FieldCount]; r.GetValues(valores);
                hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(valores.Select(v=>v==DBNull.Value ? null : v).ToArray())); filas++;
            }
            resultado.Add(tabla,new Huella(filas,Convert.ToHexString(hash.GetHashAndReset())));
        }
        return resultado;
    }

    public void VerificarOriginal()
    {
        var despues=LeerHuellas();
        if(antes.Count!=despues.Count || antes.Any(p=>!despues.TryGetValue(p.Key,out var h) || p.Value!=h))
            throw new InvalidOperationException("Los datos de la base original cambiaron durante las pruebas; revisar actividad concurrente.");
        Console.WriteLine($"OK: base original intacta ({antes.Count} tablas, {antes.Values.Sum(h=>h.Filas)} filas; contenido comprobado sin mostrar datos).");
    }

    public void Dispose()
    {
        if(!creada) return;
        // nombre es privado y generado por esta instancia, nunca el nombre de la base configurada.
        using var c=new SqlConnection(master); c.Open();
        using var cmd=new SqlCommand($"DROP DATABASE [{nombre}]",c); cmd.ExecuteNonQuery(); creada=false;
        Console.WriteLine("OK: base temporal de pruebas eliminada.");
    }
}
