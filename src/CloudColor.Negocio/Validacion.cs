namespace CloudColor.Negocio;
public static class Validacion
{
    public static string? Texto(string? valor, string campo, int maximo, bool obligatorio)
    {
        valor = valor?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            if (obligatorio) throw new ArgumentException($"{campo} es obligatorio.");
            return null;
        }
        if (valor.Length > maximo) throw new ArgumentException($"{campo} admite hasta {maximo} caracteres.");
        return valor;
    }
}
