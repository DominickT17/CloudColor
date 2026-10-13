# CloudColor Web

Nueva presentación web sobre la Fase II, en `feat/web-ui`. La aplicación WinForms permanece en la solución. No se modificaron tablas, columnas, PK, FK, DBML ni objetos SQL.

## Tecnología y arquitectura

ASP.NET Core 9 MVC, Razor, HTML, CSS local y JavaScript local. No requiere Node para ejecutar la aplicación, servicios externos ni CDN. Web referencia únicamente Negocio y Entidades:

`CloudColor.Web → CloudColor.Negocio → CloudColor.Datos → SQL Server`

Datos conserva ADO.NET, Microsoft.Data.SqlClient, procedimientos y TVP existentes. Los controladores coordinan formularios y servicios; no contienen SQL. El único método agregado a Negocio es `RestaurarSesionValidada`, para recuperar por solicitud una identidad previamente autenticada mediante cookie protegida. Nunca recibe el actor desde un formulario. WinForms conserva su mecanismo de sesión.

## Ejecución local

Requisitos: SDK .NET 9, SQL Server Express y la base existente con Fase II instalada (scripts 00–11). No volver a ejecutar los scripts sobre una base existente para abrir la web.

```powershell
cd C:\Users\domin\Documents\CloudColor
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project .\src\CloudColor.Web
```

Abrir `https://localhost:7255` o `http://localhost:5255`. El perfil local habilita ambas direcciones; HTTPS requiere que el navegador confíe en el certificado de desarrollo. La confianza del certificado no se modifica automáticamente. HTTP se ofrece exclusivamente como alternativa de desarrollo local. Fuera de Development se requiere HTTPS y la cookie es Secure.

`appsettings.json` apunta a `.\SQLEXPRESS`, `CloudColorDB`, con autenticación integrada de Windows. La identidad que ejecuta el proceso necesita acceso a SQL Server. Para otra base, usar la variable `ConnectionStrings__CloudColor` antes de ejecutar. No publicar credenciales en archivos ni introducirlas en el navegador.

Iniciar sesión con una cuenta existente activa. Si una instalación completamente nueva aún no tiene cuentas, realizar la configuración inicial con WinForms; esta presentación no duplica el asistente de configuración inicial. No hay usuario o contraseña predeterminados.

## Autenticación y sesión

Se reutiliza `SeguridadServicio.Login`: PBKDF2-HMAC-SHA256, 600000 iteraciones, salt aleatorio y comparación de tiempo constante. La contraseña viaja desde el formulario al servidor web para validarse en Negocio; SQL recibe únicamente el nombre al consultar la credencial y conserva el verificador PasswordHash. En altas/cambios de contraseña recibe el verificador, nunca la contraseña original. Hash y salt no se envían al navegador ni se incluyen en la cookie.

Cookie protegida por ASP.NET Core Data Protection, HttpOnly, SameSite=Lax, vencimiento absoluto de 30 minutos, sin renovación deslizante. Cada solicitud autenticada revalida usuario, empleado y rol en SQL; desactivar una cuenta revoca su acceso en la siguiente solicitud y cambiar el rol actualiza permisos. Cerrar sesión elimina la cookie del navegador. Un cambio de contraseña no invalida por sí solo otras cookies ya emitidas antes de su vencimiento; no se añadió una columna de sello de seguridad al modelo.

Todos los POST usan antiforgery. Los permisos se aplican en navegación, acciones del servidor, Negocio y procedimientos. Razor codifica texto; los errores inesperados se presentan mediante un mensaje genérico. CSP restringe scripts y estilos al propio sitio. Los botones muestran procesamiento y evitan envíos repetidos mientras navega el formulario; las reglas definitivas permanecen en Negocio/SQL.

## Páginas y permisos

