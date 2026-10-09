/*
  Bitácora TeamBalance
  Registrar y consultar eventos de seguridad y actividad operativa.
*/
GO

IF COL_LENGTH(N'dbo.Bitacora', N'ResultadoCodigo') IS NULL
    ALTER TABLE dbo.Bitacora ADD ResultadoCodigo INT NULL;
GO

IF COL_LENGTH(N'dbo.Bitacora', N'CriticidadCodigo') IS NULL
    ALTER TABLE dbo.Bitacora ADD CriticidadCodigo INT NULL;
GO

UPDATE dbo.Bitacora
SET ResultadoCodigo = CASE
        WHEN Resultado = N'Pendiente' THEN 1
        WHEN Resultado = N'Exitoso' THEN 2
        WHEN Resultado = N'Parcial' THEN 3
        WHEN Resultado = N'Denegado' THEN 4
        WHEN Resultado IN (N'Error', N'Fallido') THEN 5
        ELSE NULL
    END,
    CriticidadCodigo = CASE
        WHEN Criticidad IN (N'Informacion', N'Información') THEN 1
        WHEN Criticidad = N'Advertencia' THEN 2
        WHEN Criticidad IN (N'Critico', N'Crítico') THEN 3
        ELSE 1
    END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Bitacora_Registrar
    @IdUsuario INT = NULL,
    @IdAgencia INT = NULL,
    @Entidad NVARCHAR(100) = NULL,
    @IdEntidad INT = NULL,
    @Accion NVARCHAR(100),
    @Mensaje NVARCHAR(1000),
    @ResultadoCodigo INT = NULL,
    @CriticidadCodigo INT = 1,
    @Modulo NVARCHAR(100) = NULL,
    @FechaHora DATETIME2(0) = NULL,
    @DireccionIP NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Bitacora
    (
        IdUsuario,
        IdAgencia,
        Entidad,
        IdEntidad,
        Accion,
        Mensaje,
        Resultado,
        Criticidad,
        ResultadoCodigo,
        CriticidadCodigo,
        Modulo,
        FechaHora,
        DireccionIP
    )
    VALUES
    (
        @IdUsuario,
        @IdAgencia,
        NULLIF(@Entidad, N''),
        @IdEntidad,
        @Accion,
        @Mensaje,
        CASE @ResultadoCodigo WHEN 1 THEN N'Pendiente' WHEN 2 THEN N'Exitoso' WHEN 3 THEN N'Parcial' WHEN 4 THEN N'Denegado' WHEN 5 THEN N'Error' ELSE NULL END,
        CASE @CriticidadCodigo WHEN 1 THEN N'Informacion' WHEN 2 THEN N'Advertencia' WHEN 3 THEN N'Critico' ELSE N'Informacion' END,
        @ResultadoCodigo,
        COALESCE(@CriticidadCodigo, 1),
        NULLIF(@Modulo, N''),
        COALESCE(@FechaHora, SYSDATETIME()),
        NULLIF(@DireccionIP, N'')
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Bitacora_Consultar
    @IdAgencia INT = NULL,
    @Desde DATETIME2(0) = NULL,
    @Hasta DATETIME2(0) = NULL,
    @IdUsuario INT = NULL,
    @Entidad NVARCHAR(100) = NULL,
    @Accion NVARCHAR(100) = NULL,
    @ResultadoCodigo INT = NULL,
    @CriticidadCodigo INT = NULL,
    @Modulo NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ID,
        IdUsuario,
        IdAgencia,
        Entidad,
        IdEntidad,
        Accion,
        Mensaje,
        ResultadoCodigo,
        CriticidadCodigo,
        Modulo,
        FechaHora,
        DireccionIP
    FROM dbo.Bitacora
    WHERE (@IdAgencia IS NULL OR IdAgencia = @IdAgencia)
      AND (@Desde IS NULL OR FechaHora >= @Desde)
      AND (@Hasta IS NULL OR FechaHora <= @Hasta)
      AND (@IdUsuario IS NULL OR IdUsuario = @IdUsuario)
      AND (NULLIF(@Entidad, N'') IS NULL OR Entidad = @Entidad)
      AND (NULLIF(@Accion, N'') IS NULL OR Accion = @Accion)
      AND (@ResultadoCodigo IS NULL OR ResultadoCodigo = @ResultadoCodigo)
      AND (@CriticidadCodigo IS NULL OR CriticidadCodigo = @CriticidadCodigo)
      AND (NULLIF(@Modulo, N'') IS NULL OR Modulo = @Modulo)
    ORDER BY FechaHora DESC, ID DESC;
END;
GO
