CREATE OR ALTER PROCEDURE dbo.usp_SimulacionImpacto_Crear @IdTarea INT, @IdEmpleadoCandidato INT, @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdAgencia INT, @HorasTarea DECIMAL(18,2), @Capacidad DECIMAL(18,2), @CargaActual DECIMAL(18,2);
    SELECT @IdAgencia = IdAgencia FROM dbo.Usuario WHERE ID = @IdUsuario AND Activo = 1;
    SELECT @HorasTarea = T.HorasEstimadas FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto WHERE T.ID = @IdTarea AND P.IdAgencia = @IdAgencia AND T.Activo = 1;
    SELECT @Capacidad = ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado = E.ID AND DB.Activo = 1 WHERE E.ID = @IdEmpleadoCandidato AND U.IdAgencia = @IdAgencia AND E.Activo = 1;
    SELECT @CargaActual = ISNULL(SUM(T.HorasEstimadas), 0) FROM dbo.Tarea T WHERE T.IdEmpleadoAsignado = @IdEmpleadoCandidato AND T.Activo = 1 AND T.Estado NOT IN ('Finalizada','Finalizado');
    IF @HorasTarea IS NULL OR @Capacidad IS NULL THROW 51000, 'La tarea o el recurso candidato no son válidos para esta agencia.', 1;
    DECLARE @CargaProyectada DECIMAL(18,2) = @CargaActual + @HorasTarea, @Disponible DECIMAL(18,2) = @Capacidad - (@CargaActual + @HorasTarea), @OcupacionActual DECIMAL(18,2) = CASE WHEN @Capacidad = 0 THEN 0 ELSE @CargaActual * 100 / @Capacidad END, @OcupacionProyectada DECIMAL(18,2) = CASE WHEN @Capacidad = 0 THEN 100 ELSE (@CargaActual + @HorasTarea) * 100 / @Capacidad END;
    INSERT INTO dbo.SimulacionImpacto(IdTarea,IdEmpleadoCandidato,IdUsuarioCreador,CargaActual,CargaProyectada,DisponibilidadRestante,PorcentajeOcupacionActual,PorcentajeOcupacionProyectado,GeneraSobrecarga,AdvertenciasJson,ImpactoOperativo,FechaCreacion,FechaExpiracion,Activo)
    OUTPUT inserted.* VALUES(@IdTarea,@IdEmpleadoCandidato,@IdUsuario,@CargaActual,@CargaProyectada,@Disponible,@OcupacionActual,@OcupacionProyectada,CASE WHEN @Disponible < 0 THEN 1 ELSE 0 END,CASE WHEN @Disponible < 0 THEN '["La asignación supera la disponibilidad semanal."]' ELSE '[]' END,CASE WHEN @Disponible < 0 THEN 'Sobrecarga' ELSE 'Viable' END,SYSDATETIME(),DATEADD(DAY,1,SYSDATETIME()),1);
END;
