/* Encuestas públicas dinámicas: preguntas cerradas, vigencia y resultados graficables. */
GO

IF OBJECT_ID(N'dbo.Encuesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Encuesta
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Encuesta PRIMARY KEY,
        TituloEs NVARCHAR(180) NOT NULL,
        TituloEn NVARCHAR(180) NOT NULL,
        DescripcionEs NVARCHAR(600) NOT NULL,
        DescripcionEn NVARCHAR(600) NOT NULL,
        FechaInicio DATETIME2 NOT NULL,
        FechaVencimiento DATETIME2 NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Encuesta_Activo DEFAULT 1,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_Encuesta_FechaAlta DEFAULT SYSDATETIME(),
        CONSTRAINT CK_Encuesta_Vigencia CHECK (FechaVencimiento > FechaInicio)
    );
END;
GO

IF OBJECT_ID(N'dbo.PreguntaEncuesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PreguntaEncuesta
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreguntaEncuesta PRIMARY KEY,
        IdEncuesta INT NOT NULL,
        EnunciadoEs NVARCHAR(300) NOT NULL,
        EnunciadoEn NVARCHAR(300) NOT NULL,
        Orden INT NOT NULL,
        CONSTRAINT UQ_PreguntaEncuesta_Orden UNIQUE(IdEncuesta, Orden),
        CONSTRAINT FK_PreguntaEncuesta_Encuesta FOREIGN KEY(IdEncuesta) REFERENCES dbo.Encuesta(ID) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.OpcionEncuesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpcionEncuesta
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OpcionEncuesta PRIMARY KEY,
        IdPreguntaEncuesta INT NOT NULL,
        TextoEs NVARCHAR(200) NOT NULL,
        TextoEn NVARCHAR(200) NOT NULL,
        Orden INT NOT NULL,
        CONSTRAINT UQ_OpcionEncuesta_Orden UNIQUE(IdPreguntaEncuesta, Orden),
        CONSTRAINT FK_OpcionEncuesta_Pregunta FOREIGN KEY(IdPreguntaEncuesta) REFERENCES dbo.PreguntaEncuesta(ID) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.RespuestaEncuesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RespuestaEncuesta
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RespuestaEncuesta PRIMARY KEY,
        IdEncuesta INT NOT NULL,
        IdentificadorParticipante NVARCHAR(64) NOT NULL,
        FechaRespuesta DATETIME2 NOT NULL CONSTRAINT DF_RespuestaEncuesta_FechaRespuesta DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_RespuestaEncuesta_Participante UNIQUE(IdEncuesta, IdentificadorParticipante),
        CONSTRAINT FK_RespuestaEncuesta_Encuesta FOREIGN KEY(IdEncuesta) REFERENCES dbo.Encuesta(ID)
    );
END;
GO

