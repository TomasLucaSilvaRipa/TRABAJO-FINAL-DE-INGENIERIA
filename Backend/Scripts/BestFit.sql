CREATE OR ALTER PROCEDURE dbo.usp_BestFit_Sugerir @IdTarea INT, @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdAgencia INT, @IdSkill INT, @HorasTarea DECIMAL(18,2);
    SELECT @IdAgencia = IdAgencia FROM dbo.Usuario WHERE ID = @IdUsuario AND Activo = 1;
    SELECT @IdSkill = T.IdSkillRequerido, @HorasTarea = T.HorasEstimadas FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto WHERE T.ID = @IdTarea AND P.IdAgencia = @IdAgencia AND T.Activo = 1;
    IF @IdAgencia IS NULL OR @IdSkill IS NULL THROW 51000, 'La tarea no existe, no pertenece a tu agencia o no tiene una skill requerida.', 1;
    ;WITH Candidatos AS
    (
        SELECT E.ID, CAST(50 + CASE WHEN ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) >= @HorasTarea THEN 50 ELSE 20 END AS DECIMAL(5,2)) AS Puntaje
        FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
        INNER JOIN dbo.EmpleadoSkill ES ON ES.IdEmpleado = E.ID AND ES.IdSkill = @IdSkill AND ES.Activo = 1
        LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado = E.ID AND DB.Activo = 1
        WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1
    )
    INSERT INTO dbo.RecomendacionBestFit(IdTarea, IdEmpleadoSugerido, IdUsuarioSolicitante, PuntajeCompatibilidad, FechaGeneracion, SeleccionadaPorPM, Estado, Activo)
    OUTPUT inserted.ID, inserted.IdTarea, inserted.IdEmpleadoSugerido, inserted.IdUsuarioSolicitante, inserted.PuntajeCompatibilidad, inserted.FechaGeneracion, inserted.SeleccionadaPorPM, inserted.Estado, inserted.Activo
    SELECT @IdTarea, ID, @IdUsuario, Puntaje, SYSDATETIME(), 0, 'Generada', 1 FROM Candidatos ORDER BY Puntaje DESC, ID;
END;
