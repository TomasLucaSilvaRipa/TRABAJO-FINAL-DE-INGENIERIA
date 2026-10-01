CREATE OR ALTER PROCEDURE dbo.usp_BestFit_Sugerir @IdTarea INT, @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdAgencia INT, @IdSkill INT, @HorasTarea DECIMAL(18,2);
    SELECT @IdAgencia = IdAgencia FROM dbo.Usuario WHERE ID = @IdUsuario AND Activo = 1;
    SELECT @IdSkill = T.IdSkillRequerido, @HorasTarea = T.HorasEstimadas FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto WHERE T.ID = @IdTarea AND P.IdAgencia = @IdAgencia AND T.Activo = 1;
    IF @IdAgencia IS NULL OR @HorasTarea IS NULL THROW 51000, 'La tarea no existe o no pertenece a tu agencia.', 1;
    ;WITH Candidatos AS
    (
        SELECT E.ID, CAST(CASE WHEN @IdSkill IS NULL THEN 40 WHEN ES.ID IS NOT NULL THEN 70 ELSE 20 END + CASE WHEN ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) >= @HorasTarea THEN 30 ELSE 5 END AS DECIMAL(5,2)) AS Puntaje
        FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
        LEFT JOIN dbo.EmpleadoSkill ES ON ES.IdEmpleado = E.ID AND ES.IdSkill = @IdSkill AND ES.Activo = 1
        LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado = E.ID AND DB.Activo = 1
        WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1
    )
    INSERT INTO dbo.RecomendacionBestFit(IdTarea, IdEmpleadoSugerido, IdUsuarioSolicitante, PuntajeCompatibilidad, FechaGeneracion, SeleccionadaPorPM, Estado, Activo)
    SELECT @IdTarea, ID, @IdUsuario, Puntaje, SYSDATETIME(), 0, 'Generada', 1 FROM Candidatos ORDER BY Puntaje DESC, ID;
    SELECT R.*, CONCAT(U.Nombre, N' ', U.Apellido) NombreEmpleado, E.Seniority SeniorityEmpleado, ISNULL(DB.HorasSemanales,E.HorasDisponiblesSemanales) HorasDisponibles FROM dbo.RecomendacionBestFit R INNER JOIN dbo.Empleado E ON E.ID=R.IdEmpleadoSugerido INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado=E.ID AND DB.Activo=1 WHERE R.IdTarea=@IdTarea AND R.IdUsuarioSolicitante=@IdUsuario AND R.Activo=1 ORDER BY R.PuntajeCompatibilidad DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_BestFit_ConfirmarAsignacion @ID INT, @IdUsuario INT AS
BEGIN
    DECLARE @IdTarea INT,@IdEmpleado INT,@IdAgencia INT; SELECT @IdAgencia=IdAgencia FROM dbo.Usuario WHERE ID=@IdUsuario; SELECT @IdTarea=IdTarea,@IdEmpleado=IdEmpleadoSugerido FROM dbo.RecomendacionBestFit WHERE ID=@ID AND Activo=1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto INNER JOIN dbo.PM PM ON PM.ID=P.IdPMResponsable WHERE T.ID=@IdTarea AND PM.IdUsuario=@IdUsuario) THROW 51000,'No podés asignar recursos en esta tarea.',1;
    UPDATE dbo.Tarea SET IdEmpleadoAsignado=@IdEmpleado WHERE ID=@IdTarea; UPDATE dbo.RecomendacionBestFit SET SeleccionadaPorPM=CASE WHEN ID=@ID THEN 1 ELSE 0 END,Estado=CASE WHEN ID=@ID THEN N'Confirmada' ELSE N'Descartada' END WHERE IdTarea=@IdTarea AND Activo=1;
END;