IF OBJECT_ID(N'dbo.RespuestaPreguntaEncuesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RespuestaPreguntaEncuesta
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RespuestaPreguntaEncuesta PRIMARY KEY,
        IdRespuestaEncuesta INT NOT NULL,
        IdPreguntaEncuesta INT NOT NULL,
        IdOpcionEncuesta INT NOT NULL,
        CONSTRAINT UQ_RespuestaPreguntaEncuesta UNIQUE(IdRespuestaEncuesta, IdPreguntaEncuesta),
        CONSTRAINT FK_RespuestaPreguntaEncuesta_Respuesta FOREIGN KEY(IdRespuestaEncuesta) REFERENCES dbo.RespuestaEncuesta(ID),
        CONSTRAINT FK_RespuestaPreguntaEncuesta_Pregunta FOREIGN KEY(IdPreguntaEncuesta) REFERENCES dbo.PreguntaEncuesta(ID),
        CONSTRAINT FK_RespuestaPreguntaEncuesta_Opcion FOREIGN KEY(IdOpcionEncuesta) REFERENCES dbo.OpcionEncuesta(ID)
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_ConsultarPublicas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, encuesta.DescripcionEs, encuesta.DescripcionEn, encuesta.FechaInicio, encuesta.FechaVencimiento, encuesta.Activo, COUNT(respuesta.ID) AS CantidadRespuestas
    FROM dbo.Encuesta encuesta
    LEFT JOIN dbo.RespuestaEncuesta respuesta ON respuesta.IdEncuesta = encuesta.ID
    WHERE encuesta.Activo = 1 AND encuesta.FechaInicio <= SYSDATETIME() AND encuesta.FechaVencimiento > SYSDATETIME()
    GROUP BY encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, encuesta.DescripcionEs, encuesta.DescripcionEn, encuesta.FechaInicio, encuesta.FechaVencimiento, encuesta.Activo
    ORDER BY encuesta.FechaVencimiento, encuesta.ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_ConsultarGestion
AS
BEGIN
    SET NOCOUNT ON;
    SELECT encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, encuesta.DescripcionEs, encuesta.DescripcionEn, encuesta.FechaInicio, encuesta.FechaVencimiento, encuesta.Activo, COUNT(respuesta.ID) AS CantidadRespuestas
    FROM dbo.Encuesta encuesta
    LEFT JOIN dbo.RespuestaEncuesta respuesta ON respuesta.IdEncuesta = encuesta.ID
    GROUP BY encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, encuesta.DescripcionEs, encuesta.DescripcionEn, encuesta.FechaInicio, encuesta.FechaVencimiento, encuesta.Activo
    ORDER BY encuesta.Activo DESC, encuesta.FechaVencimiento DESC, encuesta.ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_ConsultarDetallePublica @IdEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, encuesta.DescripcionEs, encuesta.DescripcionEn, encuesta.FechaInicio, encuesta.FechaVencimiento, encuesta.Activo,
           pregunta.ID AS IdPreguntaEncuesta, pregunta.EnunciadoEs, pregunta.EnunciadoEn, pregunta.Orden AS OrdenPregunta,
           opcion.ID AS IdOpcionEncuesta, opcion.TextoEs, opcion.TextoEn, opcion.Orden AS OrdenOpcion
    FROM dbo.Encuesta encuesta
    INNER JOIN dbo.PreguntaEncuesta pregunta ON pregunta.IdEncuesta = encuesta.ID
    INNER JOIN dbo.OpcionEncuesta opcion ON opcion.IdPreguntaEncuesta = pregunta.ID
    WHERE encuesta.ID = @IdEncuesta AND encuesta.Activo = 1 AND encuesta.FechaInicio <= SYSDATETIME() AND encuesta.FechaVencimiento > SYSDATETIME()
    ORDER BY pregunta.Orden, opcion.Orden;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_ConsultarDetalleGestion @IdEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, encuesta.DescripcionEs, encuesta.DescripcionEn, encuesta.FechaInicio, encuesta.FechaVencimiento, encuesta.Activo,
           pregunta.ID AS IdPreguntaEncuesta, pregunta.EnunciadoEs, pregunta.EnunciadoEn, pregunta.Orden AS OrdenPregunta,
           opcion.ID AS IdOpcionEncuesta, opcion.TextoEs, opcion.TextoEn, opcion.Orden AS OrdenOpcion
    FROM dbo.Encuesta encuesta
    INNER JOIN dbo.PreguntaEncuesta pregunta ON pregunta.IdEncuesta = encuesta.ID
    INNER JOIN dbo.OpcionEncuesta opcion ON opcion.IdPreguntaEncuesta = pregunta.ID
    WHERE encuesta.ID = @IdEncuesta
    ORDER BY pregunta.Orden, opcion.Orden;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_Guardar
    @IdEncuesta INT,
    @TituloEs NVARCHAR(180),
    @TituloEn NVARCHAR(180),
    @DescripcionEs NVARCHAR(600),
    @DescripcionEn NVARCHAR(600),
    @FechaInicio DATETIME2,
    @FechaVencimiento DATETIME2,
    @PreguntasJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    IF @IdEncuesta = 0
    BEGIN
        INSERT INTO dbo.Encuesta(TituloEs, TituloEn, DescripcionEs, DescripcionEn, FechaInicio, FechaVencimiento, Activo)
        VALUES(@TituloEs, @TituloEn, @DescripcionEs, @DescripcionEn, @FechaInicio, @FechaVencimiento, 1);
        SET @IdEncuesta = CONVERT(INT, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM dbo.Encuesta WHERE ID = @IdEncuesta) THROW 53001, 'No encontramos la encuesta a modificar.', 1;
        IF EXISTS(SELECT 1 FROM dbo.RespuestaEncuesta WHERE IdEncuesta = @IdEncuesta) THROW 53002, 'No podés modificar una encuesta que ya recibió respuestas. Dala de baja y creá una nueva versión.', 1;
        UPDATE dbo.Encuesta SET TituloEs = @TituloEs, TituloEn = @TituloEn, DescripcionEs = @DescripcionEs, DescripcionEn = @DescripcionEn, FechaInicio = @FechaInicio, FechaVencimiento = @FechaVencimiento, Activo = 1 WHERE ID = @IdEncuesta;
        DELETE FROM dbo.PreguntaEncuesta WHERE IdEncuesta = @IdEncuesta;
    END;

    DECLARE @Preguntas TABLE(EnunciadoEs NVARCHAR(300), EnunciadoEn NVARCHAR(300), Orden INT, OpcionesJson NVARCHAR(MAX));
    INSERT INTO @Preguntas(EnunciadoEs, EnunciadoEn, Orden, OpcionesJson)
    SELECT EnunciadoEs, EnunciadoEn, Orden, Opciones
    FROM OPENJSON(@PreguntasJson)
    WITH(EnunciadoEs NVARCHAR(300) '$.EnunciadoEs', EnunciadoEn NVARCHAR(300) '$.EnunciadoEn', Orden INT '$.Orden', Opciones NVARCHAR(MAX) '$.Opciones' AS JSON);

    INSERT INTO dbo.PreguntaEncuesta(IdEncuesta, EnunciadoEs, EnunciadoEn, Orden)
    SELECT @IdEncuesta, EnunciadoEs, EnunciadoEn, Orden FROM @Preguntas;

    INSERT INTO dbo.OpcionEncuesta(IdPreguntaEncuesta, TextoEs, TextoEn, Orden)
    SELECT pregunta.ID, opcion.TextoEs, opcion.TextoEn, opcion.Orden
    FROM @Preguntas preguntaJson
    INNER JOIN dbo.PreguntaEncuesta pregunta ON pregunta.IdEncuesta = @IdEncuesta AND pregunta.Orden = preguntaJson.Orden
    CROSS APPLY OPENJSON(preguntaJson.OpcionesJson)
    WITH(TextoEs NVARCHAR(200) '$.TextoEs', TextoEn NVARCHAR(200) '$.TextoEn', Orden INT '$.Orden') opcion;

    COMMIT TRANSACTION;
    SELECT @IdEncuesta AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_DarDeBaja @IdEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Encuesta SET Activo = 0 WHERE ID = @IdEncuesta;
    IF @@ROWCOUNT = 0 THROW 53003, 'No encontramos la encuesta a dar de baja.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_Responder
    @IdEncuesta INT,
    @IdentificadorParticipante NVARCHAR(64),
    @RespuestasJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    IF NOT EXISTS(SELECT 1 FROM dbo.Encuesta WHERE ID = @IdEncuesta AND Activo = 1 AND FechaInicio <= SYSDATETIME() AND FechaVencimiento > SYSDATETIME()) THROW 53004, 'La encuesta ya no está disponible.', 1;
    IF EXISTS(SELECT 1 FROM dbo.RespuestaEncuesta WHERE IdEncuesta = @IdEncuesta AND IdentificadorParticipante = @IdentificadorParticipante) THROW 53005, 'Ya registramos una respuesta desde este dispositivo.', 1;

    DECLARE @Respuestas TABLE(IdPreguntaEncuesta INT, IdOpcionEncuesta INT);
    INSERT INTO @Respuestas(IdPreguntaEncuesta, IdOpcionEncuesta)
    SELECT IdPreguntaEncuesta, IdOpcionEncuesta
    FROM OPENJSON(@RespuestasJson)
    WITH(IdPreguntaEncuesta INT '$.IdPreguntaEncuesta', IdOpcionEncuesta INT '$.IdOpcionEncuesta');

    IF (SELECT COUNT(*) FROM @Respuestas) <> (SELECT COUNT(*) FROM dbo.PreguntaEncuesta WHERE IdEncuesta = @IdEncuesta) THROW 53006, 'Respondé todas las preguntas de la encuesta.', 1;
    IF EXISTS(SELECT IdPreguntaEncuesta FROM @Respuestas GROUP BY IdPreguntaEncuesta HAVING COUNT(*) > 1) THROW 53007, 'La encuesta contiene respuestas duplicadas.', 1;
    IF EXISTS(SELECT 1 FROM @Respuestas respuesta LEFT JOIN dbo.OpcionEncuesta opcion ON opcion.ID = respuesta.IdOpcionEncuesta LEFT JOIN dbo.PreguntaEncuesta pregunta ON pregunta.ID = respuesta.IdPreguntaEncuesta WHERE pregunta.ID IS NULL OR opcion.ID IS NULL OR pregunta.IdEncuesta <> @IdEncuesta OR opcion.IdPreguntaEncuesta <> respuesta.IdPreguntaEncuesta) THROW 53008, 'Una de las opciones elegidas no corresponde a la encuesta.', 1;

    INSERT INTO dbo.RespuestaEncuesta(IdEncuesta, IdentificadorParticipante) VALUES(@IdEncuesta, @IdentificadorParticipante);
    DECLARE @IdRespuestaEncuesta INT = CONVERT(INT, SCOPE_IDENTITY());
    INSERT INTO dbo.RespuestaPreguntaEncuesta(IdRespuestaEncuesta, IdPreguntaEncuesta, IdOpcionEncuesta)
    SELECT @IdRespuestaEncuesta, IdPreguntaEncuesta, IdOpcionEncuesta FROM @Respuestas;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Encuesta_ConsultarResultados @IdEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Encuesta WHERE ID = @IdEncuesta) THROW 53009, 'No encontramos la encuesta solicitada.', 1;

    DECLARE @CantidadRespuestasEncuesta INT = (SELECT COUNT(*) FROM dbo.RespuestaEncuesta WHERE IdEncuesta = @IdEncuesta);
    SELECT encuesta.ID AS IdEncuesta, encuesta.TituloEs, encuesta.TituloEn, @CantidadRespuestasEncuesta AS CantidadRespuestasEncuesta,
           pregunta.ID AS IdPreguntaEncuesta, pregunta.EnunciadoEs, pregunta.EnunciadoEn, pregunta.Orden AS OrdenPregunta,
           opcion.ID AS IdOpcionEncuesta, opcion.TextoEs, opcion.TextoEn, opcion.Orden AS OrdenOpcion,
           COUNT(detalle.ID) AS CantidadRespuestas,
           CONVERT(DECIMAL(5,2), CASE WHEN @CantidadRespuestasEncuesta = 0 THEN 0 ELSE COUNT(detalle.ID) * 100.0 / @CantidadRespuestasEncuesta END) AS Porcentaje
    FROM dbo.Encuesta encuesta
    INNER JOIN dbo.PreguntaEncuesta pregunta ON pregunta.IdEncuesta = encuesta.ID
    INNER JOIN dbo.OpcionEncuesta opcion ON opcion.IdPreguntaEncuesta = pregunta.ID
    LEFT JOIN dbo.RespuestaPreguntaEncuesta detalle ON detalle.IdOpcionEncuesta = opcion.ID
    WHERE encuesta.ID = @IdEncuesta
    GROUP BY encuesta.ID, encuesta.TituloEs, encuesta.TituloEn, pregunta.ID, pregunta.EnunciadoEs, pregunta.EnunciadoEn, pregunta.Orden, opcion.ID, opcion.TextoEs, opcion.TextoEn, opcion.Orden
    ORDER BY pregunta.Orden, opcion.Orden;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Encuesta)
