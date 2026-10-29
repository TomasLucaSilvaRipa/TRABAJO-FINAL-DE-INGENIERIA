/* Ejecutar en la base TeamBalance antes de iniciar esta versión de la API.
   Las sesiones anteriores requieren iniciar sesión nuevamente.
   No crea tablas: agrega versiones de autorización a Usuario y SesionUsuario. */
IF COL_LENGTH(N'dbo.Usuario', N'VersionAutorizacion') IS NULL
    ALTER TABLE dbo.Usuario ADD VersionAutorizacion BIGINT NOT NULL CONSTRAINT DF_Usuario_VersionAutorizacion DEFAULT (0);
IF COL_LENGTH(N'dbo.SesionUsuario', N'VersionAutorizacion') IS NULL
    ALTER TABLE dbo.SesionUsuario ADD VersionAutorizacion BIGINT NOT NULL CONSTRAINT DF_SesionUsuario_VersionAutorizacion DEFAULT (-1);
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarVersionAutorizacion
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT VersionAutorizacion FROM dbo.Usuario WHERE ID = @IdUsuario AND Activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SesionUsuario_Registrar
    @IdUsuario INT,
    @TokenHash NVARCHAR(500),
    @FechaExpiracion DATETIME2,
    @VersionAutorizacion BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SesionUsuario
        (IdUsuario, TokenHash, FechaInicio, FechaUltimaActividad, FechaExpiracion, Activa, VersionAutorizacion)
    SELECT usuario.ID, @TokenHash, SYSDATETIME(), SYSDATETIME(), @FechaExpiracion, 1, usuario.VersionAutorizacion
    FROM dbo.Usuario usuario WITH (UPDLOCK, HOLDLOCK)
    WHERE usuario.ID = @IdUsuario AND usuario.Activo = 1
      AND (@VersionAutorizacion IS NULL OR usuario.VersionAutorizacion = @VersionAutorizacion);
    IF @@ROWCOUNT <> 1
        THROW 52101, 'La autorización cambió durante el inicio de sesión. Volvé a iniciar sesión.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SesionUsuario_Validar
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE sesion
    SET FechaUltimaActividad = SYSDATETIME()
    FROM dbo.SesionUsuario sesion
    INNER JOIN dbo.Usuario usuario ON usuario.ID = sesion.IdUsuario
    WHERE sesion.TokenHash = @TokenHash AND sesion.Activa = 1
      AND sesion.FechaExpiracion >= SYSDATETIME() AND usuario.Activo = 1
      AND sesion.VersionAutorizacion = usuario.VersionAutorizacion;
    SELECT CONVERT(BIT, CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END) AS Vigente;
END;
GO

CREATE OR ALTER TRIGGER dbo.tr_Usuario_RevocarAutorizacion
ON dbo.Usuario AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Usuarios TABLE (ID INT PRIMARY KEY);
    INSERT INTO @Usuarios
    SELECT actual.ID FROM inserted actual INNER JOIN deleted anterior ON anterior.ID = actual.ID
    WHERE EXISTS (
        SELECT actual.IdAgencia, actual.IdRol, actual.Nombre, actual.Apellido, actual.Email, actual.PasswordHash, actual.Estado, actual.Activo
        EXCEPT
        SELECT anterior.IdAgencia, anterior.IdRol, anterior.Nombre, anterior.Apellido, anterior.Email, anterior.PasswordHash, anterior.Estado, anterior.Activo
    );
    IF NOT EXISTS (SELECT 1 FROM @Usuarios) RETURN;
    UPDATE usuario SET VersionAutorizacion = VersionAutorizacion + 1
    FROM dbo.Usuario usuario INNER JOIN @Usuarios afectado ON afectado.ID = usuario.ID;
    UPDATE sesion SET Activa = 0, FechaCierre = SYSDATETIME()
    FROM dbo.SesionUsuario sesion INNER JOIN @Usuarios afectado ON afectado.ID = sesion.IdUsuario
    WHERE sesion.Activa = 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.tr_UsuarioRol_RevocarAutorizacion
ON dbo.UsuarioRol AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE usuario SET VersionAutorizacion = VersionAutorizacion + 1
    FROM dbo.Usuario usuario
    WHERE usuario.ID IN (SELECT IdUsuario FROM inserted UNION SELECT IdUsuario FROM deleted);
    UPDATE sesion SET Activa = 0, FechaCierre = SYSDATETIME()
    FROM dbo.SesionUsuario sesion
    WHERE sesion.Activa = 1 AND sesion.IdUsuario IN (SELECT IdUsuario FROM inserted UNION SELECT IdUsuario FROM deleted);
END;
GO

CREATE OR ALTER TRIGGER dbo.tr_Rol_RevocarAutorizacion
ON dbo.Rol AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE usuario SET VersionAutorizacion = VersionAutorizacion + 1
    FROM dbo.Usuario usuario
    WHERE EXISTS (SELECT 1 FROM dbo.UsuarioRol usuarioRol WHERE usuarioRol.IdUsuario = usuario.ID
        AND usuarioRol.IdRol IN (SELECT ID FROM inserted UNION SELECT ID FROM deleted));
    UPDATE sesion SET Activa = 0, FechaCierre = SYSDATETIME()
    FROM dbo.SesionUsuario sesion INNER JOIN dbo.Usuario usuario ON usuario.ID = sesion.IdUsuario
    WHERE sesion.Activa = 1 AND sesion.VersionAutorizacion <> usuario.VersionAutorizacion;
END;
GO

CREATE OR ALTER TRIGGER dbo.tr_RolPermiso_RevocarAutorizacion
ON dbo.RolPermiso AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE usuario SET VersionAutorizacion = VersionAutorizacion + 1
    FROM dbo.Usuario usuario
    WHERE EXISTS (SELECT 1 FROM dbo.UsuarioRol usuarioRol WHERE usuarioRol.IdUsuario = usuario.ID
        AND usuarioRol.IdRol IN (SELECT IdRol FROM inserted UNION SELECT IdRol FROM deleted));
    UPDATE sesion SET Activa = 0, FechaCierre = SYSDATETIME()
    FROM dbo.SesionUsuario sesion INNER JOIN dbo.Usuario usuario ON usuario.ID = sesion.IdUsuario
    WHERE sesion.Activa = 1 AND sesion.VersionAutorizacion <> usuario.VersionAutorizacion;
END;
GO

CREATE OR ALTER TRIGGER dbo.tr_Permiso_RevocarAutorizacion
ON dbo.Permiso AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE usuario SET VersionAutorizacion = VersionAutorizacion + 1
    FROM dbo.Usuario usuario
    WHERE EXISTS (SELECT 1 FROM dbo.UsuarioRol usuarioRol INNER JOIN dbo.RolPermiso rolPermiso ON rolPermiso.IdRol = usuarioRol.IdRol
        WHERE usuarioRol.IdUsuario = usuario.ID AND rolPermiso.IdPermiso IN (SELECT ID FROM inserted UNION SELECT ID FROM deleted));
    UPDATE sesion SET Activa = 0, FechaCierre = SYSDATETIME()
    FROM dbo.SesionUsuario sesion INNER JOIN dbo.Usuario usuario ON usuario.ID = sesion.IdUsuario
    WHERE sesion.Activa = 1 AND sesion.VersionAutorizacion <> usuario.VersionAutorizacion;
END;
GO
