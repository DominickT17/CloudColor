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
- Inventario muestra lotes, SKU, presentación, color, bodega, ubicación, fecha, disponible y costo. Filtros por texto, variante, bodega, ubicación y estado. Stock bajo compara el total global de la variante con StockMinimo, aunque se filtre una bodega. Variantes sin ningún ingreso aparecen en tarjetas separadas sin inventar lotes ni ubicaciones; se omiten al filtrar una bodega o ubicación porque todavía no pertenecen a ninguna.
- Factura directa o desde cotización reutiliza transacciones y PEPS existentes. La web no selecciona manualmente los lotes de salida. Impresión usa una vista limpia y el diálogo del navegador, que permite guardar PDF.
- Cobros distribuye el importe entre los medios existentes, muestra total/pagado/pendiente y exige el cobro completo conforme a Fase II. SQL rechaza duplicados y referencias obligatorias ausentes.

No se agregaron SP, vistas, funciones, triggers ni TVP. Se reutilizan los objetos invocados por Seguridad, mantenimientos, Catalogos y Fase2Servicio. La función de nombre completo continúa utilizándose mediante la vista existente de usuarios.

## Diseño y accesibilidad

Fondo #F7F8FA, paneles blancos, texto #1F2937, secundarios #6B7280 y bordes #E5E7EB; acento violeta y variables CSS para el tema. Sidebar fija en escritorio y plegable en tablet/móvil. Tarjetas, formularios, tablas, estados vacíos, mensajes y confirmaciones comparten estilos locales.

Labels asociados, foco visible, enlace para saltar al contenido, navegación por teclado, Escape para cerrar navegación, confirmación con dialog nativo y estados con texto además de color. Las tablas anchas permiten desplazamiento interno en escritorio y se convierten en tarjetas con etiquetas en móvil. Se respeta reducción de movimiento. La revisión realizada no sustituye una auditoría formal con lector de pantalla.

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

### Refinamiento de usabilidad

Las tablas de listados e inventario incorporan un filtro inmediato de los resultados cargados, selector de estado, contador visible, restablecimiento y ordenación por encabezados de texto o importes. La búsqueda original sigue consultando el servidor; el filtro inmediato solo reduce lo que ya se recibió. Las fechas mantienen el orden original para evitar ordenar cronología como texto. Estos controles no guardan cambios ni solicitan permisos adicionales.

En cotizaciones, facturas directas y compras se muestra el subtotal provisional al escribir cantidad y precio/costo. La línea solo se incorpora al pulsar Agregar; el total definitivo sigue calculándose en Negocio. Las contraseñas tienen Mostrar/Ocultar y aviso de Bloq Mayús; se ocultan nuevamente cuando el navegador restaura la página.

Prueba adicional, con la suite temporal `--visual` abierta: `npm run test:interactividad --prefix tests/CloudColor.Web.Browser`. Comprueba controles de contraseña, filtros con y sin coincidencias, orden ascendente/descendente, subtotal reactivo y 20 vistas en cinco tamaños. Las capturas quedan en `%TEMP%\CloudColor-Web-Interactividad`. Este recorrido no escribe registros de negocio.

Los catálogos muestran acciones específicas como Crear cliente, permiten limpiar la búsqueda y conservan el filtro al abrir el alta. Los importes muestran Q y dos decimales; las cantidades eliminan ceros decimales innecesarios sin cambiar los valores almacenados.

El menú móvil mantiene el foco dentro de sus enlaces, bloquea la interacción con el contenido cubierto y devuelve el foco al botón de apertura al cerrar con Escape. Al pasar a escritorio libera el contenido. La página actual se identifica también con aria-current.

Al restaurar una página desde el historial, los formularios recuperan sus botones y eliminan el estado de procesamiento y la copia temporal de la acción enviada. La prueba de navegador comprueba búsqueda, moneda, foco circular del menú y recuperación mediante el evento pageshow del navegador, además del recorrido operativo existente.

Verificación del 12 de octubre de 2026: solución Release con 0 errores y 0 advertencias; 110 comprobaciones existentes de WinForms/Negocio/concurrencia y 119 comprobaciones HTTP web correctas. Las pruebas SQL 01–03 pasaron en las bases temporales. El recorrido de Edge pasó con 45 vistas responsive, sin errores JavaScript/consola, y generó la factura PDF. Las bases QA se eliminaron; las 31 tablas y 31 filas originales conservaron su huella de contenido. Esquema confirmado: 31 tablas, 182 columnas, 31 PK de tablas y 43 FK. El comando normal de ejecución inició ambos puertos locales y `/Login` devolvió HTTP 200.

Durante QA se corrigieron el valor del botón al deshabilitarlo durante un POST, el resumen de validación vacío que generaba un estilo inline incompatible con CSP, el foco/visibilidad del menú cerrado y la compresión de columnas de inventario en móvil. Las capturas revisadas incluyen formularios, listas, documentos y vistas móviles; la prueba automatizada complementa esa inspección visual.

