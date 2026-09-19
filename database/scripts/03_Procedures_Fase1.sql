-- Instalar una vez despues de 02_InitialData.sql. No modifica tablas.
USE CloudColorDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE dbo.sp_AutorizarFase1
    @UsuarioActorID int,
    @SoloGerente bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        JOIN dbo.Rol r ON r.RolID = u.RolID
        JOIN dbo.Empleado e ON e.EmpleadoID = u.EmpleadoID
        WHERE u.UsuarioID = @UsuarioActorID AND u.Activo = 1 AND e.Activo = 1 AND r.Activo = 1
          AND (r.Nombre = 'Gerente' OR (@SoloGerente = 0 AND r.Nombre = 'Digitador')))
        THROW 52001, 'Su cuenta no tiene permiso para esta operacion.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ClienteInsertar
    @UsuarioActorID int,
    @TipoCliente varchar(20),
    @NombreRazonSocial varchar(180),
    @NIT varchar(30) = NULL,
    @Telefono varchar(30) = NULL,
    @Correo varchar(150) = NULL,
    @Direccion varchar(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@TipoCliente)), '') IS NULL THROW 52002, 'TipoCliente es obligatorio.', 1;
    IF NULLIF(LTRIM(RTRIM(@NombreRazonSocial)), '') IS NULL THROW 52002, 'NombreRazonSocial es obligatorio.', 1;
    IF @TipoCliente NOT IN ('Particular', 'Empresa') THROW 52002, 'Tipo de cliente invalido.', 1;
    INSERT INTO dbo.Cliente (TipoCliente, NombreRazonSocial, NIT, Telefono, Correo, Direccion, Activo) VALUES (@TipoCliente, @NombreRazonSocial, @NIT, @Telefono, @Correo, @Direccion, 1);
    SELECT CONVERT(int, SCOPE_IDENTITY()) AS ClienteID;
END;
GO

CREATE PROCEDURE dbo.sp_ClienteModificar
    @UsuarioActorID int,
    @ClienteID int,
    @TipoCliente varchar(20),
    @NombreRazonSocial varchar(180),
    @NIT varchar(30) = NULL,
    @Telefono varchar(30) = NULL,
    @Correo varchar(150) = NULL,
    @Direccion varchar(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@TipoCliente)), '') IS NULL THROW 52002, 'TipoCliente es obligatorio.', 1;
    IF NULLIF(LTRIM(RTRIM(@NombreRazonSocial)), '') IS NULL THROW 52002, 'NombreRazonSocial es obligatorio.', 1;
    IF @TipoCliente NOT IN ('Particular', 'Empresa') THROW 52002, 'Tipo de cliente invalido.', 1;
    UPDATE dbo.Cliente SET TipoCliente = @TipoCliente, NombreRazonSocial = @NombreRazonSocial, NIT = @NIT, Telefono = @Telefono, Correo = @Correo, Direccion = @Direccion WHERE ClienteID = @ClienteID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ClienteListar
    @UsuarioActorID int,
    @Buscar varchar(180) = NULL,
    @Activo bit = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT ClienteID, TipoCliente, NombreRazonSocial, NIT, Telefono, Correo, Direccion, Activo FROM dbo.Cliente
    WHERE (@Buscar IS NULL OR NombreRazonSocial LIKE '%' + @Buscar + '%') AND (@Activo IS NULL OR Activo = @Activo)
    ORDER BY NombreRazonSocial, ClienteID;
END;
GO

CREATE PROCEDURE dbo.sp_ClienteCambiarActivo
    @UsuarioActorID int,
    @ClienteID int,
    @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    UPDATE dbo.Cliente SET Activo = @Activo WHERE ClienteID = @ClienteID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ProveedorInsertar
    @UsuarioActorID int,
    @Nombre varchar(180),
    @NIT varchar(30) = NULL,
    @Telefono varchar(30) = NULL,
    @Correo varchar(150) = NULL,
    @Direccion varchar(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@Nombre)), '') IS NULL THROW 52002, 'Nombre es obligatorio.', 1;
    INSERT INTO dbo.Proveedor (Nombre, NIT, Telefono, Correo, Direccion, Activo) VALUES (@Nombre, @NIT, @Telefono, @Correo, @Direccion, 1);
    SELECT CONVERT(int, SCOPE_IDENTITY()) AS ProveedorID;
