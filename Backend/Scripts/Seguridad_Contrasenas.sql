/*
  Seguridad TeamBalance
  Recupero y cambio de contraseñas usando Usuario, SesionUsuario y ValidacionCuenta existentes.
*/
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarPorSesion
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        u.ID,
        u.IdAgencia,
        u.IdRol,
        u.Nombre,
        u.Apellido,
        u.Email,
        u.PasswordHash,
        u.Estado,
        u.FechaAlta,
        u.Activo,
        u.FechaBaja
    FROM dbo.Usuario u
    INNER JOIN dbo.SesionUsuario s ON s.IdUsuario = u.ID
    WHERE s.TokenHash = @TokenHash
      AND s.Activa = 1
      AND s.FechaExpiracion >= SYSDATETIME()
      AND u.Activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RecuperacionPassword_ConsultarUsuario
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        u.ID,
        u.IdAgencia,
        u.IdRol,
        u.Nombre,
        u.Apellido,
        u.Email,
        u.PasswordHash,
        u.Estado,
        u.FechaAlta,
        u.Activo,
        u.FechaBaja
    FROM dbo.ValidacionCuenta v
    INNER JOIN dbo.Usuario u ON u.ID = v.IdUsuario
    WHERE v.TokenHash = @TokenHash
      AND v.Metodo = N'RecuperacionPassword'
      AND v.Utilizado = 0
      AND v.Activo = 1
      AND v.FechaExpiracion >= SYSDATETIME()
      AND u.Activo = 1
      AND u.Estado = N'Activo';
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RecuperacionPassword_Restablecer
    @IdUsuario INT,
    @TokenHash NVARCHAR(500),
    @PasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdValidacion INT;

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @IdValidacion = v.ID
    FROM dbo.ValidacionCuenta v WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.Usuario u ON u.ID = v.IdUsuario
    WHERE v.IdUsuario = @IdUsuario
      AND v.TokenHash = @TokenHash
      AND v.Metodo = N'RecuperacionPassword'
      AND v.Utilizado = 0
      AND v.Activo = 1
      AND v.FechaExpiracion >= SYSDATETIME()
      AND u.Activo = 1
      AND u.Estado = N'Activo';

    IF @IdValidacion IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51011, 'El enlace de recuperación es inválido, venció o ya fue utilizado.', 1;
    END;

    UPDATE dbo.Usuario
    SET PasswordHash = @PasswordHash
    WHERE ID = @IdUsuario;

    UPDATE dbo.ValidacionCuenta
    SET Utilizado = 1,
        FechaUtilizacion = SYSDATETIME(),
        Activo = 0
    WHERE ID = @IdValidacion;

    UPDATE dbo.SesionUsuario
    SET Activa = 0
    WHERE IdUsuario = @IdUsuario
      AND Activa = 1;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_CambiarPassword
    @IdUsuario INT,
    @PasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.Usuario
    SET PasswordHash = @PasswordHash
    WHERE ID = @IdUsuario
      AND Activo = 1;

    IF @@ROWCOUNT <> 1
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51012, 'No se encontró una cuenta activa para modificar la contraseña.', 1;
    END;

    UPDATE dbo.SesionUsuario
    SET Activa = 0
    WHERE IdUsuario = @IdUsuario
      AND Activa = 1;

    COMMIT TRANSACTION;
END;
GO
