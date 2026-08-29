/*
  Bitácora TeamBalance
  Registrar y consultar eventos de seguridad y actividad operativa.
*/
GO

CREATE OR ALTER PROCEDURE dbo.usp_Bitacora_Registrar
    @IdUsuario INT = NULL,
    @IdAgencia INT = NULL,
    @Entidad NVARCHAR(100) = NULL,
    @IdEntidad INT = NULL,
    @Accion NVARCHAR(100),
    @Mensaje NVARCHAR(1000),
    @Resultado NVARCHAR(50) = NULL,
    @Criticidad NVARCHAR(50) = NULL,
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
        NULLIF(@Resultado, N''),
        NULLIF(@Criticidad, N''),
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
    @Resultado NVARCHAR(50) = NULL,
    @Criticidad NVARCHAR(50) = NULL,
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
        Resultado,
        Criticidad,
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
      AND (NULLIF(@Resultado, N'') IS NULL OR Resultado = @Resultado)
      AND (NULLIF(@Criticidad, N'') IS NULL OR Criticidad = @Criticidad)
      AND (NULLIF(@Modulo, N'') IS NULL OR Modulo = @Modulo)
    ORDER BY FechaHora DESC, ID DESC;
END;
GO