Capturar Login, Dashboard de Gerente y Cajero, Cliente, Variante, Bodega/Ubicación, carrito de cotización, orden con 75/100 recibidos, recepción parcial, Inventario, factura desde cotización, distribución del cobro y factura imprimible. Añadir las vistas móviles de Dashboard e Inventario, y acceso denegado por perfil. Utilizar datos QA, evitando datos personales o contraseñas.

No se realizó merge ni despliegue público. Queda revisión humana de la experiencia visual y aprobación de integración. Para publicar fuera de localhost se necesita configurar alojamiento, certificado TLS confiable y protección/persistencia de claves Data Protection del entorno. Los listados actuales cargan los resultados de las consultas existentes; la paginación puede evaluarse con volúmenes reales mayores sin alterar el modelo.

### Interacciones y operación reactiva — 13 de octubre de 2026

Los mensajes de éxito, información, advertencia y error utilizan notificaciones reutilizables, con cierre manual y pausa al recibir foco o pasar el cursor. Confirmar cotización, emitir factura, cobrar y registrar recepción usan el mismo diálogo; cancelar deja el formulario intacto. Los POST indican procesamiento y evitan envíos repetidos desde sus botones. Las comprobaciones definitivas permanecen en Negocio y SQL Server.

La cabecera ofrece búsqueda global al escribir, sin atajo de apertura y menú de sesión. La búsqueda requiere dos caracteres, limita cada grupo a cinco resultados y reutiliza los servicios existentes. Gerente y Digitador consultan clientes, productos, variantes, proveedores, cotizaciones, órdenes y facturas; Cajero solamente facturas. No devuelve contraseñas ni objetos de usuario. Es una búsqueda sobre los listados existentes, no un nuevo índice SQL. Alt+N abre el alta disponible fuera de los campos; Escape cierra diálogos y menú. Los listados incorporan vista rápida lateral con los valores de la fila y enlace al registro completo.

Los formularios detectan cambios y confirman antes de abandonar por un enlace o cerrar sesión. Recargar o cerrar la pestaña usa el aviso nativo obligatorio del navegador. No se guardan borradores ni contraseñas en localStorage. Los errores de campo se actualizan al escribir y al salir del control. Filtros del servidor y filtros locales muestran chips independientes; el usuario puede retirar cada condición o limpiar todas.

Dashboard reutiliza datos actuales para indicadores de stock bajo, sin stock, cotizaciones pendientes y facturas por cobrar. Las barras cuentan órdenes y facturas por estado y enlazan al listado filtrado. La actividad incluye documentos y recepciones reales; las recepciones se consultan en las ocho órdenes más recientes. La fecha del documento es su creación y el estado es el actual: no representa una bitácora de transiciones ni inventa fechas de confirmación/cobro.

Cotizaciones, facturas directas y compras agregan, quitan y editan líneas en memoria sin recargar. La búsqueda de variantes ignora acentos; SKU, presentación, color y disponibilidad ayudan a elegir. El resumen y subtotales son provisionales; Negocio recalcula al guardar y SQL vuelve a comprobar existencias al emitir. Facturar una cotización confirmada mantiene sus líneas y precios históricos sin permitir editarlos. El detalle no persiste hasta Guardar. Cambiar proveedor y cargar variantes conserva el comportamiento de reiniciar el detalle, ahora con confirmación explícita.

Compras muestra porcentaje recibido e historial con enlaces al detalle de cada recepción. Recepciones separa Bodega y Ubicación y anticipa si la cantidad completa o recibe parcialmente una línea. El POST conserva UbicacionID y las validaciones existentes. Inventario agrega resumen global, filtros rápidos Disponible/Stock bajo/Sin stock y contexto de filtros. Los totales de las tarjetas son globales; el contador de filas y unidades corresponde al resultado filtrado.

Cobros presenta detalle de factura, importes distribuidos y saldo pendiente. Permite editar monto/referencia y retirar medios antes de confirmar; detecta duplicados, referencias faltantes y excedentes. Confirmar se habilita al distribuir exactamente el pendiente. Una factura pagada muestra que ya no tiene saldo, deshabilita entradas y conserva el acceso al detalle de sus medios. No cambian las transacciones, PEPS, permisos ni reglas de cobro completo.

Las interacciones se organizan en `site.js` (navegación/tablas), `ux.js` (notificaciones, diálogos, búsqueda y formularios) y `cart.js` (documentos, cobros y recepción). Son JavaScript local sin frameworks, paquetes nuevos de aplicación ni servicios externos. Web continúa llamando a Negocio; no se incorporó SQL en controladores ni vistas.

Con la base temporal `--visual` abierta, ejecutar en orden:

```powershell
npm test --prefix tests/CloudColor.Web.Browser
npm run test:interactividad --prefix tests/CloudColor.Web.Browser
npm run test:ux --prefix tests/CloudColor.Web.Browser
```

