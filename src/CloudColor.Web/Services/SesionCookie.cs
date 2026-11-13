using System.Security.Claims;
using CloudColor.Entidades;
using CloudColor.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace CloudColor.Web.Services;
public sealed class SesionCookie(Aplicacion app) : CookieAuthenticationEvents
{
    public static ClaimsPrincipal Principal(Sesion s)=>new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier,s.UsuarioID.ToString()),new Claim(ClaimTypes.Name,s.NombreUsuario),new Claim(ClaimTypes.Role,s.Rol)
    ],CookieAuthenticationDefaults.AuthenticationScheme));
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        try
        {
            if(!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out int id)) throw new InvalidOperationException();
            var sesion=app.Seguridad.RestaurarSesionValidada(id);
            context.ReplacePrincipal(Principal(sesion));
        }
        catch(InvalidOperationException)
        {
            context.RejectPrincipal(); await context.HttpContext.SignOutAsync();
        }
    }
}
