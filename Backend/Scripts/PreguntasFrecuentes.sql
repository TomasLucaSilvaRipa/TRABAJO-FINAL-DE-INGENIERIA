IF OBJECT_ID(N'dbo.PreguntaFrecuente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PreguntaFrecuente
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreguntaFrecuente PRIMARY KEY,
        Categoria NVARCHAR(80) NOT NULL,
        PreguntaEs NVARCHAR(300) NOT NULL,
        RespuestaEs NVARCHAR(4000) NOT NULL,
        PreguntaEn NVARCHAR(300) NOT NULL,
        RespuestaEn NVARCHAR(4000) NOT NULL,
        Orden INT NOT NULL CONSTRAINT DF_PreguntaFrecuente_Orden DEFAULT(0),
        Activo BIT NOT NULL CONSTRAINT DF_PreguntaFrecuente_Activo DEFAULT(1),
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_PreguntaFrecuente_FechaAlta DEFAULT(SYSDATETIME()),
        FechaModificacion DATETIME2 NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.PreguntaFrecuente', N'CategoriaCodigo') IS NULL
    ALTER TABLE dbo.PreguntaFrecuente ADD CategoriaCodigo INT NOT NULL CONSTRAINT DF_PreguntaFrecuente_CategoriaCodigo DEFAULT(5);
GO

UPDATE dbo.PreguntaFrecuente
SET CategoriaCodigo = CASE
    WHEN Categoria LIKE N'Primeros%' THEN 1
    WHEN Categoria LIKE N'Proyectos%' THEN 2
    WHEN Categoria LIKE N'Equipo%' THEN 3
    WHEN Categoria LIKE N'Cuenta%' THEN 4
    ELSE 5
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PreguntaFrecuente_ConsultarPublicas
AS
BEGIN
    SELECT ID, CategoriaCodigo AS Categoria, PreguntaEs, RespuestaEs, PreguntaEn, RespuestaEn, Orden, Activo
    FROM dbo.PreguntaFrecuente
    WHERE Activo = 1
    ORDER BY Categoria, Orden, ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PreguntaFrecuente_ConsultarGestion
AS
BEGIN
    SELECT ID, CategoriaCodigo AS Categoria, PreguntaEs, RespuestaEs, PreguntaEn, RespuestaEn, Orden, Activo
    FROM dbo.PreguntaFrecuente
    ORDER BY Activo DESC, Categoria, Orden, ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PreguntaFrecuente_Guardar
    @IdPreguntaFrecuente INT,
    @CategoriaCodigo INT,
    @PreguntaEs NVARCHAR(300),
    @RespuestaEs NVARCHAR(4000),
    @PreguntaEn NVARCHAR(300),
    @RespuestaEn NVARCHAR(4000),
    @Orden INT
AS
BEGIN
    IF @IdPreguntaFrecuente = 0
    BEGIN
        INSERT INTO dbo.PreguntaFrecuente(Categoria, CategoriaCodigo, PreguntaEs, RespuestaEs, PreguntaEn, RespuestaEn, Orden, Activo)
        VALUES(N'', @CategoriaCodigo, @PreguntaEs, @RespuestaEs, @PreguntaEn, @RespuestaEn, @Orden, 1);
        SET @IdPreguntaFrecuente = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.PreguntaFrecuente
        SET CategoriaCodigo = @CategoriaCodigo, PreguntaEs = @PreguntaEs, RespuestaEs = @RespuestaEs, PreguntaEn = @PreguntaEn, RespuestaEn = @RespuestaEn, Orden = @Orden, Activo = 1, FechaModificacion = SYSDATETIME()
        WHERE ID = @IdPreguntaFrecuente;
    END;

    SELECT ID, CategoriaCodigo AS Categoria, PreguntaEs, RespuestaEs, PreguntaEn, RespuestaEn, Orden, Activo
    FROM dbo.PreguntaFrecuente
    WHERE ID = @IdPreguntaFrecuente;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PreguntaFrecuente_DarDeBaja
    @IdPreguntaFrecuente INT
AS
BEGIN
    UPDATE dbo.PreguntaFrecuente SET Activo = 0, FechaModificacion = SYSDATETIME() WHERE ID = @IdPreguntaFrecuente;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.PreguntaFrecuente)
BEGIN
    INSERT INTO dbo.PreguntaFrecuente(Categoria, CategoriaCodigo, PreguntaEs, RespuestaEs, PreguntaEn, RespuestaEn, Orden)
    VALUES
    (N'Primeros pasos', 1, N'¿Cómo sé qué opciones puedo usar?', N'Las opciones disponibles dependen de tu rol y de los permisos asignados. Si necesitás una función que no aparece, consultá a la persona responsable de tu agencia.', N'How do I know which options I can use?', N'The available options depend on your role and assigned permissions. If you need a missing feature, contact the person responsible for your agency.', 10),
    (N'Proyectos y tareas', 2, N'¿Cómo creo y planifico una tarea?', N'Desde Proyectos, el PM carga los datos de la tarea y puede asignar un responsable o pedir una sugerencia mediante Best Fit.', N'How do I create and plan a task?', N'From Projects, the PM enters task data and can assign a person or request a Best Fit suggestion.', 20),
    (N'Equipo y disponibilidad', 3, N'¿Cómo informo una ausencia o cambio de disponibilidad?', N'Desde Mi disponibilidad podés mantener tu horario base y registrar licencias o ausencias cuando corresponda.', N'How do I report an absence or availability change?', N'From My availability you can maintain your base schedule and register leaves or absences when needed.', 30),
    (N'Cuenta y suscripción', 4, N'¿Cómo gestiono la suscripción de mi agencia?', N'El Dueño consulta el plan vigente, historial y renovación desde Configuración de agencia > Suscripción.', N'How do I manage my agency subscription?', N'The Owner can check the current plan, history and renewal under Agency settings > Subscription.', 40);
END;
GO

UPDATE dbo.PreguntaFrecuente
SET CategoriaCodigo = 1,
    PreguntaEs = N'¿Cómo sé qué opciones puedo usar?',
    RespuestaEs = N'Las opciones disponibles dependen de tu rol y de los permisos asignados. Si necesitás una función que no aparece, consultá a la persona responsable de tu agencia.'
WHERE ID = 1 AND PreguntaEn = N'How do I know which options I can use?';

UPDATE dbo.PreguntaFrecuente
SET CategoriaCodigo = 2,
    PreguntaEs = N'¿Cómo creo y planifico una tarea?',
    RespuestaEs = N'Desde Proyectos, el PM carga los datos de la tarea y puede asignar un responsable o pedir una sugerencia mediante Best Fit.'
WHERE ID = 2 AND PreguntaEn = N'How do I create and plan a task?';

UPDATE dbo.PreguntaFrecuente
SET CategoriaCodigo = 3,
    PreguntaEs = N'¿Cómo informo una ausencia o cambio de disponibilidad?',
    RespuestaEs = N'Desde Mi disponibilidad podés mantener tu horario base y registrar licencias o ausencias cuando corresponda.'
WHERE ID = 3 AND PreguntaEn = N'How do I report an absence or availability change?';

UPDATE dbo.PreguntaFrecuente
SET CategoriaCodigo = 4,
    PreguntaEs = N'¿Cómo gestiono la suscripción de mi agencia?',
    RespuestaEs = N'El Dueño consulta el plan vigente, historial y renovación desde Configuración de agencia > Suscripción.'
WHERE ID = 4 AND PreguntaEn = N'How do I manage my agency subscription?';
GO