BEGIN
    DECLARE @IdEncuestaInicial INT;
    INSERT INTO dbo.Encuesta(TituloEs, TituloEn, DescripcionEs, DescripcionEn, FechaInicio, FechaVencimiento, Activo)
    VALUES(N'Ayudanos a mejorar TeamBalance', N'Help us improve TeamBalance', N'Tu respuesta nos ayuda a priorizar mejoras para agencias y equipos.', N'Your response helps us prioritize improvements for agencies and teams.', DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, 30, SYSDATETIME()), 1);
    SET @IdEncuestaInicial = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.PreguntaEncuesta(IdEncuesta, EnunciadoEs, EnunciadoEn, Orden)
    VALUES(@IdEncuestaInicial, N'¿Qué aspecto te resultaría más valioso mejorar?', N'Which area would be most valuable to improve?', 1),
          (@IdEncuestaInicial, N'¿Qué tan importante sería reducir la cantidad de clics?', N'How important would reducing the number of clicks be?', 2),
          (@IdEncuestaInicial, N'¿Qué funcionalidad te resultaría más útil?', N'Which feature would be most useful to you?', 3);

    DECLARE @PreguntaUno INT = (SELECT ID FROM dbo.PreguntaEncuesta WHERE IdEncuesta = @IdEncuestaInicial AND Orden = 1);
    DECLARE @PreguntaDos INT = (SELECT ID FROM dbo.PreguntaEncuesta WHERE IdEncuesta = @IdEncuestaInicial AND Orden = 2);
    DECLARE @PreguntaTres INT = (SELECT ID FROM dbo.PreguntaEncuesta WHERE IdEncuesta = @IdEncuestaInicial AND Orden = 3);
    INSERT INTO dbo.OpcionEncuesta(IdPreguntaEncuesta, TextoEs, TextoEn, Orden)
    VALUES(@PreguntaUno, N'Facilidad de uso', N'Ease of use', 1), (@PreguntaUno, N'Claridad de las pantallas', N'Interface clarity', 2), (@PreguntaUno, N'Velocidad de la plataforma', N'Platform speed', 3), (@PreguntaUno, N'Organización de la información', N'Information organization', 4),
          (@PreguntaDos, N'Poco importante', N'Not important', 1), (@PreguntaDos, N'Algo importante', N'Somewhat important', 2), (@PreguntaDos, N'Importante', N'Important', 3), (@PreguntaDos, N'Muy importante', N'Very important', 4), (@PreguntaDos, N'Fundamental', N'Essential', 5),
          (@PreguntaTres, N'Más reportes', N'More reports', 1), (@PreguntaTres, N'Más automatización', N'More automation', 2), (@PreguntaTres, N'Más integraciones', N'More integrations', 3), (@PreguntaTres, N'Mejor seguimiento de proyectos', N'Better project tracking', 4);
