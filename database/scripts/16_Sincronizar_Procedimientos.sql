-- Sincroniza procedimientos existentes sin modificar tablas ni datos.
-- Requiere haber instalado los scripts 03 y 10. Ejecutar completo con errores habilitados.
USE CloudColorDB;
GO

ALTER PROCEDURE dbo.sp_RolModificar
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
    INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
    VALUES(@UsuarioActorID,GETDATE(),'MODIFICAR_ROL','Rol',CONVERT(varchar(100),@RolID),
        CONCAT('Descripcion: ',ISNULL(@Descripcion,''),' | Activo: ',CONVERT(varchar(1),@Activo)));
END;
GO

ALTER PROCEDURE dbo.sp_UsuarioCrear
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
    DECLARE @NuevoUsuarioID int=CONVERT(int,SCOPE_IDENTITY());
    INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosNuevos)
    VALUES(@UsuarioActorID,GETDATE(),'CREAR_USUARIO','Usuario',CONVERT(varchar(100),@NuevoUsuarioID),
        CONCAT('Nombre: ',LTRIM(RTRIM(@NombreUsuario)),' | EmpleadoID: ',@EmpleadoID,' | RolID: ',@RolID));
    SELECT @NuevoUsuarioID AS UsuarioID;
END;
GO

ALTER PROCEDURE dbo.sp_UsuarioModificar
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
    DECLARE @NombreAnterior varchar(50),@EmpleadoAnterior int;
    SELECT @NombreAnterior=NombreUsuario,@EmpleadoAnterior=EmpleadoID FROM dbo.Usuario WHERE UsuarioID=@UsuarioID;
    UPDATE dbo.Usuario SET EmpleadoID = @EmpleadoID, NombreUsuario = LTRIM(RTRIM(@NombreUsuario)),
        PasswordHash = ISNULL(@PasswordHash, PasswordHash) WHERE UsuarioID = @UsuarioID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El usuario no existe.', 1;
    INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosAnteriores,DatosNuevos)
    VALUES(@UsuarioActorID,GETDATE(),'MODIFICAR_USUARIO','Usuario',CONVERT(varchar(100),@UsuarioID),
        CONCAT('Nombre: ',@NombreAnterior,' | EmpleadoID: ',@EmpleadoAnterior),
        CONCAT('Nombre: ',LTRIM(RTRIM(@NombreUsuario)),' | EmpleadoID: ',@EmpleadoID,
            CASE WHEN @PasswordHash IS NULL THEN '' ELSE ' | Contraseña actualizada (verificador omitido)' END));
END;
GO

ALTER PROCEDURE dbo.sp_UsuarioCambiarRol
    @UsuarioActorID int,
    @UsuarioID int,
    @RolID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase1 @UsuarioActorID, 1;
    IF @UsuarioID = @UsuarioActorID THROW 52003, 'No puede cambiar su propia cuenta desde administracion.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE RolID = @RolID AND Activo = 1 AND Nombre IN ('Gerente','Digitador','Cajero')) THROW 52002, 'Rol invalido.', 1;
    DECLARE @RolAnterior varchar(50),@RolNuevo varchar(50);
    SELECT @RolAnterior=r.Nombre FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolID=u.RolID WHERE u.UsuarioID=@UsuarioID;
    SELECT @RolNuevo=Nombre FROM dbo.Rol WHERE RolID=@RolID;
    UPDATE dbo.Usuario SET RolID = @RolID WHERE UsuarioID = @UsuarioID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El usuario no existe.', 1;
    INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosAnteriores,DatosNuevos)
    VALUES(@UsuarioActorID,GETDATE(),'CAMBIAR_ROL_USUARIO','Usuario',CONVERT(varchar(100),@UsuarioID),@RolAnterior,@RolNuevo);
END;
GO

ALTER PROCEDURE dbo.sp_FacturaPagos @UsuarioActorID int, @FacturaID int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_AutorizarFase2 @UsuarioActorID, 'FACTURA_CONSULTA';
    SELECT p.MedioPagoID,m.Nombre AS Medio,p.Monto,p.Referencia,p.FechaHora,
        u.NombreUsuario AS Cajero
    FROM dbo.PagoFactura p JOIN dbo.MedioPago m ON m.MedioPagoID=p.MedioPagoID
    JOIN dbo.Usuario u ON u.UsuarioID=p.UsuarioCajeroID
    WHERE p.FacturaID=@FacturaID ORDER BY p.PagoFacturaID;
END;
GO

ALTER PROCEDURE dbo.sp_UsuarioCambiarActivo
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
    DECLARE @ActivoAnterior bit;
    SELECT @ActivoAnterior=Activo FROM dbo.Usuario WHERE UsuarioID=@UsuarioID;
    UPDATE dbo.Usuario SET Activo = @Activo WHERE UsuarioID = @UsuarioID;
    IF @@ROWCOUNT = 0 THROW 52004, 'El usuario no existe.', 1;
    INSERT dbo.Bitacora(UsuarioID,FechaHora,Accion,TablaAfectada,RegistroID,DatosAnteriores,DatosNuevos)
    VALUES(@UsuarioActorID,GETDATE(),'CAMBIAR_ESTADO_USUARIO','Usuario',CONVERT(varchar(100),@UsuarioID),
        CASE WHEN @ActivoAnterior=1 THEN 'Activo' ELSE 'Inactivo' END,
        CASE WHEN @Activo=1 THEN 'Activo' ELSE 'Inactivo' END);
END;
GO
