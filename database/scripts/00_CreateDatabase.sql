-- Ejecutar primero en SSMS con permisos para crear bases de datos.
USE master;
GO
IF DB_ID('CloudColorDB') IS NULL
BEGIN
    CREATE DATABASE CloudColorDB;
    PRINT 'Base CloudColorDB creada.';
END
ELSE
BEGIN
    PRINT 'CloudColorDB ya existe; no se ha reemplazado.';
END;
GO
USE CloudColorDB;
GO
