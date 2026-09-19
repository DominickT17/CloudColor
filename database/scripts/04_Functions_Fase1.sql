USE CloudColorDB;
GO
-- Nombre completo centralizado para listados y seleccion de empleados.
CREATE FUNCTION dbo.fn_EmpleadoNombreCompleto (@EmpleadoID int)
RETURNS varchar(201)
AS
BEGIN
    DECLARE @Nombre varchar(201);
    SELECT @Nombre = Nombres + ' ' + Apellidos FROM dbo.Empleado WHERE EmpleadoID = @EmpleadoID;
    RETURN @Nombre;
END;
GO
