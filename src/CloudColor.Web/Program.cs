using System.Globalization;
using CloudColor.Negocio;
using CloudColor.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

var builder=WebApplication.CreateBuilder(args);
var sqlKeys=builder.Configuration.GetValue<bool>("CloudColor:UseSqlDataProtection");
if (sqlKeys)
{
    var cadena=builder.Configuration.GetConnectionString("CloudColor")
        ?? throw new InvalidOperationException("Falta la conexion CloudColor.");
    builder.Services.AddDataProtection().SetApplicationName("CloudColor")
        .AddKeyManagementOptions(o=>o.XmlRepository=new SqlXmlRepository(cadena));
}
builder.Services.AddHealthChecks().AddCheck<SqlReadyHealthCheck>("sql", tags: ["ready"]);
builder.Services.Configure<ForwardedHeadersOptions>(o=>
{
    o.ForwardedHeaders=ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var networks=builder.Configuration["CloudColor:TrustedProxyNetworks"] ?? "";
    foreach(var value in networks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var network=System.Net.IPNetwork.Parse(value);
        o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(network.BaseAddress, network.PrefixLength));
    }
});
builder.Services.AddControllersWithViews(o=>o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddScoped<Aplicacion>(_=>new Aplicacion(builder.Configuration.GetConnectionString("CloudColor")));
builder.Services.AddScoped<SesionCookie>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>
{
    o.LoginPath="/Login"; o.AccessDeniedPath="/AccesoDenegado";
    o.Cookie.Name="CloudColor.Session"; o.Cookie.HttpOnly=true; o.Cookie.SameSite=SameSiteMode.Lax;
    o.Cookie.SecurePolicy=builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan=TimeSpan.FromMinutes(30); o.SlidingExpiration=false; o.EventsType=typeof(SesionCookie);
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(o=>o.Cookie.SameSite=SameSiteMode.Strict);
var app=builder.Build();
app.UseForwardedHeaders();
app.MapWhen(ctx=>ctx.Request.Path == "/health/live" || ctx.Request.Path == "/health/ready", health=>
{
    health.UseRouting();
    health.UseEndpoints(endpoints=>
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _=>false }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check=>check.Tags.Contains("ready") }).AllowAnonymous();
    });
});
app.UseExceptionHandler("/Error");
if(!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async(ctx,next)=>
{
    ctx.Response.Headers["X-Content-Type-Options"]="nosniff";
    ctx.Response.Headers["Referrer-Policy"]="same-origin";
    ctx.Response.Headers["Content-Security-Policy"]="default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    ctx.Response.Headers.CacheControl="no-store";
    await next();
});
app.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("es-GT").AddSupportedCultures("es-GT").AddSupportedUICultures("es-GT"));
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }
