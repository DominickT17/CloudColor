-- Solo catalogos del enunciado. Se puede repetir sin duplicar nombres.
USE CloudColorDB;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = 'Digitador')
        INSERT INTO dbo.Rol (Nombre, Activo) VALUES ('Digitador', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = 'Cajero')
        INSERT INTO dbo.Rol (Nombre, Activo) VALUES ('Cajero', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre = 'Gerente')
        INSERT INTO dbo.Rol (Nombre, Activo) VALUES ('Gerente', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Accesorios')
        INSERT INTO dbo.CategoriaProducto (Nombre, Activo) VALUES ('Accesorios', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Solventes')
        INSERT INTO dbo.CategoriaProducto (Nombre, Activo) VALUES ('Solventes', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Pinturas')
        INSERT INTO dbo.CategoriaProducto (Nombre, Activo) VALUES ('Pinturas', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Barnices')
        INSERT INTO dbo.CategoriaProducto (Nombre, Activo) VALUES ('Barnices', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Lacas')
        INSERT INTO dbo.CategoriaProducto (Nombre, Activo) VALUES ('Lacas', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = 'Selladores')
        INSERT INTO dbo.CategoriaProducto (Nombre, Activo) VALUES ('Selladores', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.MedioPago WHERE Nombre = 'Efectivo')
        INSERT INTO dbo.MedioPago (Nombre, Activo) VALUES ('Efectivo', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.MedioPago WHERE Nombre = 'Cheque')
        INSERT INTO dbo.MedioPago (Nombre, Activo) VALUES ('Cheque', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.MedioPago WHERE Nombre = 'Tarjeta')
        INSERT INTO dbo.MedioPago (Nombre, Activo) VALUES ('Tarjeta', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = 'Unidad')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('Unidad', 'Unidad', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/32 de galon')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1/32 de galon', 'Galon', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/16 de galon')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1/16 de galon', 'Galon', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/8 de galon')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1/8 de galon', 'Galon', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/4 de galon')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1/4 de galon', 'Galon', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1/2 de galon')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1/2 de galon', 'Galon', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1 galon')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1 galon', 'Galon', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Presentacion WHERE Nombre = '1 cubeta')
        INSERT INTO dbo.Presentacion (Nombre, UnidadMedida, Activo) VALUES ('1 cubeta', 'Cubeta', 1);
    COMMIT TRANSACTION;
    PRINT 'Catalogos iniciales disponibles.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
