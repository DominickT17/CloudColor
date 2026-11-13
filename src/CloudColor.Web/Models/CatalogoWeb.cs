using System.Globalization;
using CloudColor.Negocio;
namespace CloudColor.Web.Models;
public sealed record Opcion(string Valor,string Texto);
public sealed record Campo(string Nombre,string Etiqueta,string Tipo="text",bool Requerido=false,int Maximo=250,List<Opcion>? Opciones=null);
public sealed class CatalogoWeb
{
    public required string Ruta { get; init; }
    public required string Titulo { get; init; }
    public required string Singular { get; init; }
    public required string Descripcion { get; init; }
    public required Type Tipo { get; init; }
    public required List<Campo> Campos { get; init; }
    public required Func<string?,List<object>> Listar { get; init; }
    public required Action<object,string?> Guardar { get; init; }
    public Action<int,bool>? Activar { get; init; }
    public bool Crear { get; init; }=true;
    public int ID(object o)=>(int)Tipo.GetProperty(Tipo.Name+"ID")!.GetValue(o)!;
    public bool Activo(object o)=>(bool)Tipo.GetProperty("Activo")!.GetValue(o)!;
    public string Valor(object o,string n)=>Convert.ToString(Tipo.GetProperty(n)?.GetValue(o),CultureInfo.InvariantCulture)??"";
    public string Mostrar(object o,Campo f)
    {
        string v=Valor(o,f.Nombre);
        if(f.Tipo=="checkbox") return v=="True" ? "Sí" : "No";
        if(f.Tipo is "money" or "quantity" && decimal.TryParse(v,NumberStyles.Number,CultureInfo.InvariantCulture,out var numero))
            return f.Tipo=="money" ? "Q "+numero.ToString("N2",CultureInfo.GetCultureInfo("es-GT")) : numero.ToString("0.####",CultureInfo.GetCultureInfo("es-GT"));
        return f.Opciones?.FirstOrDefault(x=>x.Valor==v)?.Texto ?? (v=="" ? "—" : v);
    }
}
public sealed record ListadoCatalogo(CatalogoWeb Catalogo,List<object> Filas,string? Buscar);
public sealed record EditorCatalogo(CatalogoWeb Catalogo,int Id,Dictionary<string,string> Valores,string? Buscar);
public sealed record Metrica(string Nombre,string Valor,string Nota,string Ruta);
public sealed record Actividad(string Tipo,string Numero,string Persona,string Estado,DateTime Fecha,string Ruta);
public sealed record Barra(string Nombre,int Cantidad,string Ruta);
public sealed record Grafico(string Titulo,string Nota,List<Barra> Barras);
public sealed record DashboardVm(List<Metrica> Metricas,List<Actividad> Actividad,List<Grafico> Graficos);
