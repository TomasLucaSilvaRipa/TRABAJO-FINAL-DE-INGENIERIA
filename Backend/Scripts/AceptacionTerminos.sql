IF NOT EXISTS (SELECT 1 FROM dbo.TerminosCondiciones WHERE Vigente = 1)
BEGIN
    INSERT INTO dbo.TerminosCondiciones(Version, Titulo, Contenido, FechaVigenciaDesde, FechaVigenciaHasta, Vigente)
    VALUES(N'1.0', N'Términos y Condiciones TeamBalance', N'Versión vigente de los Términos y Condiciones publicados por TeamBalance.', SYSDATETIME(), NULL, 1);
END;
GO

DECLARE @IdTerminosCondiciones INT;
SELECT TOP (1) @IdTerminosCondiciones = ID FROM dbo.TerminosCondiciones WHERE Vigente = 1 ORDER BY FechaVigenciaDesde DESC, ID DESC;

INSERT INTO dbo.AceptacionTerminos(IdUsuario, IdTerminosCondiciones, FechaAceptacion, DireccionIP)
SELECT usuario.ID, @IdTerminosCondiciones, COALESCE(usuario.FechaAlta, SYSDATETIME()), NULL
FROM dbo.Usuario usuario
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.AceptacionTerminos aceptacion
    WHERE aceptacion.IdUsuario = usuario.ID AND aceptacion.IdTerminosCondiciones = @IdTerminosCondiciones
);
GO

DECLARE @Sql NVARCHAR(MAX) = N'';
SELECT @Sql = @Sql + N'ALTER TABLE dbo.Usuario DROP CONSTRAINT [' + nombre.name + N'];'
FROM sys.default_constraints nombre
INNER JOIN sys.columns columna ON columna.object_id = nombre.parent_object_id AND columna.column_id = nombre.parent_column_id
WHERE nombre.parent_object_id = OBJECT_ID(N'dbo.Usuario') AND columna.name IN (N'AceptaTerminos', N'FechaAceptacionTerminos');
IF @Sql <> N'' EXEC sp_executesql @Sql;
GO

IF COL_LENGTH(N'dbo.Usuario', N'AceptaTerminos') IS NOT NULL ALTER TABLE dbo.Usuario DROP COLUMN AceptaTerminos;
IF COL_LENGTH(N'dbo.Usuario', N'FechaAceptacionTerminos') IS NOT NULL ALTER TABLE dbo.Usuario DROP COLUMN FechaAceptacionTerminos;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_RegistrarGestion
    @IdAgencia INT = NULL,
    @IdRolPrincipal INT,
    @Nombre NVARCHAR(100),
    @Apellido NVARCHAR(100),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(1000),
    @Estado NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS(SELECT 1 FROM dbo.Usuario WHERE Email = @Email)
        THROW 52005, 'Ya existe un usuario con ese email.', 1;

    DECLARE @IdUsuario INT;
    DECLARE @IdTerminosCondiciones INT;

    INSERT INTO dbo.Usuario(IdAgencia, IdRol, Nombre, Apellido, Email, PasswordHash, Estado, FechaAlta, Activo)
    VALUES(@IdAgencia, @IdRolPrincipal, @Nombre, @Apellido, @Email, @PasswordHash, @Estado, SYSDATETIME(), 1);

    SET @IdUsuario = CONVERT(INT, SCOPE_IDENTITY());

    SELECT TOP (1) @IdTerminosCondiciones = ID FROM dbo.TerminosCondiciones WHERE Vigente = 1 ORDER BY FechaVigenciaDesde DESC, ID DESC;

    IF @IdTerminosCondiciones IS NOT NULL
        INSERT INTO dbo.AceptacionTerminos(IdUsuario, IdTerminosCondiciones, FechaAceptacion, DireccionIP) VALUES(@IdUsuario, @IdTerminosCondiciones, SYSDATETIME(), NULL);

    SELECT @IdUsuario AS ID;
END;
GO
