/* Bandeja interna por usuario. El email se compara completo: no se quitan alias +texto. */
GO
IF OBJECT_ID(N'dbo.NotificacionUsuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificacionUsuario
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificacionUsuario PRIMARY KEY,
        IdUsuario INT NOT NULL,
        Titulo NVARCHAR(220) NOT NULL,
        Mensaje NVARCHAR(1200) NOT NULL,
        Fecha DATETIME2 NOT NULL CONSTRAINT DF_NotificacionUsuario_Fecha DEFAULT SYSDATETIME(),
        Leida BIT NOT NULL CONSTRAINT DF_NotificacionUsuario_Leida DEFAULT 0,
        CONSTRAINT FK_NotificacionUsuario_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID)
    );
    CREATE INDEX IX_NotificacionUsuario_Usuario_Fecha ON dbo.NotificacionUsuario(IdUsuario, Fecha DESC);
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Notificacion_RegistrarPorEmail
    @EmailExacto NVARCHAR(150), @Titulo NVARCHAR(220), @Mensaje NVARCHAR(1200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdUsuario INT;
    SELECT TOP (1) @IdUsuario = ID FROM dbo.Usuario WHERE Email = @EmailExacto AND Activo = 1;
    IF @IdUsuario IS NOT NULL
        INSERT INTO dbo.NotificacionUsuario(IdUsuario, Titulo, Mensaje) VALUES(@IdUsuario, @Titulo, @Mensaje);
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Notificacion_ConsultarPropias @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, IdUsuario, Titulo, Mensaje, Fecha, Leida FROM dbo.NotificacionUsuario WHERE IdUsuario = @IdUsuario ORDER BY Fecha DESC, ID DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Notificacion_MarcarLeida @IdUsuario INT, @IdNotificacion INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.NotificacionUsuario SET Leida = 1 WHERE ID = @IdNotificacion AND IdUsuario = @IdUsuario;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Notificacion_MarcarTodasLeidas @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.NotificacionUsuario SET Leida = 1 WHERE IdUsuario = @IdUsuario AND Leida = 0;
END;
GO
