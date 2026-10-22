/* Foto única de perfil: se almacena como Data URL validada por la BLL. */
GO

IF COL_LENGTH(N'dbo.Usuario', N'FotoPerfil') IS NULL
    ALTER TABLE dbo.Usuario ADD FotoPerfil NVARCHAR(MAX) NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ActualizarFotoPerfil
    @IdUsuario INT,
    @FotoPerfil NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE ID = @IdUsuario AND Activo = 1)
        THROW 52040, 'El usuario indicado no existe o se encuentra inactivo.', 1;

    UPDATE dbo.Usuario
    SET FotoPerfil = @FotoPerfil
    WHERE ID = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarFotoPerfil
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT FotoPerfil
    FROM dbo.Usuario
    WHERE ID = @IdUsuario AND Activo = 1;
END;
GO
