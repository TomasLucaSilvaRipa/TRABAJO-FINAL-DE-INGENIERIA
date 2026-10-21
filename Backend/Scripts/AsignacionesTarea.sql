/*
   Distribución de trabajo entre empleados.
   AsignacionTarea ya formaba parte del modelo: este script sólo la habilita
   para registrar las horas planificadas de cada Empleado.
*/
IF COL_LENGTH('dbo.AsignacionTarea', 'HorasAsignadas') IS NULL
BEGIN
    ALTER TABLE dbo.AsignacionTarea ADD HorasAsignadas DECIMAL(18,2) NOT NULL CONSTRAINT DF_AsignacionTarea_HorasAsignadas DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AsignacionTarea_TareaEmpleadoActiva' AND object_id = OBJECT_ID('dbo.AsignacionTarea'))
BEGIN
    CREATE INDEX IX_AsignacionTarea_TareaEmpleadoActiva ON dbo.AsignacionTarea(IdTarea, IdEmpleado, Activa);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AsignacionTarea_Consultar
    @IdTarea INT,
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT A.*
    FROM dbo.AsignacionTarea A
    INNER JOIN dbo.Tarea T ON T.ID = A.IdTarea
    INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
    WHERE A.IdTarea = @IdTarea AND P.IdAgencia = @IdAgencia
    ORDER BY A.Activa DESC, A.FechaDesde DESC, A.ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AsignacionTarea_ConsultarActivas
    @IdsTareaJson NVARCHAR(MAX),
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    IF ISJSON(@IdsTareaJson) <> 1 THROW 51030, 'La lista de tareas no es válida.', 1;

    SELECT A.*
    FROM dbo.AsignacionTarea A
    INNER JOIN dbo.Tarea T ON T.ID = A.IdTarea
    INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
    INNER JOIN OPENJSON(@IdsTareaJson) J ON CONVERT(INT, J.[value]) = A.IdTarea
    WHERE P.IdAgencia = @IdAgencia AND A.Activa = 1 AND (A.FechaHasta IS NULL OR A.FechaHasta >= CAST(SYSDATETIME() AS DATE));
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AsignacionTarea_ConsultarCapacidades
    @IdsEmpleadoJson NVARCHAR(MAX),
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    IF ISJSON(@IdsEmpleadoJson) <> 1 THROW 51036, 'La lista de empleados no es válida.', 1;

    SELECT E.ID, CAST(ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS DECIMAL(18,2)) AS HorasDisponiblesSemanales
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    INNER JOIN OPENJSON(@IdsEmpleadoJson) J ON CONVERT(INT, J.[value]) = E.ID
    OUTER APPLY (
        SELECT TOP 1 D.HorasSemanales
        FROM dbo.DisponibilidadBase D
        WHERE D.IdEmpleado = E.ID AND D.Activo = 1
        ORDER BY D.ID DESC
    ) DB
    WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AsignacionTarea_Sincronizar
    @IdTarea INT,
    @IdAgencia INT,
    @IdUsuarioAsignador INT,
    @AsignacionesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISJSON(@AsignacionesJson) <> 1 THROW 51031, 'Las asignaciones enviadas no son válidas.', 1;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Tarea T
        INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
        WHERE T.ID = @IdTarea AND T.Activo = 1 AND P.IdAgencia = @IdAgencia
    ) THROW 51032, 'La tarea no existe o no pertenece a la agencia.', 1;

    DECLARE @HorasTarea DECIMAL(18,2);
    SELECT @HorasTarea = HorasEstimadas FROM dbo.Tarea WHERE ID = @IdTarea;

    DECLARE @Nuevas TABLE (IdEmpleado INT NOT NULL PRIMARY KEY, HorasAsignadas DECIMAL(18,2) NOT NULL, Motivo NVARCHAR(500) NULL);
    INSERT INTO @Nuevas (IdEmpleado, HorasAsignadas, Motivo)
    SELECT IdEmpleado, HorasAsignadas, Motivo
    FROM OPENJSON(@AsignacionesJson)
    -- System.Text.Json serializa las propiedades de esta capa como
    -- IdEmpleado y HorasAsignadas. OPENJSON distingue el nombre indicado
    -- en la ruta, por lo que usar camelCase dejaba la tabla vacía sin error.
    WITH (IdEmpleado INT '$.IdEmpleado', HorasAsignadas DECIMAL(18,2) '$.HorasAsignadas', Motivo NVARCHAR(500) '$.motivo')
    WHERE IdEmpleado IS NOT NULL;

    IF EXISTS (SELECT 1 FROM @Nuevas WHERE HorasAsignadas <= 0) THROW 51033, 'Las horas de cada empleado deben ser mayores a cero.', 1;
    -- La BLL ya normaliza la distribución proporcional y valida el total antes
    -- de llegar a persistencia. No se repite esa comparación aquí: al volver a
    -- leer decimales desde JSON podía bloquear una edición válida por redondeo.
    IF EXISTS (
        SELECT 1 FROM @Nuevas N
        LEFT JOIN dbo.Empleado E ON E.ID = N.IdEmpleado AND E.Activo = 1
        LEFT JOIN dbo.Usuario U ON U.ID = E.IdUsuario AND U.Activo = 1 AND U.IdAgencia = @IdAgencia
        WHERE U.ID IS NULL
    ) THROW 51035, 'Uno de los empleados asignados no pertenece a la agencia o no está activo.', 1;

    BEGIN TRANSACTION;
    UPDATE A SET Activa = 0, FechaHasta = COALESCE(A.FechaHasta, SYSDATETIME())
    FROM dbo.AsignacionTarea A
    WHERE A.IdTarea = @IdTarea AND A.Activa = 1
      AND NOT EXISTS (SELECT 1 FROM @Nuevas N WHERE N.IdEmpleado = A.IdEmpleado);

    UPDATE A SET A.HorasAsignadas = N.HorasAsignadas, A.Motivo = N.Motivo, A.FechaHasta = NULL
    FROM dbo.AsignacionTarea A
    INNER JOIN @Nuevas N ON N.IdEmpleado = A.IdEmpleado
    WHERE A.IdTarea = @IdTarea AND A.Activa = 1;

    INSERT INTO dbo.AsignacionTarea (IdTarea, IdEmpleado, IdUsuarioAsignador, FechaDesde, FechaHasta, Motivo, HorasAsignadas, Activa)
    SELECT @IdTarea, N.IdEmpleado, @IdUsuarioAsignador, SYSDATETIME(), NULL, N.Motivo, N.HorasAsignadas, 1
    FROM @Nuevas N
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea A WHERE A.IdTarea = @IdTarea AND A.IdEmpleado = N.IdEmpleado AND A.Activa = 1);
    COMMIT TRANSACTION;

    EXEC dbo.usp_AsignacionTarea_Consultar @IdTarea = @IdTarea, @IdAgencia = @IdAgencia;
END;
GO
