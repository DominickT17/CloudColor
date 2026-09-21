USE CloudColorDB;
GO
-- Nunca expone PasswordHash.
CREATE VIEW dbo.vw_UsuariosRoles
AS
    SELECT u.UsuarioID, u.EmpleadoID, u.RolID, u.NombreUsuario,
           dbo.fn_EmpleadoNombreCompleto(u.EmpleadoID) AS Empleado,
           r.Nombre AS Rol, u.Activo, e.Activo AS EmpleadoActivo, r.Activo AS RolActivo
    FROM dbo.Usuario u JOIN dbo.Empleado e ON e.EmpleadoID = u.EmpleadoID
    JOIN dbo.Rol r ON r.RolID = u.RolID;
GO
CREATE VIEW dbo.vw_ProductosDetalle
AS
    SELECT p.ProductoID, p.Nombre, p.Descripcion, p.CategoriaProductoID, p.MarcaID,
           c.Nombre AS Categoria, m.Nombre AS Marca, p.ManejaLote, p.AplicaPEPS, p.Activo
    FROM dbo.Producto p JOIN dbo.CategoriaProducto c ON c.CategoriaProductoID = p.CategoriaProductoID
    JOIN dbo.Marca m ON m.MarcaID = p.MarcaID;
GO
