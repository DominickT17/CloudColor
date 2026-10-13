using CloudColor.Negocio;
using CloudColor.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CloudColor.Web.Controllers;
public sealed class CuentaController(Aplicacion app) : Controller
{
    [AllowAnonymous,HttpGet("/Login")]
    public IActionResult Login()=>User.Identity?.IsAuthenticated==true ? Redirect("/Dashboard") : View();
    [AllowAnonymous,HttpPost("/Login")]
    public async Task<IActionResult> Login(string usuario,string password)
    {
        try { var s=app.Seguridad.Login(usuario??"",password??""); await HttpContext.SignInAsync(SesionCookie.Principal(s)); return Redirect("/Dashboard"); }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException) { ViewBag.Error="No pudimos iniciar sesión. Revisa tus credenciales y que tu cuenta esté activa."; return View(); }
    }
    [Authorize,HttpPost("/Salir")]
    public async Task<IActionResult> Salir() { app.Seguridad.CerrarSesion(); await HttpContext.SignOutAsync(); return Redirect("/Login"); }
    [AllowAnonymous,HttpGet("/")] public IActionResult Inicio()=>Redirect("/Dashboard");
    [AllowAnonymous,Route("/Error")] public IActionResult Error() { Response.StatusCode=500; return View("Mensaje","No pudimos completar la operación. Intenta nuevamente o revisa la disponibilidad del servicio."); }
    [AllowAnonymous,HttpGet("/AccesoDenegado")] public IActionResult Denegado() { Response.StatusCode=403; return View("Mensaje","Tu perfil no tiene permiso para esta operación."); }
}
