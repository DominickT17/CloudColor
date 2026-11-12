using System.Transactions;
using CloudColor.Entidades;
using CloudColor.Negocio;
using Microsoft.Data.SqlClient;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InitializationException($"Falta la variable {name}.");

static Account ReadAccount(string prefix, string role) => new(
    role,
    Validacion.Texto(Required($"CLOUDCOLOR_{prefix}_CODIGO"), "Codigo", 30, true)!,
    Validacion.Texto(Required($"CLOUDCOLOR_{prefix}_NOMBRES"), "Nombres", 100, true)!,
    Validacion.Texto(Required($"CLOUDCOLOR_{prefix}_APELLIDOS"), "Apellidos", 100, true)!,
    Validacion.Texto(Required($"CLOUDCOLOR_{prefix}_USUARIO"), "Usuario", 50, true)!,
    Required($"CLOUDCOLOR_{prefix}_PASSWORD"));

static bool Same(string left, string right) =>
    string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

try
{
    string connectionString = Required("ConnectionStrings__CloudColor");
    Account manager = ReadAccount("GERENTE", "Gerente");
    Account digitador = ReadAccount("DIGITADOR", "Digitador");
    Account cajero = ReadAccount("CAJERO", "Cajero");
    Account[] accounts = [manager, digitador, cajero];

    if (accounts.Select(a => a.Username).Distinct(StringComparer.OrdinalIgnoreCase).Count() != accounts.Length ||
        accounts.Select(a => a.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != accounts.Length)
        throw new InitializationException("Los nombres de usuario y codigos de empleado deben ser distintos entre las tres cuentas.");

    var results = new List<string>();
    // Una sola conexion de Aplicacion participa en la transaccion: si falla cualquier
    // cuenta, tambien se revierten los empleados y la configuracion inicial.
    using (var scope = new TransactionScope(TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
    {
        using (var app = new Aplicacion(connectionString))
        {
            if (app.Seguridad.InicialPendiente())
            {
                app.Seguridad.Configurar(manager.Code, manager.Names, manager.Surnames, manager.Username, manager.Password);
                results.Add("Gerente creado.");
            }
            else
            {
                results.Add("Gerente existente; sin cambios.");
            }

            Sesion managerSession;
            try
            {
                managerSession = app.Seguridad.Login(manager.Username, manager.Password);
            }
            catch (ArgumentException)
            {
                throw new InitializationException("No se pudo autenticar el Gerente configurado. Revise su estado y las variables correspondientes.");
            }
            if (managerSession.Rol != "Gerente")
                throw new InitializationException("La cuenta configurada como Gerente tiene un rol diferente.");

            List<Rol> roles = app.Seguridad.Roles();
            List<Usuario> users = app.Seguridad.Usuarios();
            List<Empleado> employees = app.Empleados.Listar();

            foreach (Account account in accounts)
            {
                Rol? role = roles.SingleOrDefault(r => r.Nombre == account.Role && r.Activo);
                if (role is null)
                    throw new InitializationException($"El rol {account.Role} no existe o esta inactivo.");

                Usuario? user = users.SingleOrDefault(u => Same(u.NombreUsuario, account.Username));
                Empleado? employee = employees.SingleOrDefault(e => Same(e.CodigoEmpleado, account.Code));
                if (user is not null)
                {
                    if (!user.Activo || user.RolID != role.RolID || employee is null ||
                        !employee.Activo || user.EmpleadoID != employee.EmpleadoID)
                        throw new InitializationException($"La cuenta {account.Role} existente tiene rol, empleado o estado incompatible; revise la base.");
                    if (account != manager) results.Add($"{account.Role} existente; sin cambios.");
                    continue;
                }

                if (account == manager)
                    throw new InitializationException("No se encontro la cuenta del Gerente autenticado en el listado de usuarios.");
                if (employee is not null)
                    throw new InitializationException($"El codigo de empleado de {account.Role} ya esta en uso; revise la base.");

                int employeeId = app.Empleados.Guardar(new Empleado
                {
                    CodigoEmpleado = account.Code,
                    Nombres = account.Names,
                    Apellidos = account.Surnames
                });
                app.Seguridad.GuardarUsuario(new Usuario
                {
                    EmpleadoID = employeeId,
                    RolID = role.RolID,
                    NombreUsuario = account.Username
                }, account.Password);
                results.Add($"{account.Role} creado.");
            }
        }
        scope.Complete();
    }
    foreach (string result in results) Console.WriteLine(result);
    return 0;
}
catch (InitializationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or SqlException or TransactionException)
{
    // Los detalles de proveedor o validacion no deben exponer valores del entorno.
    Console.Error.WriteLine("No se pudieron preparar las cuentas. Revise los datos, la conexion y el estado de la base; no se confirmaron cambios de esta ejecucion.");
    return 1;
}

sealed record Account(string Role, string Code, string Names, string Surnames, string Username, string Password);
sealed class InitializationException(string message) : Exception(message);
