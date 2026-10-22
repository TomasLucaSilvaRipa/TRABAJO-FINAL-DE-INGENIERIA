/*
  Autenticación TOTP (Google Authenticator) y desafíos de acceso.
  Script idempotente; ejecutar una sola vez sobre TeamBalance.
*/
GO

IF OBJECT_ID(N'dbo.UsuarioAutenticacionDosFactores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioAutenticacionDosFactores
    (
        IdUsuario INT NOT NULL PRIMARY KEY,
        SecretoTotpCifrado NVARCHAR(1000) NULL,
        SecretoPendienteCifrado NVARCHAR(1000) NULL,
        FechaExpiracionPendiente DATETIME2 NULL,
        IntentosPendientes INT NOT NULL CONSTRAINT DF_Usuario2FA_IntentosPendientes DEFAULT (0),
        TotpHabilitado BIT NOT NULL CONSTRAINT DF_Usuario2FA_TotpHabilitado DEFAULT (0),
        FechaActivacionTotp DATETIME2 NULL,
        UltimoPasoTotp BIGINT NULL,
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_Usuario2FA_FechaActualizacion DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Usuario2FA_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID)
    );
END;
GO

IF OBJECT_ID(N'dbo.CodigoRecuperacionDosFactores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CodigoRecuperacionDosFactores
    (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdUsuario INT NOT NULL,
        CodigoHash NVARCHAR(500) NOT NULL,
        FechaGeneracion DATETIME2 NOT NULL,
        FechaUtilizacion DATETIME2 NULL,
        Activo BIT NOT NULL CONSTRAINT DF_CodigoRecuperacion2FA_Activo DEFAULT (1),
        CONSTRAINT FK_CodigoRecuperacion2FA_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT UQ_CodigoRecuperacion2FA_Hash UNIQUE (CodigoHash)
    );
END;
GO

IF OBJECT_ID(N'dbo.DesafioAutenticacionDosFactores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DesafioAutenticacionDosFactores
    (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdUsuario INT NOT NULL,
        TokenHash NVARCHAR(500) NOT NULL,
        MantenerSesion BIT NOT NULL,
        FechaGeneracion DATETIME2 NOT NULL,
        FechaExpiracion DATETIME2 NOT NULL,
        IntentosFallidos INT NOT NULL CONSTRAINT DF_Desafio2FA_Intentos DEFAULT (0),
        Completado BIT NOT NULL CONSTRAINT DF_Desafio2FA_Completado DEFAULT (0),
        Activo BIT NOT NULL CONSTRAINT DF_Desafio2FA_Activo DEFAULT (1),
        CONSTRAINT FK_Desafio2FA_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT UQ_Desafio2FA_TokenHash UNIQUE (TokenHash)
    );
END;
GO

IF TYPE_ID(N'dbo.ListaTexto') IS NULL
BEGIN
    EXEC(N'CREATE TYPE dbo.ListaTexto AS TABLE (Valor NVARCHAR(500) NOT NULL);');
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_Consultar
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdUsuario, SecretoTotpCifrado, SecretoPendienteCifrado, FechaExpiracionPendiente,
           IntentosPendientes, TotpHabilitado, FechaActivacionTotp, UltimoPasoTotp
    FROM dbo.UsuarioAutenticacionDosFactores
    WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarPorId
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ID, IdAgencia, IdRol, Nombre, Apellido, Email, PasswordHash, Estado, FechaAlta, Activo, FechaBaja
    FROM dbo.Usuario WHERE ID = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_GuardarPendiente
    @IdUsuario INT,
    @SecretoPendienteCifrado NVARCHAR(1000),
    @FechaExpiracionPendiente DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    MERGE dbo.UsuarioAutenticacionDosFactores AS destino
    USING (SELECT @IdUsuario AS IdUsuario) AS origen ON destino.IdUsuario = origen.IdUsuario
    WHEN MATCHED THEN UPDATE SET SecretoPendienteCifrado = @SecretoPendienteCifrado, FechaExpiracionPendiente = @FechaExpiracionPendiente, IntentosPendientes = 0, FechaActualizacion = SYSDATETIME()
    WHEN NOT MATCHED THEN INSERT (IdUsuario, SecretoPendienteCifrado, FechaExpiracionPendiente, IntentosPendientes, TotpHabilitado, FechaActualizacion)
        VALUES (@IdUsuario, @SecretoPendienteCifrado, @FechaExpiracionPendiente, 0, 0, SYSDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_Activar
    @IdUsuario INT,
    @SecretoTotpCifrado NVARCHAR(1000),
    @PasoTotp BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UsuarioAutenticacionDosFactores
    SET SecretoTotpCifrado = @SecretoTotpCifrado, SecretoPendienteCifrado = NULL,
        FechaExpiracionPendiente = NULL, IntentosPendientes = 0, TotpHabilitado = 1,
        FechaActivacionTotp = SYSDATETIME(), UltimoPasoTotp = @PasoTotp, FechaActualizacion = SYSDATETIME()
    WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_RegistrarIntentoPendiente
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UsuarioAutenticacionDosFactores SET IntentosPendientes = IntentosPendientes + 1, FechaActualizacion = SYSDATETIME() WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_Desactivar
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.UsuarioAutenticacionDosFactores SET SecretoTotpCifrado = NULL, SecretoPendienteCifrado = NULL, FechaExpiracionPendiente = NULL, IntentosPendientes = 0, TotpHabilitado = 0, FechaActivacionTotp = NULL, UltimoPasoTotp = NULL, FechaActualizacion = SYSDATETIME() WHERE IdUsuario = @IdUsuario;
    UPDATE dbo.CodigoRecuperacionDosFactores SET Activo = 0 WHERE IdUsuario = @IdUsuario AND Activo = 1;
    UPDATE dbo.DesafioAutenticacionDosFactores SET Activo = 0 WHERE IdUsuario = @IdUsuario AND Activo = 1;
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_ActualizarUltimoPaso
    @IdUsuario INT,
    @PasoTotp BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UsuarioAutenticacionDosFactores SET UltimoPasoTotp = @PasoTotp, FechaActualizacion = SYSDATETIME() WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario2FA_ReemplazarCodigosRecuperacion
    @IdUsuario INT,
    @Codigos dbo.ListaTexto READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.CodigoRecuperacionDosFactores SET Activo = 0 WHERE IdUsuario = @IdUsuario AND Activo = 1;
    INSERT INTO dbo.CodigoRecuperacionDosFactores (IdUsuario, CodigoHash, FechaGeneracion, Activo)
    SELECT @IdUsuario, Valor, SYSDATETIME(), 1 FROM @Codigos;
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Desafio2FA_Crear
    @IdUsuario INT,
    @TokenHash NVARCHAR(500),
    @MantenerSesion BIT,
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.DesafioAutenticacionDosFactores SET Activo = 0 WHERE IdUsuario = @IdUsuario AND Activo = 1;
    INSERT INTO dbo.DesafioAutenticacionDosFactores (IdUsuario, TokenHash, MantenerSesion, FechaGeneracion, FechaExpiracion, IntentosFallidos, Completado, Activo)
    VALUES (@IdUsuario, @TokenHash, @MantenerSesion, SYSDATETIME(), @FechaExpiracion, 0, 0, 1);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CodigoRecuperacion2FA_Consumir
    @IdUsuario INT,
    @CodigoHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CodigoRecuperacionDosFactores
    SET Activo = 0, FechaUtilizacion = SYSDATETIME()
    WHERE IdUsuario = @IdUsuario AND CodigoHash = @CodigoHash AND Activo = 1 AND FechaUtilizacion IS NULL;
    SELECT CONVERT(bit, CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END) AS Consumido;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Desafio2FA_Consumir
    @TokenHash NVARCHAR(500),
    @Exitoso BIT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdUsuario INT, @MantenerSesion BIT, @Intentos INT;
    SELECT TOP (1) @IdUsuario = IdUsuario, @MantenerSesion = MantenerSesion, @Intentos = IntentosFallidos
    FROM dbo.DesafioAutenticacionDosFactores WITH (UPDLOCK, HOLDLOCK)
    WHERE TokenHash = @TokenHash AND Activo = 1 AND Completado = 0 AND FechaExpiracion >= SYSDATETIME();
    IF @IdUsuario IS NULL BEGIN SELECT CAST(0 AS bit) AS Valido, CAST(NULL AS int) AS IdUsuario, CAST(NULL AS bit) AS MantenerSesion; RETURN; END;
    IF @Exitoso = 1
    BEGIN UPDATE dbo.DesafioAutenticacionDosFactores SET Completado = 1, Activo = 0 WHERE TokenHash = @TokenHash; SELECT CAST(1 AS bit) AS Valido, @IdUsuario AS IdUsuario, @MantenerSesion AS MantenerSesion; RETURN; END;
    UPDATE dbo.DesafioAutenticacionDosFactores SET IntentosFallidos = IntentosFallidos + 1, Activo = CASE WHEN @Intentos + 1 >= 5 THEN 0 ELSE 1 END WHERE TokenHash = @TokenHash;
    SELECT CAST(1 AS bit) AS Valido, @IdUsuario AS IdUsuario, @MantenerSesion AS MantenerSesion;
END;
GO
