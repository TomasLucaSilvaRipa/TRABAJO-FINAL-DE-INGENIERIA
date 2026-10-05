CREATE OR ALTER PROCEDURE dbo.usp_RegistroHora_Registrar @IdTarea INT, @IdUsuario INT, @Fecha DATETIME2, @CantidadHoras DECIMAL(18,2), @Descripcion NVARCHAR(500) = NULL AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdEmpleado INT = (SELECT E.ID FROM dbo.Empleado E WHERE E.IdUsuario = @IdUsuario AND E.Activo = 1);
    IF @IdEmpleado IS NULL THROW 50020, 'Tu usuario no tiene un perfil de empleado activo.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Tarea WHERE ID = @IdTarea AND IdEmpleadoAsignado = @IdEmpleado AND Activo = 1) THROW 50021, 'La tarea seleccionada no está asignada a tu usuario.', 1;
    INSERT INTO dbo.RegistroHora(IdTarea, IdEmpleado, Fecha, CantidadHoras, Descripcion, Activo) VALUES(@IdTarea, @IdEmpleado, @Fecha, @CantidadHoras, @Descripcion, 1);
    SELECT ID, IdTarea, IdEmpleado, Fecha, CantidadHoras, Descripcion, Activo FROM dbo.RegistroHora WHERE ID = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistroHora_ConsultarPropios @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT RH.ID, RH.IdTarea, RH.IdEmpleado, RH.Fecha, RH.CantidadHoras, RH.Descripcion, RH.Activo
    FROM dbo.RegistroHora RH INNER JOIN dbo.Empleado E ON E.ID = RH.IdEmpleado
    WHERE E.IdUsuario = @IdUsuario AND RH.Activo = 1 ORDER BY RH.Fecha DESC, RH.ID DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistroHora_Previsualizar @IdUsuario INT, @RegistrosJson NVARCHAR(MAX) AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdEmpleado INT = (SELECT E.ID FROM dbo.Empleado E WHERE E.IdUsuario = @IdUsuario AND E.Activo = 1);
    IF @IdEmpleado IS NULL THROW 50020, 'Tu usuario no tiene un perfil de empleado activo.', 1;

    DECLARE @Registros TABLE(Orden INT IDENTITY(1,1), IdTarea INT, Fecha DATETIME2, CantidadHoras DECIMAL(18,2), Descripcion NVARCHAR(500));
    INSERT INTO @Registros(IdTarea, Fecha, CantidadHoras, Descripcion)
    SELECT IdTarea, Fecha, CantidadHoras, Descripcion
    FROM OPENJSON(@RegistrosJson) WITH(IdTarea INT '$.idTarea', Fecha DATETIME2 '$.fecha', CantidadHoras DECIMAL(18,2) '$.cantidadHoras', Descripcion NVARCHAR(500) '$.descripcion');

    IF (SELECT COUNT(*) FROM @Registros) = 0 THROW 50023, 'No se recibieron horas para registrar.', 1;
    IF EXISTS (SELECT 1 FROM @Registros R LEFT JOIN dbo.Tarea T ON T.ID = R.IdTarea AND T.IdEmpleadoAsignado = @IdEmpleado AND T.Activo = 1 WHERE T.ID IS NULL) THROW 50021, 'Una de las tareas seleccionadas no está asignada a tu usuario.', 1;
    IF EXISTS (SELECT 1 FROM @Registros R INNER JOIN dbo.Tarea T ON T.ID = R.IdTarea WHERE T.FechaInicio IS NOT NULL AND CONVERT(DATE, R.Fecha) < CONVERT(DATE, T.FechaInicio)) THROW 50025, 'La fecha de trabajo no puede ser anterior a la fecha de inicio de la tarea.', 1;

    ;WITH ConsumoActual AS
    (
        SELECT R.IdTarea, SUM(R.CantidadHoras) AS HorasNuevas
        FROM @Registros R
        GROUP BY R.IdTarea
    )
    SELECT R.Orden, R.IdTarea, T.HorasEstimadas, ISNULL((SELECT SUM(RH.CantidadHoras) FROM dbo.RegistroHora RH WHERE RH.IdTarea = R.IdTarea AND RH.Activo = 1), 0) + C.HorasNuevas AS HorasRealesResultantes,
        CASE WHEN T.HorasEstimadas > 0 THEN ROUND(((ISNULL((SELECT SUM(RH.CantidadHoras) FROM dbo.RegistroHora RH WHERE RH.IdTarea = R.IdTarea AND RH.Activo = 1), 0) + C.HorasNuevas) / T.HorasEstimadas) * 100, 2) ELSE 0 END AS PorcentajeConsumo,
        CAST(CASE WHEN T.HorasEstimadas > 0 AND ((ISNULL((SELECT SUM(RH.CantidadHoras) FROM dbo.RegistroHora RH WHERE RH.IdTarea = R.IdTarea AND RH.Activo = 1), 0) + C.HorasNuevas) / T.HorasEstimadas) * 100 >= 80 THEN 1 ELSE 0 END AS BIT) AS RequiereAdvertencia
    FROM @Registros R
    INNER JOIN dbo.Tarea T ON T.ID = R.IdTarea
    INNER JOIN ConsumoActual C ON C.IdTarea = R.IdTarea
    ORDER BY R.Orden;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistroHora_RegistrarImputaciones @IdUsuario INT, @RegistrosJson NVARCHAR(MAX) AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @IdEmpleado INT = (SELECT E.ID FROM dbo.Empleado E WHERE E.IdUsuario = @IdUsuario AND E.Activo = 1);
    IF @IdEmpleado IS NULL THROW 50020, 'Tu usuario no tiene un perfil de empleado activo.', 1;

    DECLARE @Registros TABLE(Orden INT IDENTITY(1,1), IdTarea INT, Fecha DATETIME2, CantidadHoras DECIMAL(18,2), Descripcion NVARCHAR(500));
    INSERT INTO @Registros(IdTarea, Fecha, CantidadHoras, Descripcion)
    SELECT IdTarea, Fecha, CantidadHoras, Descripcion
    FROM OPENJSON(@RegistrosJson) WITH(IdTarea INT '$.idTarea', Fecha DATETIME2 '$.fecha', CantidadHoras DECIMAL(18,2) '$.cantidadHoras', Descripcion NVARCHAR(500) '$.descripcion');

    IF (SELECT COUNT(*) FROM @Registros) = 0 THROW 50023, 'No se recibieron horas para registrar.', 1;
    IF EXISTS (SELECT 1 FROM @Registros R LEFT JOIN dbo.Tarea T ON T.ID = R.IdTarea AND T.IdEmpleadoAsignado = @IdEmpleado AND T.Activo = 1 WHERE T.ID IS NULL) THROW 50021, 'Una de las tareas seleccionadas no está asignada a tu usuario.', 1;
    IF EXISTS (SELECT 1 FROM @Registros R INNER JOIN dbo.Tarea T ON T.ID = R.IdTarea WHERE T.FechaInicio IS NOT NULL AND CONVERT(DATE, R.Fecha) < CONVERT(DATE, T.FechaInicio)) THROW 50025, 'La fecha de trabajo no puede ser anterior a la fecha de inicio de la tarea.', 1;

    BEGIN TRANSACTION;
    DECLARE @Resultado TABLE(ID INT, IdTarea INT, IdEmpleado INT, Fecha DATETIME2, CantidadHoras DECIMAL(18,2), Descripcion NVARCHAR(500), Activo BIT);
    INSERT INTO dbo.RegistroHora(IdTarea, IdEmpleado, Fecha, CantidadHoras, Descripcion, Activo)
    OUTPUT inserted.ID, inserted.IdTarea, inserted.IdEmpleado, inserted.Fecha, inserted.CantidadHoras, inserted.Descripcion, inserted.Activo INTO @Resultado
    SELECT IdTarea, @IdEmpleado, Fecha, CantidadHoras, Descripcion, 1
    FROM @Registros;
    COMMIT TRANSACTION;

    SELECT ID, IdTarea, IdEmpleado, Fecha, CantidadHoras, Descripcion, Activo FROM @Resultado ORDER BY ID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ActualizarEstadoEmpleado @ID INT, @IdUsuario INT, @Estado NVARCHAR(50) AS
BEGIN
    SET NOCOUNT ON;
    UPDATE T SET Estado = @Estado
    FROM dbo.Tarea T INNER JOIN dbo.Empleado E ON E.ID = T.IdEmpleadoAsignado
    WHERE T.ID = @ID AND E.IdUsuario = @IdUsuario AND T.Activo = 1;
    IF @@ROWCOUNT = 0 THROW 50022, 'No se pudo actualizar el estado de la tarea asignada.', 1;
END
GO
