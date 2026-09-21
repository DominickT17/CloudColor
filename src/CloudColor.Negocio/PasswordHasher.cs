using System.Security.Cryptography;
namespace CloudColor.Negocio;
public static class PasswordHasher
{
    public const int Iteraciones = 600_000;
    public static string Crear(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
            throw new ArgumentException("Use una contraseña de entre 12 y 128 caracteres.");
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2$SHA256${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public static bool Verificar(string password, string almacenado)
    {
        if (password.Length > 128 || almacenado.Length > 255) return false;
        string[] partes = almacenado.Split('$');
        if (partes.Length != 5 || partes[0] != "PBKDF2" || partes[1] != "SHA256" ||
            !int.TryParse(partes[2], out int iteraciones) || iteraciones < 600_000 || iteraciones > 2_000_000) return false;
        try
        {
            byte[] salt = Convert.FromBase64String(partes[3]);
            byte[] esperado = Convert.FromBase64String(partes[4]);
            if (salt.Length != 16 || esperado.Length != 32) return false;
            byte[] obtenido = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, 32);
            bool valido = CryptographicOperations.FixedTimeEquals(obtenido, esperado);
            CryptographicOperations.ZeroMemory(obtenido);
            return valido;
        }
        catch (FormatException) { return false; }
    }
}