El último recorrido comprueba búsqueda al escribir y navegación por teclado, cierre con Escape, vista rápida, cancelar/aceptar salida sin guardar, chips, carrito sin recarga, totales editables, notificaciones, cancelación de emisión, cobro con excedente y corrección, factura pagada y detalle de recepción. Genera 55 vistas en 1920×1080, 1440×900, 1366×768, 768×1024 y 390×844, además de cinco capturas operativas. Requiere los datos temporales preparados por el primer recorrido; no usar cuentas ni puertos de la base permanente.

Las capturas de esta revisión están en `%TEMP%\CloudColor-Web-UX\despues`. La carpeta hermana `antes` conserva cinco capturas reales de la versión anterior: Dashboard, Inventario, cotización, compra parcial y cobro. Los conjuntos de datos QA pueden diferir; sirven para comparar presentación, no cifras operativas. Son archivos locales temporales, no se incluyen datos de QA en Git.

Resultado del 13 de octubre de 2026: Release con `-warnaserror`, 0 errores y 0 advertencias. Pasaron 110 comprobaciones WinForms/Negocio/concurrencia, 127 comprobaciones HTTP web y las pruebas SQL 01–03 en bases temporales. En Edge pasaron los recorridos de 45, 20 y 55 vistas responsive (120 verificaciones de página en total), sin errores JavaScript/consola, además del PDF. Se inspeccionaron capturas de escritorio y móvil. Las bases QA se eliminaron y la huella de las 31 tablas y 31 filas originales permaneció intacta.

En esta revisión se corrigieron el cierre con Escape del buscador, el desplazamiento de botones causado por validación tardía y el mensaje de saldo de una factura ya pagada. La prueba UX cubre estas interacciones. No se modificaron DBML, tablas, claves, procedimientos, WinForms, PBKDF2 ni capas de negocio/datos. Quedan la revisión humana de las capturas y una auditoría específica con lector de pantalla; la publicación externa y el merge requieren aprobación independiente.

### Pulido de búsqueda y microinteracciones

La búsqueda ahora es un campo directamente en la cabecera. Al escribir dos caracteres consulta tras 300 ms, cancela solicitudes anteriores y muestra un panel debajo con grupos, enlaces y descripción de módulo. Limpiar borra el texto, cierra resultados y mantiene el foco; Escape cierra el panel, las flechas recorren enlaces y Enter abre el resultado enfocado. Clic fuera o abandonar el campo y sus resultados cierra el panel. Se eliminó el atajo de apertura y su indicación visual. Los permisos y el límite de cinco resultados por grupo se conservan.

Se mantiene el hero y el orden de la navegación. Las tarjetas completas siguen siendo enlaces y ganan elevación de 2 px; los números positivos del Dashboard se animan durante 400 ms, manteniendo su valor real como etiqueta accesible. La flecha del hero responde al hover y las barras tienen una entrada única. Sidebar conserva categorías y ofrece indicador activo y scroll fino. La fecha utiliza el formato español «13 de octubre de 2026».

Inventario marca las tarjetas de Stock bajo/Sin stock seleccionadas; volver a pulsarlas retira el estado sin borrar los demás filtros. Las filas con registro asociado permiten abrirlo al pulsar una celda, sin interferir con botones, formularios o selección de texto. Los filtros locales destacan brevemente el resultado sin nuevas consultas por tecla.

El carrito resalta líneas nuevas y totales editados; quitar una línea usa una transición de 140 ms. La recepción anima el progreso desde el último valor visto por pestaña (sessionStorage guarda solo ese número, no borradores ni datos personales) y destaca cuando llega al total. Facturas muestra saldo pendiente explícito. Cobros conserva la validación del servidor y aclara los tres estados de distribución.

Notificaciones cierran aproximadamente a los cuatro segundos, con pausa de foco/hover y cierre manual. Los diálogos usan el elemento nativo y un recorrido circular explícito de Tab, cancelación con Escape y retorno al control original; se añade entrada suave. El menú de sesión cierra por clic fuera o Escape. Los controles deshabilitados tienen cursor y apariencia coherentes. Las nuevas animaciones respetan `prefers-reduced-motion`.

La prueba `ux.cjs` también verifica ausencia de apertura por el antiguo atajo, búsqueda sin resultados, limpiar y conservar foco, navegación con flechas, menú de sesión, fecha, filtros de tarjetas, foco dentro y después del modal, filas clicables y movimiento reducido. Captura el panel de búsqueda en los cinco tamaños, además de las páginas existentes. No agrega dependencias, SQL ni cambios de reglas de negocio.

Validación de este pulido: build Release con 0 errores y 0 advertencias; 110 comprobaciones existentes y 127 verificaciones HTTP web correctas, incluidas pruebas SQL en bases temporales. Edge completó 45 + 20 + 55 vistas responsive y cinco capturas adicionales del buscador. También se comprobó el autocierre de toast y se capturaron perfil y modal. Los tamaños fueron 1920×1080, 1440×900, 1366×768, 768×1024 y 390×844. El recorrido circular de Tab se reforzó tras detectar que el foco podía salir del diálogo. Las capturas del Dashboard esperan el valor final de los números. Se eliminaron las bases temporales y las 31 tablas/31 filas originales conservaron su huella. Sin cambios de modelo, WinForms o capas de negocio/datos.
