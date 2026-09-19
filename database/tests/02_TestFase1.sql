USE CloudColorDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT OFF;
IF @@TRANCOUNT <> 0 THROW 53000, 'Ejecute sin transacciones abiertas.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Codigo varchar(25) = LEFT(CONVERT(varchar(36), NEWID()), 25);
    DECLARE @Gerente int, @Empleado int, @Usuario int, @Rol int, @Marca int, @Categoria int;
    DECLARE @Cliente int, @Proveedor int, @Producto int, @Rechazado bit;
    DECLARE @IDs TABLE (ID int);
    -- Credencial deliberadamente inutilizable: solo se prueba el contrato SQL, no un login.
    DECLARE @Hash varchar(255) = 'PBKDF2$SHA256$600000$SOLO_PRUEBA$NO_ES_UN_HASH_VALIDO';
    SELECT @Rol = RolID FROM dbo.Rol WHERE Nombre = 'Gerente';
    INSERT dbo.Empleado (CodigoEmpleado, Nombres, Apellidos, Activo) VALUES (@Codigo, 'Gerente temporal', 'Prueba', 1);
    SET @Empleado = SCOPE_IDENTITY();
    INSERT dbo.Usuario (EmpleadoID, RolID, NombreUsuario, PasswordHash, Activo) VALUES (@Empleado, @Rol, @Codigo, @Hash, 1);
    SET @Gerente = SCOPE_IDENTITY();
    INSERT @IDs EXEC dbo.sp_ClienteInsertar @Gerente, 'Particular', 'Cliente temporal';
    SELECT @Cliente = ID FROM @IDs;
    EXEC dbo.sp_ClienteModificar @Gerente, @Cliente, 'Empresa', 'Cliente modificado', @NIT = 'PRUEBA';
    EXEC dbo.sp_ClienteCambiarActivo @Gerente, @Cliente, 0;
    IF NOT EXISTS (SELECT 1 FROM dbo.Cliente WHERE ClienteID = @Cliente AND Activo = 0 AND NIT = 'PRUEBA' AND TipoCliente = 'Empresa') THROW 53001, 'Fallo Cliente.', 1;
    EXEC dbo.sp_ClienteCambiarActivo @Gerente, @Cliente, 1;
    DECLARE @Clientes TABLE (ClienteID int, TipoCliente varchar(20), NombreRazonSocial varchar(180), NIT varchar(30), Telefono varchar(30), Correo varchar(150), Direccion varchar(250), Activo bit);
    INSERT @Clientes EXEC dbo.sp_ClienteListar @Gerente, 'Cliente modificado', 1;
    IF NOT EXISTS (SELECT 1 FROM @Clientes WHERE ClienteID = @Cliente) THROW 53002, 'Fallo consulta parametrizada Cliente.', 1;
    DELETE FROM @IDs;
    INSERT @IDs EXEC dbo.sp_ProveedorInsertar @Gerente, 'Proveedor temporal';
    SELECT @Proveedor = ID FROM @IDs;
    EXEC dbo.sp_ProveedorModificar @Gerente, @Proveedor, 'Proveedor modificado';
    EXEC dbo.sp_ProveedorCambiarActivo @Gerente, @Proveedor, 0;
    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedor WHERE ProveedorID = @Proveedor AND Nombre = 'Proveedor modificado' AND Activo = 0) THROW 53003, 'Fallo Proveedor.', 1;
    EXEC dbo.sp_ProveedorCambiarActivo @Gerente, @Proveedor, 1;
    EXEC dbo.sp_ProveedorListar @Gerente, 'Proveedor modificado';
    DELETE FROM @IDs;
    INSERT @IDs EXEC dbo.sp_MarcaInsertar @Gerente, @Codigo;
    SELECT @Marca = ID FROM @IDs;
    SELECT @Categoria = CategoriaProductoID FROM dbo.CategoriaProducto WHERE Nombre = 'Pinturas';
    DELETE FROM @IDs;
    INSERT @IDs EXEC dbo.sp_ProductoInsertar @Gerente, @Categoria, @Marca, 'Producto temporal', NULL, 1, 1;
    SELECT @Producto = ID FROM @IDs;
    EXEC dbo.sp_ProductoModificar @Gerente, @Producto, @Categoria, @Marca, 'Producto modificado', NULL, 1, 1;
    EXEC dbo.sp_ProductoCambiarActivo @Gerente, @Producto, 0;
    IF NOT EXISTS (SELECT 1 FROM dbo.Producto WHERE ProductoID = @Producto AND Nombre = 'Producto modificado' AND Activo = 0) THROW 53004, 'Fallo Producto.', 1;
    EXEC dbo.sp_ProductoCambiarActivo @Gerente, @Producto, 1;
    EXEC dbo.sp_ProductoListar @Gerente, 'Producto modificado';
    DELETE FROM @IDs;
    DECLARE @Codigo2 varchar(30) = 'E-' + @Codigo;
    INSERT @IDs EXEC dbo.sp_EmpleadoInsertar @Gerente, @Codigo2, 'Empleado', 'Temporal';
    SELECT @Empleado = ID FROM @IDs;
    EXEC dbo.sp_EmpleadoModificar @Gerente, @Empleado, @Codigo2, 'Empleado modificado', 'Temporal';
    EXEC dbo.sp_EmpleadoCambiarActivo @Gerente, @Empleado, 0;
    IF NOT EXISTS (SELECT 1 FROM dbo.Empleado WHERE EmpleadoID = @Empleado AND Activo = 0) THROW 53005, 'Fallo Empleado.', 1;
    EXEC dbo.sp_EmpleadoCambiarActivo @Gerente, @Empleado, 1;
    EXEC dbo.sp_EmpleadoListar @Gerente, 'Empleado modificado';
    IF dbo.fn_EmpleadoNombreCompleto(@Empleado) <> 'Empleado modificado Temporal' THROW 53006, 'Fallo funcion.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.vw_ProductosDetalle WHERE ProductoID = @Producto AND Marca = @Codigo AND Categoria = 'Pinturas') THROW 53007, 'Fallo vista productos.', 1;
    SELECT @Rol = RolID FROM dbo.Rol WHERE Nombre = 'Digitador';
    DELETE FROM @IDs;
    INSERT @IDs EXEC dbo.sp_UsuarioCrear @Gerente, @Empleado, @Rol, @Codigo2, @Hash;
    SELECT @Usuario = ID FROM @IDs;
    EXEC dbo.sp_UsuarioModificar @Gerente, @Usuario, @Empleado, @Codigo2;
    EXEC dbo.sp_UsuarioCambiarActivo @Gerente, @Usuario, 0;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE UsuarioID = @Usuario AND Activo = 0 AND PasswordHash = @Hash) THROW 53008, 'Fallo Usuario.', 1;
    EXEC dbo.sp_UsuarioCambiarActivo @Gerente, @Usuario, 1;
    EXEC dbo.sp_ClienteListar @Usuario, 'Cliente modificado';
    SELECT @Rol = RolID FROM dbo.Rol WHERE Nombre = 'Cajero';
    EXEC dbo.sp_UsuarioCambiarRol @Gerente, @Usuario, @Rol;
    EXEC dbo.sp_RolModificar @Gerente, @Rol, 'Descripcion temporal', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE RolID = @Rol AND Descripcion = 'Descripcion temporal') THROW 53009, 'Fallo Rol.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.vw_UsuariosRoles WHERE UsuarioID = @Usuario AND Rol = 'Cajero') THROW 53010, 'Fallo vista usuarios.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.vw_UsuariosRoles') AND name = 'PasswordHash') THROW 53011, 'La vista expone credenciales.', 1;
    EXEC dbo.sp_UsuarioListar @Gerente;
    EXEC dbo.sp_RolListar @Gerente;
    SET @Rechazado = 0;
    BEGIN TRY
        EXEC dbo.sp_ClienteListar @Usuario;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 52001 THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 53012, 'Cajero accedio a mantenimiento.', 1;
    SET @Rechazado = 0;
    BEGIN TRY
        EXEC dbo.sp_ProductoModificar @Gerente, @Producto, @Categoria, -2147483648, 'Invalido', NULL, 1, 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 52002 THROW;
        SET @Rechazado = 1;
    END CATCH;
    IF @Rechazado = 0 THROW 53013, 'Se acepto marca inexistente.', 1;
    ROLLBACK TRANSACTION;
    PRINT 'OK Fase I: cuatro mantenimientos, parametros, usuarios, roles, permisos, funcion y vistas. Datos revertidos.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