END;
GO

CREATE PROCEDURE dbo.sp_ProveedorModificar
    @UsuarioActorID int,
    @ProveedorID int,
    @Nombre varchar(180),
    @NIT varchar(30) = NULL,
    @Telefono varchar(30) = NULL,
    @Correo varchar(150) = NULL,
    @Direccion varchar(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@Nombre)), '') IS NULL THROW 52002, 'Nombre es obligatorio.', 1;
    UPDATE dbo.Proveedor SET Nombre = @Nombre, NIT = @NIT, Telefono = @Telefono, Correo = @Correo, Direccion = @Direccion WHERE ProveedorID = @ProveedorID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ProveedorListar
    @UsuarioActorID int,
    @Buscar varchar(180) = NULL,
    @Activo bit = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT ProveedorID, Nombre, NIT, Telefono, Correo, Direccion, Activo FROM dbo.Proveedor
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%') AND (@Activo IS NULL OR Activo = @Activo)
    ORDER BY Nombre, ProveedorID;
END;
GO

CREATE PROCEDURE dbo.sp_ProveedorCambiarActivo
    @UsuarioActorID int,
    @ProveedorID int,
    @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    UPDATE dbo.Proveedor SET Activo = @Activo WHERE ProveedorID = @ProveedorID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ProductoInsertar
    @UsuarioActorID int,
    @CategoriaProductoID int,
    @MarcaID int,
    @Nombre varchar(150),
    @Descripcion varchar(300) = NULL,
    @ManejaLote bit,
    @AplicaPEPS bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@Nombre)), '') IS NULL THROW 52002, 'Nombre es obligatorio.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE CategoriaProductoID = @CategoriaProductoID AND Activo = 1) THROW 52002, 'Seleccione una categoria activa.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Marca WHERE MarcaID = @MarcaID AND Activo = 1) THROW 52002, 'Seleccione una marca activa.', 1;
    INSERT INTO dbo.Producto (CategoriaProductoID, MarcaID, Nombre, Descripcion, ManejaLote, AplicaPEPS, Activo) VALUES (@CategoriaProductoID, @MarcaID, @Nombre, @Descripcion, @ManejaLote, @AplicaPEPS, 1);
    SELECT CONVERT(int, SCOPE_IDENTITY()) AS ProductoID;
END;
GO

CREATE PROCEDURE dbo.sp_ProductoModificar
    @UsuarioActorID int,
    @ProductoID int,
    @CategoriaProductoID int,
    @MarcaID int,
    @Nombre varchar(150),
    @Descripcion varchar(300) = NULL,
    @ManejaLote bit,
    @AplicaPEPS bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@Nombre)), '') IS NULL THROW 52002, 'Nombre es obligatorio.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE CategoriaProductoID = @CategoriaProductoID AND Activo = 1) THROW 52002, 'Seleccione una categoria activa.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Marca WHERE MarcaID = @MarcaID AND Activo = 1) THROW 52002, 'Seleccione una marca activa.', 1;
    UPDATE dbo.Producto SET CategoriaProductoID = @CategoriaProductoID, MarcaID = @MarcaID, Nombre = @Nombre, Descripcion = @Descripcion, ManejaLote = @ManejaLote, AplicaPEPS = @AplicaPEPS WHERE ProductoID = @ProductoID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ProductoListar
    @UsuarioActorID int,
    @Buscar varchar(180) = NULL,
    @Activo bit = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT ProductoID, CategoriaProductoID, MarcaID, Nombre, Descripcion, ManejaLote, AplicaPEPS, Activo FROM dbo.vw_ProductosDetalle
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%') AND (@Activo IS NULL OR Activo = @Activo)
    ORDER BY Nombre, ProductoID;
END;
GO