| Páginas | Digitador | Cajero | Gerente |
|---|---|---|---|
| Login, Dashboard, salir | Sí | Sí | Sí |
| Clientes, Proveedores, Productos, Variantes | Sí | No | Sí |
| Bodegas, Ubicaciones, ProveedorProducto, Inventario | Sí | No | Sí |
| Compras, Recepciones, Cotizaciones | Sí | No | Sí |
| Facturas: consultar e imprimir | Sí | Sí | Sí |
| Facturas: emitir directa o desde cotización | Sí | No | Sí |
| Cobros | No | Sí | Sí |
| Empleados | Sí | No | Sí |
| Usuarios, Roles | No | No | Sí |

Las rutas de listados corresponden a los nombres anteriores con `/` inicial. Los catálogos usan `/Editar/{id}` y POST `/Estado/{id}`. Documentos usan `/Nueva`, `/Ver/{id}`; recepciones `/Recepciones/Nueva/{ordenId}`; cobros `/Cobros/Registrar/{facturaId}`; impresión `/Facturas/Imprimir/{id}`.

Los catálogos permiten búsqueda, alta, edición y activación/desactivación según el servicio existente. Producto y ProductoVariante permanecen separados. Marca y Color son catálogos auxiliares accesibles desde sus listados. Los selectores muestran nombres, SKU y presentaciones; no requieren escribir IDs. Usuarios permite cambiar rol y contraseña; vacía al editar conserva la actual. Roles administra descripción/estado de Digitador, Cajero y Gerente; no admite crear, renombrar ni eliminar perfiles.

## Operaciones y reutilización

- Dashboard calcula indicadores y actividad reciente a partir de consultas existentes. Cajero ve únicamente información de facturas autorizada. Las existencias son una suma de unidades entre variantes; no representan valor monetario.
- Cotizaciones utiliza detalle tipo carrito, confirmación y factura posterior. Al facturar se recuperan el cliente y los precios históricos desde la cotización, sin confiar en campos ocultos enviados por el navegador.
- Compras carga únicamente variantes asociadas al proveedor. Su detalle muestra solicitado, recibido, pendiente y progreso. Recepciones selecciona ubicación agrupada por bodega, cantidad, costo real, lote proveedor y vencimiento; SQL impide exceder lo pendiente.
- Inventario muestra lotes, SKU, presentación, color, bodega, ubicación, fecha, disponible y costo. Filtros por texto, variante, bodega y estado. Stock bajo compara el total global de la variante con StockMinimo, aunque se filtre una bodega. Variantes sin ningún ingreso no tienen fila de lote; los lotes agotados sí muestran sin stock.
- Factura directa o desde cotización reutiliza transacciones y PEPS existentes. La web no selecciona manualmente los lotes de salida. Impresión usa una vista limpia y el diálogo del navegador, que permite guardar PDF.
- Cobros distribuye el importe entre los medios existentes, muestra total/pagado/pendiente y exige el cobro completo conforme a Fase II. SQL rechaza duplicados y referencias obligatorias ausentes.

No se agregaron SP, vistas, funciones, triggers ni TVP. Se reutilizan los objetos invocados por Seguridad, mantenimientos, Catalogos y Fase2Servicio. La función de nombre completo continúa utilizándose mediante la vista existente de usuarios.

## Diseño y accesibilidad

Fondo #F7F8FA, paneles blancos, texto #1F2937, secundarios #6B7280 y bordes #E5E7EB; acento violeta y variables CSS para el tema. Sidebar fija en escritorio y plegable en tablet/móvil. Tarjetas, formularios, tablas, estados vacíos, mensajes y confirmaciones comparten estilos locales.

Labels asociados, foco visible, enlace para saltar al contenido, navegación por teclado, Escape para cerrar navegación, confirmación con dialog nativo y estados con texto además de color. Las tablas anchas permiten desplazamiento interno sin ensanchar la página. Se respeta reducción de movimiento. La revisión realizada no sustituye una auditoría formal con lector de pantalla.

## Pruebas reproducibles

```powershell
dotnet build src/CloudColor.sln -c Release --nologo -warnaserror
dotnet run --project tests/CloudColor.Pruebas -c Release --no-build -- --concurrencia
dotnet run --project tests/CloudColor.Web.Pruebas -c Release --no-build
```

