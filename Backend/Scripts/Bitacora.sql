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

IF COL_LENGTH(N'dbo.Bitacora', N'ModuloCodigo') IS NULL
    ALTER TABLE dbo.Bitacora ADD ModuloCodigo INT NULL;
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
    END,
    ModuloCodigo = CASE
        WHEN Modulo = N'General' THEN 1
        WHEN Modulo IN (N'Contratacion', N'Contratación') THEN 2
        WHEN Modulo = N'FAQs' THEN 3
        WHEN Modulo = N'HelpDesk' THEN 4
        WHEN Modulo = N'Kanban' THEN 5
        WHEN Modulo = N'Novedades' THEN 6
        WHEN Modulo = N'Planes' THEN 7
        WHEN Modulo IN (N'Planificacion', N'Planificación') THEN 8
        WHEN Modulo = N'Proyectos' THEN 9
        WHEN Modulo = N'Recursos' THEN 10
        WHEN Modulo = N'Registro' THEN 11
        WHEN Modulo = N'Respaldos' THEN 12
        WHEN Modulo = N'Seguridad' THEN 13
        WHEN Modulo IN (N'Suscripcion', N'Suscripción') THEN 14
        WHEN Modulo = N'Usuarios' THEN 15
        WHEN Modulo = N'Operadores' THEN 16
        WHEN Modulo = N'Encuestas' THEN 17
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
    @ModuloCodigo INT = 1,
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
        ModuloCodigo,
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
        COALESCE(@ModuloCodigo, 1),
        CASE COALESCE(@ModuloCodigo, 1) WHEN 1 THEN N'General' WHEN 2 THEN N'Contratacion' WHEN 3 THEN N'FAQs' WHEN 4 THEN N'HelpDesk' WHEN 5 THEN N'Kanban' WHEN 6 THEN N'Novedades' WHEN 7 THEN N'Planes' WHEN 8 THEN N'Planificación' WHEN 9 THEN N'Proyectos' WHEN 10 THEN N'Recursos' WHEN 11 THEN N'Registro' WHEN 12 THEN N'Respaldos' WHEN 13 THEN N'Seguridad' WHEN 14 THEN N'Suscripción' WHEN 15 THEN N'Usuarios' WHEN 16 THEN N'Operadores' WHEN 17 THEN N'Encuestas' ELSE N'General' END,
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
    @ModuloCodigo INT = NULL
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
        ModuloCodigo,
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
      AND (@ModuloCodigo IS NULL OR ModuloCodigo = @ModuloCodigo)
    ORDER BY FechaHora DESC, ID DESC;
END;
GO