CREATE PROCEDURE dbo.sp_ProductoCambiarActivo
    @UsuarioActorID int,
    @ProductoID int,
    @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    UPDATE dbo.Producto SET Activo = @Activo WHERE ProductoID = @ProductoID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_EmpleadoInsertar
    @UsuarioActorID int,
    @CodigoEmpleado varchar(30),
    @Nombres varchar(100),
    @Apellidos varchar(100),
    @Telefono varchar(30) = NULL,
    @Correo varchar(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@CodigoEmpleado)), '') IS NULL THROW 52002, 'CodigoEmpleado es obligatorio.', 1;
    IF NULLIF(LTRIM(RTRIM(@Nombres)), '') IS NULL THROW 52002, 'Nombres es obligatorio.', 1;
    IF NULLIF(LTRIM(RTRIM(@Apellidos)), '') IS NULL THROW 52002, 'Apellidos es obligatorio.', 1;
    INSERT INTO dbo.Empleado (CodigoEmpleado, Nombres, Apellidos, Telefono, Correo, Activo) VALUES (@CodigoEmpleado, @Nombres, @Apellidos, @Telefono, @Correo, 1);
    SELECT CONVERT(int, SCOPE_IDENTITY()) AS EmpleadoID;
END;
GO

CREATE PROCEDURE dbo.sp_EmpleadoModificar
    @UsuarioActorID int,
    @EmpleadoID int,
    @CodigoEmpleado varchar(30),
    @Nombres varchar(100),
    @Apellidos varchar(100),
    @Telefono varchar(30) = NULL,
    @Correo varchar(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE EmpleadoID = @EmpleadoID AND UsuarioID = @UsuarioActorID)
        THROW 52003, 'Su propio empleado se conserva durante esta sesion; use otra cuenta autorizada.', 1;
    IF NULLIF(LTRIM(RTRIM(@CodigoEmpleado)), '') IS NULL THROW 52002, 'CodigoEmpleado es obligatorio.', 1;
    IF NULLIF(LTRIM(RTRIM(@Nombres)), '') IS NULL THROW 52002, 'Nombres es obligatorio.', 1;
    IF NULLIF(LTRIM(RTRIM(@Apellidos)), '') IS NULL THROW 52002, 'Apellidos es obligatorio.', 1;
    UPDATE dbo.Empleado SET CodigoEmpleado = @CodigoEmpleado, Nombres = @Nombres, Apellidos = @Apellidos, Telefono = @Telefono, Correo = @Correo WHERE EmpleadoID = @EmpleadoID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_EmpleadoListar
    @UsuarioActorID int,
    @Buscar varchar(180) = NULL,
    @Activo bit = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT EmpleadoID, CodigoEmpleado, Nombres, Apellidos, Telefono, Correo, Activo FROM dbo.Empleado
    WHERE (@Buscar IS NULL OR Nombres LIKE '%' + @Buscar + '%' OR Apellidos LIKE '%' + @Buscar + '%' OR CodigoEmpleado LIKE '%' + @Buscar + '%') AND (@Activo IS NULL OR Activo = @Activo)
    ORDER BY Nombres, EmpleadoID;
END;
GO

CREATE PROCEDURE dbo.sp_EmpleadoCambiarActivo
    @UsuarioActorID int,
    @EmpleadoID int,
    @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF @Activo = 0 AND EXISTS (SELECT 1 FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID = u.RolID WHERE u.EmpleadoID = @EmpleadoID AND u.Activo = 1 AND r.Nombre = 'Gerente')
        THROW 52003, 'Cambie o desactive primero la cuenta Gerente de este empleado.', 1;
    UPDATE dbo.Empleado SET Activo = @Activo WHERE EmpleadoID = @EmpleadoID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El registro no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_MarcaListar
    @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT MarcaID, Nombre, Activo FROM dbo.Marca ORDER BY Nombre;
END;
GO

CREATE PROCEDURE dbo.sp_CategoriaProductoListar
    @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT CategoriaProductoID, Nombre, Activo FROM dbo.CategoriaProducto ORDER BY Nombre;
END;
GO

CREATE PROCEDURE dbo.sp_PresentacionListar
    @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT PresentacionID, Nombre, Activo FROM dbo.Presentacion ORDER BY Nombre;
END;
GO

CREATE PROCEDURE dbo.sp_ColorListar
    @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    SELECT ColorID, Nombre, Activo FROM dbo.Color ORDER BY Nombre;
