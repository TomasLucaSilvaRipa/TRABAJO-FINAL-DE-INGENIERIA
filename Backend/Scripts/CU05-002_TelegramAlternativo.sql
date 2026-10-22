/* CU05-002: contexto efímero para elegir email o Telegram tras el alta. */
GO
IF OBJECT_ID(N'dbo.SesionValidacionRegistro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SesionValidacionRegistro
    (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdUsuario INT NOT NULL,
        TokenHash NVARCHAR(500) NOT NULL,
        FechaExpiracion DATETIME2 NOT NULL,
        Activa BIT NOT NULL CONSTRAINT DF_SesionValidacionRegistro_Activa DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_SesionValidacionRegistro_FechaCreacion DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_SesionValidacionRegistro_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT UQ_SesionValidacionRegistro_TokenHash UNIQUE (TokenHash)
    );
    CREATE INDEX IX_SesionValidacionRegistro_UsuarioActiva ON dbo.SesionValidacionRegistro (IdUsuario, Activa);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistroValidacion_CrearSesion
    @IdUsuario INT,
    @TokenHash NVARCHAR(500),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.SesionValidacionRegistro SET Activa = 0 WHERE IdUsuario = @IdUsuario AND Activa = 1;
    INSERT INTO dbo.SesionValidacionRegistro (IdUsuario, TokenHash, FechaExpiracion, Activa) VALUES (@IdUsuario, @TokenHash, @FechaExpiracion, 1);
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistroValidacion_ConsultarSesion
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) sesion.IdUsuario
    FROM dbo.SesionValidacionRegistro sesion
    INNER JOIN dbo.Usuario usuario ON usuario.ID = sesion.IdUsuario
    WHERE sesion.TokenHash = @TokenHash AND sesion.Activa = 1 AND sesion.FechaExpiracion >= SYSDATETIME()
      AND usuario.Estado = N'PendienteValidacion' AND usuario.Activo = 1;
END;
GO