END;
GO

UPDATE pregunta
SET EnunciadoEs = CASE pregunta.Orden
        WHEN 1 THEN N'¿Qué aspecto te resultaría más valioso mejorar?'
        WHEN 2 THEN N'¿Qué tan importante sería reducir la cantidad de clics?'
        WHEN 3 THEN N'¿Qué funcionalidad te resultaría más útil?'
    END
FROM dbo.PreguntaEncuesta pregunta
INNER JOIN dbo.Encuesta encuesta ON encuesta.ID = pregunta.IdEncuesta
WHERE encuesta.TituloEs = N'Ayudanos a mejorar TeamBalance' AND pregunta.Orden IN (1, 2, 3);

UPDATE opcion
SET TextoEs = CASE pregunta.Orden
        WHEN 1 THEN CASE opcion.Orden WHEN 1 THEN N'Facilidad de uso' WHEN 2 THEN N'Claridad de las pantallas' WHEN 3 THEN N'Velocidad de la plataforma' WHEN 4 THEN N'Organización de la información' END
        WHEN 2 THEN CASE opcion.Orden WHEN 1 THEN N'Poco importante' WHEN 2 THEN N'Algo importante' WHEN 3 THEN N'Importante' WHEN 4 THEN N'Muy importante' WHEN 5 THEN N'Fundamental' END
        WHEN 3 THEN CASE opcion.Orden WHEN 1 THEN N'Más reportes' WHEN 2 THEN N'Más automatización' WHEN 3 THEN N'Más integraciones' WHEN 4 THEN N'Mejor seguimiento de proyectos' END
    END
FROM dbo.OpcionEncuesta opcion
INNER JOIN dbo.PreguntaEncuesta pregunta ON pregunta.ID = opcion.IdPreguntaEncuesta
INNER JOIN dbo.Encuesta encuesta ON encuesta.ID = pregunta.IdEncuesta
WHERE encuesta.TituloEs = N'Ayudanos a mejorar TeamBalance' AND pregunta.Orden IN (1, 2, 3);
GO