END;
GO

CREATE PROCEDURE dbo.sp_MarcaInsertar
    @UsuarioActorID int,
    @Nombre varchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID;
    IF NULLIF(LTRIM(RTRIM(@Nombre)), '') IS NULL THROW 52002, 'Nombre obligatorio.', 1;
    INSERT INTO dbo.Marca (Nombre, Activo) VALUES (LTRIM(RTRIM(@Nombre)), 1);
    SELECT CONVERT(int, SCOPE_IDENTITY()) AS MarcaID;
END;
GO

CREATE PROCEDURE dbo.sp_RolListar
    @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    SELECT RolID, Nombre, Descripcion, Activo FROM dbo.Rol ORDER BY Nombre;
END;
GO

CREATE PROCEDURE dbo.sp_RolModificar
    @UsuarioActorID int,
    @RolID int,
    @Descripcion varchar(200) = NULL,
    @Activo bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    IF @Activo = 0 AND EXISTS (SELECT 1 FROM dbo.Rol WHERE RolID = @RolID AND Nombre = 'Gerente')
        THROW 52003, 'El rol Gerente debe permanecer activo.', 1;
    UPDATE dbo.Rol SET Descripcion = @Descripcion, Activo = @Activo WHERE RolID = @RolID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El rol no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioListar
    @UsuarioActorID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    -- ISNULL conserva la columna Empleado como varchar(201) no anulable en el contrato.
    SELECT UsuarioID, EmpleadoID, RolID, NombreUsuario, Activo,
           ISNULL(Empleado, '') AS Empleado, Rol
    FROM dbo.vw_UsuariosRoles ORDER BY NombreUsuario;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioObtenerPorNombreUsuario
    @NombreUsuario varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UsuarioID, u.NombreUsuario, u.PasswordHash, u.Activo,
           e.Activo AS EmpleadoActivo, r.Activo AS RolActivo, r.Nombre AS Rol
    FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID = u.RolID
    JOIN dbo.Empleado e ON e.EmpleadoID = u.EmpleadoID
    WHERE u.NombreUsuario = @NombreUsuario;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioSesion
    @UsuarioID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UsuarioID, u.NombreUsuario, r.Nombre AS Rol
    FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID = u.RolID
    JOIN dbo.Empleado e ON e.EmpleadoID = u.EmpleadoID
    WHERE u.UsuarioID = @UsuarioID AND u.Activo = 1 AND e.Activo = 1 AND r.Activo = 1;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioCrear
    @UsuarioActorID int,
    @EmpleadoID int,
    @RolID int,
    @NombreUsuario varchar(50),
    @PasswordHash varchar(255)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    IF NULLIF(LTRIM(RTRIM(@NombreUsuario)), '') IS NULL THROW 52002, 'Nombre de usuario obligatorio.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Empleado WHERE EmpleadoID = @EmpleadoID AND Activo = 1) THROW 52002, 'Empleado inactivo o inexistente.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE RolID = @RolID AND Activo = 1 AND Nombre IN ('Gerente','Digitador','Cajero')) THROW 52002, 'Rol invalido o inactivo.', 1;
    IF @PasswordHash IS NULL OR @PasswordHash NOT LIKE 'PBKDF2$SHA256$%$%$%' THROW 52002, 'Se requiere un hash PBKDF2 generado por la aplicacion.', 1;
    INSERT INTO dbo.Usuario (EmpleadoID, RolID, NombreUsuario, PasswordHash, Activo)
    VALUES (@EmpleadoID, @RolID, LTRIM(RTRIM(@NombreUsuario)), @PasswordHash, 1);
    SELECT CONVERT(int, SCOPE_IDENTITY()) AS UsuarioID;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioModificar
    @UsuarioActorID int,
    @UsuarioID int,
    @EmpleadoID int,
    @NombreUsuario varchar(50),
    @PasswordHash varchar(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    IF @UsuarioID = @UsuarioActorID THROW 52003, 'No puede cambiar su propia cuenta desde administracion.', 1;
    IF NULLIF(LTRIM(RTRIM(@NombreUsuario)), '') IS NULL THROW 52002, 'Nombre de usuario obligatorio.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Empleado WHERE EmpleadoID = @EmpleadoID AND Activo = 1) THROW 52002, 'Empleado invalido.', 1;
    IF @PasswordHash IS NOT NULL AND @PasswordHash NOT LIKE 'PBKDF2$SHA256$%$%$%' THROW 52002, 'Hash invalido.', 1;
    UPDATE dbo.Usuario SET EmpleadoID = @EmpleadoID, NombreUsuario = LTRIM(RTRIM(@NombreUsuario)),
        PasswordHash = ISNULL(@PasswordHash, PasswordHash) WHERE UsuarioID = @UsuarioID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El usuario no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioCambiarRol
    @UsuarioActorID int,
    @UsuarioID int,
    @RolID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    IF @UsuarioID = @UsuarioActorID THROW 52003, 'No puede cambiar su propia cuenta desde administracion.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE RolID = @RolID AND Activo = 1 AND Nombre IN ('Gerente','Digitador','Cajero')) THROW 52002, 'Rol invalido.', 1;
    UPDATE dbo.Usuario SET RolID = @RolID WHERE UsuarioID = @UsuarioID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El usuario no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_UsuarioCambiarActivo
    @UsuarioActorID int,
    @UsuarioID int,
    @Activo bit
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    IF @UsuarioID = @UsuarioActorID THROW 52003, 'No puede cambiar su propia cuenta desde administracion.', 1;
    IF @Activo = 1 AND NOT EXISTS (SELECT 1 FROM dbo.Usuario u JOIN dbo.Empleado e ON e.EmpleadoID = u.EmpleadoID JOIN dbo.Rol r ON r.RolID = u.RolID WHERE u.UsuarioID = @UsuarioID AND e.Activo = 1 AND r.Activo = 1)
        THROW 52002, 'El empleado y el rol deben estar activos.', 1;
    UPDATE dbo.Usuario SET Activo = @Activo WHERE UsuarioID = @UsuarioID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El usuario no existe.', 1;
END;
GO

CREATE PROCEDURE dbo.sp_ConfiguracionInicialPendiente

AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.Usuario) THEN 0 ELSE 1 END) AS Pendiente;
END;
GO

