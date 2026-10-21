CREATE OR ALTER PROCEDURE dbo.usp_BestFit_ConsultarCandidatos @IdTarea INT, @IdAgencia INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdSkill INT;
    SELECT @IdSkill = T.IdSkillRequerido FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto WHERE T.ID = @IdTarea AND P.IdAgencia = @IdAgencia AND T.Activo = 1;
    IF @IdSkill IS NULL THROW 51000, 'La tarea no existe o no pertenece a la agencia.', 1;

    SELECT E.ID, U.Nombre, U.Apellido, E.Seniority, ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) HorasDisponiblesSemanales, ES.Nivel NivelSkill,
        ISNULL(Carga.HorasComprometidasSemanales, 0) HorasComprometidasSemanales,
        ISNULL(Historial.CantidadTareasHistoricasComparables, 0) CantidadTareasHistoricasComparables,
        ISNULL(Historial.RatioHistoricoConsumo, 1) RatioHistoricoConsumo
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    INNER JOIN dbo.EmpleadoSkill ES ON ES.IdEmpleado = E.ID AND ES.IdSkill = @IdSkill AND ES.Activo = 1
    OUTER APPLY (SELECT TOP 1 Disponibilidad.HorasSemanales FROM dbo.DisponibilidadBase Disponibilidad WHERE Disponibilidad.IdEmpleado = E.ID AND Disponibilidad.Activo = 1 ORDER BY Disponibilidad.ID DESC) DB
    OUTER APPLY (
        SELECT SUM(((CASE WHEN Asignacion.ID IS NULL THEN TareaAsignada.HorasEstimadas ELSE Asignacion.HorasAsignadas END) * (100 - TareaAsignada.PorcentajeAvance) / 100.0) /
            CASE WHEN TareaAsignada.Deadline IS NULL OR TareaAsignada.Deadline <= CONVERT(date, SYSDATETIME()) THEN 1 ELSE CEILING(DATEDIFF(day, CONVERT(date, SYSDATETIME()), TareaAsignada.Deadline) / 7.0) END) HorasComprometidasSemanales
        FROM dbo.Tarea TareaAsignada
        LEFT JOIN dbo.AsignacionTarea Asignacion ON Asignacion.IdTarea = TareaAsignada.ID AND Asignacion.IdEmpleado = E.ID AND Asignacion.Activa = 1
        WHERE (Asignacion.ID IS NOT NULL OR (TareaAsignada.IdEmpleadoAsignado = E.ID AND NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea Existente WHERE Existente.IdTarea = TareaAsignada.ID AND Existente.Activa = 1)))
          AND TareaAsignada.Activo = 1 AND TareaAsignada.FechaFinReal IS NULL AND TareaAsignada.ID <> @IdTarea
    ) Carga
    OUTER APPLY (
        SELECT COUNT(*) CantidadTareasHistoricasComparables, AVG(Consumo.RatioConsumo) RatioHistoricoConsumo
        FROM (
            SELECT TareaHistorica.ID, CAST(ISNULL(SUM(Registro.CantidadHoras), 0) / NULLIF(TareaHistorica.HorasEstimadas, 0) AS DECIMAL(18,4)) RatioConsumo
            FROM dbo.Tarea TareaHistorica
            LEFT JOIN dbo.RegistroHora Registro ON Registro.IdTarea = TareaHistorica.ID AND Registro.IdEmpleado = E.ID AND Registro.Activo = 1
            WHERE (EXISTS (SELECT 1 FROM dbo.AsignacionTarea AsignacionHistorica WHERE AsignacionHistorica.IdTarea = TareaHistorica.ID AND AsignacionHistorica.IdEmpleado = E.ID)
                   OR (NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea AsignacionHistorica WHERE AsignacionHistorica.IdTarea = TareaHistorica.ID)
                       AND TareaHistorica.IdEmpleadoAsignado = E.ID))
              AND TareaHistorica.Activo = 1 AND TareaHistorica.FechaFinReal IS NOT NULL AND TareaHistorica.IdSkillRequerido = @IdSkill AND TareaHistorica.HorasEstimadas > 0
            GROUP BY TareaHistorica.ID, TareaHistorica.HorasEstimadas
        ) Consumo
    ) Historial
    WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea AsignacionActual WHERE AsignacionActual.IdTarea = @IdTarea AND AsignacionActual.IdEmpleado = E.ID AND AsignacionActual.Activa = 1)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.Tarea TareaActual
          WHERE TareaActual.ID = @IdTarea AND TareaActual.IdEmpleadoAsignado = E.ID
            AND NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea AsignacionActual WHERE AsignacionActual.IdTarea = @IdTarea AND AsignacionActual.Activa = 1)
      );
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_BestFit_GuardarRecomendacion @IdTarea INT, @IdEmpleado INT, @IdUsuario INT, @PuntajeCompatibilidad DECIMAL(18,2) AS
BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable WHERE T.ID = @IdTarea AND T.Activo = 1 AND PM.IdUsuario = @IdUsuario) THROW 51000, 'No podés conservar una recomendación para esta tarea.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario INNER JOIN dbo.Tarea T ON T.ID = @IdTarea INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto WHERE E.ID = @IdEmpleado AND E.Activo = 1 AND U.Activo = 1 AND U.IdAgencia = P.IdAgencia) THROW 51000, 'El empleado seleccionado no pertenece a la agencia.', 1;
    IF EXISTS(SELECT 1 FROM dbo.RecomendacionBestFit WHERE IdTarea = @IdTarea AND IdEmpleadoSugerido = @IdEmpleado AND Activo = 1 AND SeleccionadaPorPM = 1)
        UPDATE dbo.RecomendacionBestFit SET IdUsuarioSolicitante = @IdUsuario, PuntajeCompatibilidad = @PuntajeCompatibilidad, FechaGeneracion = SYSDATETIME(), Estado = N'Recomendada' WHERE IdTarea = @IdTarea AND IdEmpleadoSugerido = @IdEmpleado AND Activo = 1 AND SeleccionadaPorPM = 1;
    ELSE
        INSERT INTO dbo.RecomendacionBestFit(IdTarea, IdEmpleadoSugerido, IdUsuarioSolicitante, PuntajeCompatibilidad, FechaGeneracion, SeleccionadaPorPM, Estado, Activo) VALUES(@IdTarea, @IdEmpleado, @IdUsuario, @PuntajeCompatibilidad, SYSDATETIME(), 1, N'Recomendada', 1);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BestFit_ConsultarRecomendaciones @IdTarea INT, @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable WHERE T.ID = @IdTarea AND PM.IdUsuario = @IdUsuario)
        THROW 51000, 'La tarea no existe o no pertenece a uno de tus proyectos.', 1;

    SELECT R.ID, R.IdTarea, R.IdEmpleadoSugerido, R.IdUsuarioSolicitante, R.PuntajeCompatibilidad, R.FechaGeneracion, R.SeleccionadaPorPM, R.Estado, R.Activo,
        CONCAT(U.Nombre, N' ', U.Apellido) NombreEmpleado, E.Seniority SeniorityEmpleado, ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) HorasDisponibles
    FROM dbo.RecomendacionBestFit R
    INNER JOIN dbo.Empleado E ON E.ID = R.IdEmpleadoSugerido
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    OUTER APPLY (SELECT TOP 1 Disponibilidad.HorasSemanales FROM dbo.DisponibilidadBase Disponibilidad WHERE Disponibilidad.IdEmpleado = E.ID AND Disponibilidad.Activo = 1 ORDER BY Disponibilidad.ID DESC) DB
    WHERE R.IdTarea = @IdTarea AND R.Activo = 1 AND R.SeleccionadaPorPM = 1 AND R.Estado = N'Recomendada'
    ORDER BY R.PuntajeCompatibilidad DESC, R.FechaGeneracion DESC, R.ID DESC;
END;
