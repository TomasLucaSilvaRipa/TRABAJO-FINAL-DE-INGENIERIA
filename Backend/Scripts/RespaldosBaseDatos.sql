/*
  Respaldo de la base de datos de TeamBalance.
  Retención operativa: 30 días. La ubicación física se configura en la API y debe ser
  un almacenamiento separado del volumen principal de SQL Server en producción.
*/
GO

IF OBJECT_ID(N'dbo.RespaldoBaseDatos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RespaldoBaseDatos
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RespaldoBaseDatos PRIMARY KEY,
        NombreArchivo NVARCHAR(260) NOT NULL,
        RutaArchivo NVARCHAR(1000) NOT NULL,
        FechaInicio DATETIME2 NOT NULL,
        FechaFin DATETIME2 NULL,
        Estado NVARCHAR(40) NOT NULL CONSTRAINT DF_RespaldoBaseDatos_Estado DEFAULT N'En proceso',
        Verificado BIT NOT NULL CONSTRAINT DF_RespaldoBaseDatos_Verificado DEFAULT 0,
        TamanoBytes BIGINT NULL,
        Mensaje NVARCHAR(2000) NULL,
        IdUsuarioSolicitante INT NULL,
        FechaExpiracion DATETIME2 NOT NULL,
        CONSTRAINT FK_RespaldoBaseDatos_Usuario FOREIGN KEY(IdUsuarioSolicitante) REFERENCES dbo.Usuario(ID)
    );
    CREATE INDEX IX_RespaldoBaseDatos_FechaInicio ON dbo.RespaldoBaseDatos(FechaInicio DESC);
END;
GO

IF OBJECT_ID(N'dbo.PruebaRestauracionRespaldo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PruebaRestauracionRespaldo
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PruebaRestauracionRespaldo PRIMARY KEY,
        IdRespaldo INT NOT NULL,
        BaseDatosDestino NVARCHAR(128) NOT NULL,
        FechaInicio DATETIME2 NOT NULL,
        FechaFin DATETIME2 NULL,
        Estado NVARCHAR(40) NOT NULL CONSTRAINT DF_PruebaRestauracionRespaldo_Estado DEFAULT N'En proceso',
        Mensaje NVARCHAR(2000) NULL,
        IdUsuarioSolicitante INT NULL,
        CONSTRAINT FK_PruebaRestauracionRespaldo_Respaldo FOREIGN KEY(IdRespaldo) REFERENCES dbo.RespaldoBaseDatos(ID),
        CONSTRAINT FK_PruebaRestauracionRespaldo_Usuario FOREIGN KEY(IdUsuarioSolicitante) REFERENCES dbo.Usuario(ID)
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RespaldoBaseDatos_Registrar
    @NombreArchivo NVARCHAR(260),
    @RutaArchivo NVARCHAR(1000),
    @FechaInicio DATETIME2,
    @IdUsuarioSolicitante INT = NULL,
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.RespaldoBaseDatos(NombreArchivo, RutaArchivo, FechaInicio, Estado, IdUsuarioSolicitante, FechaExpiracion)
    VALUES(@NombreArchivo, @RutaArchivo, @FechaInicio, N'En proceso', @IdUsuarioSolicitante, @FechaExpiracion);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RespaldoBaseDatos_Finalizar
    @IdRespaldo INT,
    @FechaFin DATETIME2 = NULL,
    @Estado NVARCHAR(40),
    @Verificado BIT,
    @TamanoBytes BIGINT = NULL,
    @Mensaje NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.RespaldoBaseDatos
    SET FechaFin = @FechaFin, Estado = @Estado, Verificado = @Verificado, TamanoBytes = @TamanoBytes, Mensaje = @Mensaje
    WHERE ID = @IdRespaldo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RespaldoBaseDatos_Consultar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NombreArchivo, RutaArchivo, FechaInicio, FechaFin, Estado, Verificado, TamanoBytes, Mensaje, IdUsuarioSolicitante, FechaExpiracion
    FROM dbo.RespaldoBaseDatos
    ORDER BY FechaInicio DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RespaldoBaseDatos_Eliminar
    @IdRespaldo INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.RespaldoBaseDatos WHERE ID = @IdRespaldo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PruebaRestauracion_Registrar
    @IdRespaldo INT,
    @BaseDatosDestino NVARCHAR(128),
    @FechaInicio DATETIME2,
    @IdUsuarioSolicitante INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.PruebaRestauracionRespaldo(IdRespaldo, BaseDatosDestino, FechaInicio, Estado, IdUsuarioSolicitante)
    VALUES(@IdRespaldo, @BaseDatosDestino, @FechaInicio, N'En proceso', @IdUsuarioSolicitante);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PruebaRestauracion_Finalizar
    @IdPrueba INT,
    @FechaFin DATETIME2 = NULL,
    @Estado NVARCHAR(40),
    @Mensaje NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PruebaRestauracionRespaldo SET FechaFin = @FechaFin, Estado = @Estado, Mensaje = @Mensaje WHERE ID = @IdPrueba;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PruebaRestauracion_Consultar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, IdRespaldo, BaseDatosDestino, FechaInicio, FechaFin, Estado, Mensaje, IdUsuarioSolicitante
    FROM dbo.PruebaRestauracionRespaldo
    ORDER BY FechaInicio DESC;
END;
GO