CREATE PROCEDURE dbo.sp_ConfiguracionInicial
    @CodigoEmpleado varchar(30),
    @Nombres varchar(100),
    @Apellidos varchar(100),
    @NombreUsuario varchar(50),
    @PasswordHash varchar(255)
AS
BEGIN
    SET NOCOUNT ON;
    IF @PasswordHash IS NULL OR @PasswordHash NOT LIKE 'PBKDF2$SHA256$%$%$%' THROW 52002, 'Se requiere un hash PBKDF2 generado por la aplicacion.', 1;
    IF NULLIF(LTRIM(RTRIM(@CodigoEmpleado)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@Nombres)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Apellidos)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@NombreUsuario)), '') IS NULL
        THROW 52002, 'Complete todos los campos de configuracion inicial.', 1;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Serializa la primera alta para que dos ventanas no creen dos cuentas iniciales.
        IF EXISTS (SELECT 1 FROM dbo.Usuario WITH (TABLOCKX, HOLDLOCK))
            THROW 52005, 'La configuracion inicial ya fue completada.', 1;
        DECLARE @RolID int, @EmpleadoID int;
        SELECT @RolID = RolID FROM dbo.Rol WHERE Nombre = 'Gerente' AND Activo = 1;
        IF @RolID IS NULL THROW 52002, 'No existe el rol Gerente activo. Ejecute los catalogos.', 1;
        INSERT INTO dbo.Empleado (CodigoEmpleado, Nombres, Apellidos, Activo)
        VALUES (@CodigoEmpleado, @Nombres, @Apellidos, 1);
        SET @EmpleadoID = SCOPE_IDENTITY();
        INSERT INTO dbo.Usuario (EmpleadoID, RolID, NombreUsuario, PasswordHash, Activo)
        VALUES (@EmpleadoID, @RolID, LTRIM(RTRIM(@NombreUsuario)), @PasswordHash, 1);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
