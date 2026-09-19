# Estado de Fase I

Validación local: 19 de septiembre de 2026. Entorno: SQL Server 2022 Express, .NET SDK 9.0.318, Windows Desktop 9.0.20.

## Entregables

- Normalización aplicada a las 31 tablas, con ejemplos de 1FN, 2FN, 3FN, tablas puente, encabezados/detalles e historia.
- 33 procedimientos parametrizados, una función escalar y dos vistas.
- Cuatro capas en C# y formularios de configuración inicial, login, menú, Cliente, Proveedor, Producto, Empleado, Usuarios, Roles y alta auxiliar de Marca.
- Pruebas SQL y ejecutable de integración sin framework de pruebas adicional.
- Modelo, tablas, columnas, PK, FK e índice filtrado originales conservados.

## Procedimientos

- `dbo.sp_AutorizarFase1`
- `dbo.sp_ClienteInsertar`
- `dbo.sp_ClienteModificar`
- `dbo.sp_ClienteListar`
- `dbo.sp_ClienteCambiarActivo`
- `dbo.sp_ProveedorInsertar`
- `dbo.sp_ProveedorModificar`
- `dbo.sp_ProveedorListar`
- `dbo.sp_ProveedorCambiarActivo`
- `dbo.sp_ProductoInsertar`
- `dbo.sp_ProductoModificar`
- `dbo.sp_ProductoListar`
- `dbo.sp_ProductoCambiarActivo`
- `dbo.sp_EmpleadoInsertar`
- `dbo.sp_EmpleadoModificar`
- `dbo.sp_EmpleadoListar`
- `dbo.sp_EmpleadoCambiarActivo`
- `dbo.sp_MarcaListar`
- `dbo.sp_CategoriaProductoListar`
- `dbo.sp_PresentacionListar`
- `dbo.sp_ColorListar`
- `dbo.sp_MarcaInsertar`
- `dbo.sp_RolListar`
- `dbo.sp_RolModificar`
- `dbo.sp_UsuarioListar`
- `dbo.sp_UsuarioObtenerPorNombreUsuario`
- `dbo.sp_UsuarioSesion`
- `dbo.sp_UsuarioCrear`
- `dbo.sp_UsuarioModificar`
- `dbo.sp_UsuarioCambiarRol`
- `dbo.sp_UsuarioCambiarActivo`
- `dbo.sp_ConfiguracionInicialPendiente`
- `dbo.sp_ConfiguracionInicial`

Todos los mantenimientos usan Activo para la baja lógica. No hay DELETE físico en los procedimientos. El procedimiento de autorización comprueba usuario, empleado y rol activos. La primera configuración y las modificaciones compuestas de usuario son transaccionales.

## Función y vistas

- `dbo.fn_EmpleadoNombreCompleto(@EmpleadoID)`: devuelve los nombres y apellidos del empleado; devuelve NULL si no existe. Se utiliza en la vista de usuarios.
- `dbo.vw_UsuariosRoles`: usuario, empleado, rol y estados, sin PasswordHash.
- `dbo.vw_ProductosDetalle`: producto con nombres de categoría y marca.

Los listados operativos de la aplicación se ejecutan mediante procedimientos. Las vistas están disponibles para consultas académicas y comprobaciones en SSMS, sin agregar módulos de reportes a la UI.

## Evidencia de validación

- Scripts 03, 04 y 05 ejecutados correctamente en CloudColorDB.
- Prueba SQL 02: inserción, consulta parametrizada, modificación, baja/alta lógica de los cuatro mantenimientos; administración de usuarios y roles; función y vistas; rechazo de Cajero y marca inexistente. ROLLBACK al terminar.
- Suite C#: 42 comprobaciones correctas, incluyendo creación del primer Gerente desde el formulario, login válido e inválido, mantenimiento desde botones y controles, usuarios/roles, hash con salt distinto, formato inválido, roles, cuenta/empleado inactivos y revalidación de una sesión abierta.
- Un segundo intento de configuración inicial es rechazado. La serialización del bootstrap está implementada mediante bloqueo transaccional; la suite verifica intentos secuenciales, no una carrera simultánea de dos procesos.
- Compilación Release sin errores ni advertencias.
- Revisión visual de formularios renderizados; corregida alineación del editor y selección de la fila a modificar.
- Auditoría NuGet de dependencias directas y transitivas: sin vulnerabilidades reportadas por los orígenes consultados.
- Al terminar las pruebas: 31 tablas y 20 filas totales, sin usuarios ni datos comerciales de prueba. Las pruebas no restituyen los valores IDENTITY consumidos.

## Permisos

| Perfil | Operaciones |
|---|---|
| Digitador | Cliente, Proveedor, Producto y Empleado; consulta de catálogos y alta auxiliar de marca |
| Gerente | Lo anterior y administración de Usuario/Rol |
| Cajero | Login y menú; cobros fuera de Fase I |

La UI oculta datos técnicos innecesarios y nunca muestra PasswordHash. Negocio y los procedimientos comprueban permisos: no se depende solamente de botones deshabilitados. Estos perfiles no reemplazan los privilegios de la cuenta Windows conectada a SQL Server.

## Decisiones

1. Autenticación: PBKDF2-HMAC-SHA256 en Negocio, 600 000 iteraciones, salt aleatorio de 16 bytes, hash de 32 bytes, comparación de tiempo constante y formato autocontenido en PasswordHash. Se sigue la instrucción técnica confirmada; no se implementa cifrado de contraseñas en SQL Server. La diferencia con la frase literal del enunciado requiere explicación académica.
2. Roles: los nombres son fijos; se editan descripción y estado. Gerente no se desactiva. Desde administración no se modifica la propia cuenta; otro Gerente puede efectuar esos cambios. Un empleado con cuenta Gerente activa no se desactiva directamente.
3. Catálogos: se permite capturar una marca real desde Producto para que su FK pueda completarse sin datos ficticios. No se agregan tablas ni un quinto mantenimiento principal.
4. Precios: pertenecen a ProductoVariante según el DBML; no se inventa un campo precio en Producto. El mantenimiento de variantes no forma parte de esta entrega.
5. Instalación: no se instaló un SDK o software global para el proyecto; se usaron las herramientas existentes y paquetes NuGet locales.
6. Git: main se creó desde la etapa inicial terminada porque no existía. Se conservó feat/database-schema. El trabajo nuevo permanece en feat/fase1; no se integra a main hasta revisión.

## Cobertura académica y pendientes

| Requisito de Fase I | Estado |
|---|---|
| Modelo relacional e implementación | 31 tablas conservadas y verificadas |
| Justificación de 3FN | Documento aplicado al modelo, con límites y redundancias históricas explicadas |
| Login y protección de contraseña | Implementados según la decisión PBKDF2 en C#; pendiente validar la interpretación académica de cifrado desde la base |
| Administración de usuarios y roles | Formularios y procedimientos implementados |
| Cuatro mantenimientos | Cliente, Proveedor, Producto y Empleado implementados |
| Procedimientos, función y vistas | Instalados y probados |
| Parámetros y arquitectura multicapa | Implementados y compilados |
| Revisión y presentación | Pendientes de revisión del equipo/docente y demostración final |

El primer usuario real debe ser creado por la persona responsable desde la aplicación; no se dejó una cuenta de prueba. No se han implementado compras, facturación, cobros, impresión, reportes, devoluciones, anulaciones o PEPS operativo de fases posteriores.