Ambas suites crean su propia `CloudColor_QA_<GUID>`, instalan 00–11 y ejecutan las pruebas SQL 01–03. Comparan la huella del contenido original antes/después sin mostrar datos sensibles y eliminan su base temporal al terminar. Se necesitan los permisos locales indicados en el README. No ejecutar escrituras manuales sobre CloudColorDB durante esta comparación. Una interrupción forzada puede impedir la limpieza: identificar la base QA exacta antes de eliminarla, nunca usar comodines.

La suite web inicia Kestrel en el puerto 5256, comprueba previamente que esté libre y prueba HTTP real: login/logout, CSRF, restricciones GET/POST, CRUD, usuario con contraseña conservada, roles inmutables, ventas, precios históricos, PEPS, compra 100/recepción 75+25, rechazo de 75+30, dos medios de pago, cobro repetido y revocación de cuenta.

Para la revisión de navegador, dejar la suite temporal abierta:

```powershell
dotnet run --project tests/CloudColor.Web.Pruebas -c Release --no-build -- --visual
```

En otra terminal, con Node y Microsoft Edge disponibles, instalar la dependencia de pruebas localmente (no es requisito del servidor web):

```powershell
npm install --prefix tests/CloudColor.Web.Browser
npm test --prefix tests/CloudColor.Web.Browser
```

El script de navegador está dirigido exclusivamente al puerto QA 5256 y usa cuentas efímeras de esa base. Realiza un recorrido operativo, genera PDF y captura nueve páginas en 1920×1080, 1440×900, 1366×768, 768×1024 y 390×844. Comprueba ausencia de overflow de página, errores JavaScript/consola y funcionamiento del menú móvil. Ejecutarlo una vez por base QA nueva. Las capturas quedan en `%TEMP%\CloudColor-Web-Capturas`.

Al terminar, crear el archivo `.stop` exacto que imprime la suite para cerrar Kestrel, verificar la base original y eliminar QA. La espera también finaliza a los 30 minutos. Detener el servidor de pruebas antes de recompilar para evitar bloqueo de DLL.

## Evidencias recomendadas y límites

Verificación del 12 de octubre de 2026: solución Release con 0 errores y 0 advertencias; 110 comprobaciones existentes de WinForms/Negocio/concurrencia y 119 comprobaciones HTTP web correctas. Las pruebas SQL 01–03 pasaron en las bases temporales. El recorrido de Edge pasó con 45 vistas responsive, sin errores JavaScript/consola, y generó la factura PDF. Las bases QA se eliminaron; las 31 tablas y 31 filas originales conservaron su huella de contenido. Esquema confirmado: 31 tablas, 182 columnas, 31 PK de tablas y 43 FK. El comando normal de ejecución inició ambos puertos locales y `/Login` devolvió HTTP 200.

Durante QA se corrigieron el valor del botón al deshabilitarlo durante un POST, el resumen de validación vacío que generaba un estilo inline incompatible con CSP, el foco/visibilidad del menú cerrado y la compresión de columnas de inventario en móvil. Las capturas revisadas incluyen formularios, listas, documentos y vistas móviles; la prueba automatizada complementa esa inspección visual.

Capturar Login, Dashboard de Gerente y Cajero, Cliente, Variante, Bodega/Ubicación, carrito de cotización, orden con 75/100 recibidos, recepción parcial, Inventario, factura desde cotización, distribución del cobro y factura imprimible. Añadir las vistas móviles de Dashboard e Inventario, y acceso denegado por perfil. Utilizar datos QA, evitando datos personales o contraseñas.

No se realizó merge ni despliegue público. Queda revisión humana de la experiencia visual y aprobación de integración. Para publicar fuera de localhost se necesita configurar alojamiento, certificado TLS confiable y protección/persistencia de claves Data Protection del entorno. Los listados actuales cargan los resultados de las consultas existentes; la paginación puede evaluarse con volúmenes reales mayores sin alterar el modelo.
