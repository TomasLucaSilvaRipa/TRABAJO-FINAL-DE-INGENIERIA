/*
  Telegram Bot API: vinculación privada y OTP.
  Script idempotente para la base TeamBalance.
*/
GO

IF OBJECT_ID(N'dbo.UsuarioTelegram', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioTelegram
    (
        IdUsuario INT NOT NULL PRIMARY KEY,
        TelegramUserId BIGINT NOT NULL,
        ChatId BIGINT NOT NULL,
        NombreUsuario NVARCHAR(256) NULL,
        Vinculado BIT NOT NULL CONSTRAINT DF_UsuarioTelegram_Vinculado DEFAULT (1),
        FechaVinculacion DATETIME2 NOT NULL CONSTRAINT DF_UsuarioTelegram_FechaVinculacion DEFAULT (SYSDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_UsuarioTelegram_FechaActualizacion DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_UsuarioTelegram_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT UQ_UsuarioTelegram_TelegramUserId UNIQUE (TelegramUserId),
        CONSTRAINT UQ_UsuarioTelegram_ChatId UNIQUE (ChatId)
    );
END;
GO

-- La relación es única por usuario de TeamBalance (PK IdUsuario), no por cuenta
-- de Telegram. Una misma cuenta de Telegram puede validar usuarios distintos y
-- cada código continúa ligado a la solicitud y al IdUsuario correspondiente.
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_UsuarioTelegram_TelegramUserId' AND parent_object_id = OBJECT_ID(N'dbo.UsuarioTelegram'))
    ALTER TABLE dbo.UsuarioTelegram DROP CONSTRAINT UQ_UsuarioTelegram_TelegramUserId;
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_UsuarioTelegram_ChatId' AND parent_object_id = OBJECT_ID(N'dbo.UsuarioTelegram'))
    ALTER TABLE dbo.UsuarioTelegram DROP CONSTRAINT UQ_UsuarioTelegram_ChatId;
GO

IF OBJECT_ID(N'dbo.SolicitudTelegram', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SolicitudTelegram
    (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdUsuario INT NOT NULL,
        Tipo NVARCHAR(32) NOT NULL,
        TokenHash NVARCHAR(500) NULL,
        Estado NVARCHAR(32) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_SolicitudTelegram_FechaCreacion DEFAULT (SYSDATETIME()),
        FechaExpiracion DATETIME2 NOT NULL,
        TelegramUserId BIGINT NULL,
        ChatId BIGINT NULL,
        CodigoHash NVARCHAR(500) NULL,
        FechaExpiracionCodigo DATETIME2 NULL,
        IntentosFallidos INT NOT NULL CONSTRAINT DF_SolicitudTelegram_Intentos DEFAULT (0),
        Reenvios INT NOT NULL CONSTRAINT DF_SolicitudTelegram_Reenvios DEFAULT (0),
        Activa BIT NOT NULL CONSTRAINT DF_SolicitudTelegram_Activa DEFAULT (1),
        CONSTRAINT FK_SolicitudTelegram_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT UQ_SolicitudTelegram_TokenHash UNIQUE (TokenHash)
    );
    CREATE INDEX IX_SolicitudTelegram_UsuarioActiva ON dbo.SolicitudTelegram (IdUsuario, Tipo, Activa);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Telegram_ConsultarEstado
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        CONVERT(bit, CASE WHEN u.IdUsuario IS NOT NULL AND u.Vinculado = 1 THEN 1 ELSE 0 END) AS Vinculado,
        s.Estado AS EstadoSolicitud, s.FechaExpiracion AS FechaExpiracionSolicitud, u.FechaVinculacion
    FROM (SELECT @IdUsuario AS IdUsuario) base
    LEFT JOIN dbo.UsuarioTelegram u ON u.IdUsuario = base.IdUsuario
    OUTER APPLY (SELECT TOP (1) Estado, FechaExpiracion FROM dbo.SolicitudTelegram WHERE IdUsuario = @IdUsuario AND Activa = 1 ORDER BY ID DESC) s;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Telegram_CrearSolicitud
    @IdUsuario INT,
    @TokenHash NVARCHAR(500),
    @Tipo NVARCHAR(32),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.SolicitudTelegram SET Activa = 0 WHERE IdUsuario = @IdUsuario AND Tipo = @Tipo AND Activa = 1;
    INSERT INTO dbo.SolicitudTelegram (IdUsuario, Tipo, TokenHash, Estado, FechaExpiracion, Activa)
    VALUES (@IdUsuario, @Tipo, @TokenHash, N'PendienteInicio', @FechaExpiracion, 1);
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Telegram_ProcesarInicio
    @TokenHash NVARCHAR(500),
    @TelegramUserId BIGINT,
    @ChatId BIGINT,
    @NombreUsuario NVARCHAR(256) = NULL,
    @CodigoHash NVARCHAR(500),
    @FechaExpiracionCodigo DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdUsuario INT;
    BEGIN TRANSACTION;
    SELECT TOP (1) @IdUsuario = IdUsuario
    FROM dbo.SolicitudTelegram WITH (UPDLOCK, HOLDLOCK)
    WHERE TokenHash = @TokenHash AND Tipo IN (N'Vinculacion', N'Registro') AND Estado = N'PendienteInicio' AND Activa = 1 AND FechaExpiracion >= SYSDATETIME();
    IF @IdUsuario IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT CAST(0 AS bit) AS Valida, CAST(NULL AS BIGINT) AS ChatId;
        RETURN;
    END;
    DECLARE @Tipo NVARCHAR(32);
    SELECT @Tipo = Tipo FROM dbo.SolicitudTelegram WHERE IdUsuario = @IdUsuario AND TokenHash = @TokenHash AND Activa = 1;
    UPDATE dbo.SolicitudTelegram
    SET Estado = N'CodigoEnviado', TelegramUserId = @TelegramUserId, ChatId = @ChatId, CodigoHash = @CodigoHash,
        FechaExpiracionCodigo = @FechaExpiracionCodigo, IntentosFallidos = 0, Reenvios = 0
    WHERE IdUsuario = @IdUsuario AND Tipo = @Tipo AND Activa = 1;
    COMMIT TRANSACTION;
    SELECT CAST(1 AS bit) AS Valida, @ChatId AS ChatId, @Tipo AS Tipo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Telegram_ConfirmarCodigo
    @IdUsuario INT,
    @Tipo NVARCHAR(32),
    @CodigoHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ID INT, @TelegramUserId BIGINT, @ChatId BIGINT, @Intentos INT;
    BEGIN TRANSACTION;
    SELECT TOP (1) @ID = ID, @TelegramUserId = TelegramUserId, @ChatId = ChatId, @Intentos = IntentosFallidos
    FROM dbo.SolicitudTelegram WITH (UPDLOCK, HOLDLOCK)
    WHERE IdUsuario = @IdUsuario AND Tipo = @Tipo AND Estado = N'CodigoEnviado' AND Activa = 1 AND FechaExpiracionCodigo >= SYSDATETIME();
    IF @ID IS NULL
    BEGIN ROLLBACK TRANSACTION; SELECT CAST(0 AS bit) AS Confirmada; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM dbo.SolicitudTelegram WHERE ID = @ID AND CodigoHash = @CodigoHash)
    BEGIN
        UPDATE dbo.SolicitudTelegram SET IntentosFallidos = IntentosFallidos + 1, Activa = CASE WHEN @Intentos + 1 >= 5 THEN 0 ELSE 1 END WHERE ID = @ID;
        COMMIT TRANSACTION; SELECT CAST(0 AS bit) AS Confirmada; RETURN;
    END;
    IF @Tipo IN (N'Vinculacion', N'Registro')
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.UsuarioTelegram WHERE IdUsuario = @IdUsuario)
            UPDATE dbo.UsuarioTelegram SET TelegramUserId = @TelegramUserId, ChatId = @ChatId, Vinculado = 1, FechaVinculacion = SYSDATETIME(), FechaActualizacion = SYSDATETIME() WHERE IdUsuario = @IdUsuario;
        ELSE
            INSERT INTO dbo.UsuarioTelegram (IdUsuario, TelegramUserId, ChatId, Vinculado) VALUES (@IdUsuario, @TelegramUserId, @ChatId, 1);
        IF @Tipo = N'Registro'
            UPDATE dbo.Usuario SET Estado = N'Activo' WHERE ID = @IdUsuario AND Estado = N'PendienteValidacion';
    END;
    ELSE IF @Tipo = N'Desvinculacion'
        DELETE FROM dbo.UsuarioTelegram WHERE IdUsuario = @IdUsuario;
    UPDATE dbo.SolicitudTelegram SET Estado = N'Confirmada', Activa = 0 WHERE ID = @ID;
    COMMIT TRANSACTION;
    SELECT CAST(1 AS bit) AS Confirmada;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Telegram_ReenviarCodigo
    @IdUsuario INT,
    @Tipo NVARCHAR(32),
    @CodigoHash NVARCHAR(500),
    @FechaExpiracionCodigo DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ID INT, @ChatId BIGINT, @Reenvios INT;
    BEGIN TRANSACTION;
    SELECT TOP (1) @ID = ID, @ChatId = ChatId, @Reenvios = Reenvios FROM dbo.SolicitudTelegram WITH (UPDLOCK, HOLDLOCK)
    WHERE IdUsuario = @IdUsuario AND Tipo = @Tipo AND Estado = N'CodigoEnviado' AND Activa = 1 AND ChatId IS NOT NULL;
    IF @ID IS NULL OR @Reenvios >= 3
    BEGIN ROLLBACK TRANSACTION; SELECT CAST(0 AS bit) AS Valida, CAST(NULL AS BIGINT) AS ChatId; RETURN; END;
    UPDATE dbo.SolicitudTelegram SET CodigoHash = @CodigoHash, FechaExpiracionCodigo = @FechaExpiracionCodigo, IntentosFallidos = 0, Reenvios = Reenvios + 1 WHERE ID = @ID;
    COMMIT TRANSACTION;
    SELECT CAST(1 AS bit) AS Valida, @ChatId AS ChatId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Telegram_CrearDesvinculacion
    @IdUsuario INT,
    @CodigoHash NVARCHAR(500),
    @FechaExpiracionCodigo DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ChatId BIGINT;
    SELECT @ChatId = ChatId FROM dbo.UsuarioTelegram WHERE IdUsuario = @IdUsuario AND Vinculado = 1;
    IF @ChatId IS NULL BEGIN SELECT CAST(0 AS bit) AS Valida, CAST(NULL AS BIGINT) AS ChatId; RETURN; END;
    BEGIN TRANSACTION;
    UPDATE dbo.SolicitudTelegram SET Activa = 0 WHERE IdUsuario = @IdUsuario AND Tipo = N'Desvinculacion' AND Activa = 1;
    INSERT INTO dbo.SolicitudTelegram (IdUsuario, Tipo, Estado, FechaExpiracion, ChatId, CodigoHash, FechaExpiracionCodigo, Activa)
    VALUES (@IdUsuario, N'Desvinculacion', N'CodigoEnviado', @FechaExpiracionCodigo, @ChatId, @CodigoHash, @FechaExpiracionCodigo, 1);
    COMMIT TRANSACTION;
    SELECT CAST(1 AS bit) AS Valida, @ChatId AS ChatId;
END;
GO
