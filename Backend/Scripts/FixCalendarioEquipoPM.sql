USE [TeamBalance]
GO

CREATE OR ALTER PROCEDURE dbo.usp_Disponibilidad_ConsultarCalendarioEquipo
    @IdAgencia INT,
    @IdUsuarioSolicitante INT,
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdProyecto INT = NULL,
    @IdEmpleado INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EsDueno BIT = CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID = UR.IdRol
        WHERE UR.IdUsuario = @IdUsuarioSolicitante AND R.Activo = 1 AND R.TipoUsuario = N'Dueno'
    ) THEN 1 ELSE 0 END;
    DECLARE @IdPMResponsable INT =
    (
        SELECT TOP (1) PM.ID
        FROM dbo.PM PM
        WHERE PM.IdUsuario = @IdUsuarioSolicitante AND PM.Activo = 1
    );

    SELECT E.ID AS IdEmpleado, U.ID AS IdUsuario, CONCAT(U.Nombre, N' ', U.Apellido) AS NombreEmpleado,
           ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS HorasSemanales,
           A.ID AS IdAusencia, A.TipoPeriodo,
           CASE WHEN A.Estado IN (N'Aprobada', N'Aprobada parcialmente') THEN COALESCE(A.FechaInicioAprobada, A.FechaInicioSolicitada) ELSE A.FechaInicioSolicitada END AS FechaInicio,
           CASE WHEN A.Estado IN (N'Aprobada', N'Aprobada parcialmente') THEN COALESCE(A.FechaFinAprobada, A.FechaFinSolicitada) ELSE A.FechaFinSolicitada END AS FechaFin,
           CASE WHEN A.Estado IN (N'Aprobada', N'Aprobada parcialmente') THEN COALESCE(A.HorasNoDisponiblesAprobadas, A.HorasNoDisponiblesSolicitadas) ELSE A.HorasNoDisponiblesSolicitadas END AS HorasNoDisponibles,
           A.Estado, A.Motivo, A.MotivoResolucion,
           ISNULL(Tareas.Cantidad, 0) AS CantidadTareasAfectadas, Tareas.Detalle AS TareasAfectadas
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado = E.ID AND DB.Activo = 1
    LEFT JOIN dbo.AusenciaEmpleado A ON A.IdEmpleado = E.ID AND A.Activo = 1
      AND CAST(CASE WHEN A.Estado IN (N'Aprobada', N'Aprobada parcialmente') THEN COALESCE(A.FechaInicioAprobada, A.FechaInicioSolicitada) ELSE A.FechaInicioSolicitada END AS DATE) <= @FechaHasta
      AND CAST(CASE WHEN A.Estado IN (N'Aprobada', N'Aprobada parcialmente') THEN COALESCE(A.FechaFinAprobada, A.FechaFinSolicitada) ELSE A.FechaFinSolicitada END AS DATE) >= @FechaDesde
    OUTER APPLY
    (
        SELECT COUNT(*) AS Cantidad,
               STRING_AGG(CONCAT(T.Titulo, N' · ', P.Nombre, N' · ', CONVERT(nvarchar(10), T.Deadline, 103)), N' | ') AS Detalle
        FROM dbo.Tarea T
        INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
        WHERE A.ID IS NOT NULL AND P.IdAgencia = @IdAgencia AND T.Activo = 1 AND P.Activo = 1
          AND T.Estado NOT IN (N'Finalizada', N'Finalizado')
          AND (EXISTS (SELECT 1 FROM dbo.AsignacionTarea AT WHERE AT.IdTarea = T.ID AND AT.IdEmpleado = E.ID AND AT.Activa = 1)
            OR (T.IdEmpleadoAsignado = E.ID AND NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea AT2 WHERE AT2.IdTarea = T.ID AND AT2.Activa = 1)))
          AND T.Deadline BETWEEN COALESCE(A.FechaInicioAprobada, A.FechaInicioSolicitada) AND DATEADD(DAY, 1, COALESCE(A.FechaFinAprobada, A.FechaFinSolicitada))
    ) Tareas
    WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1
      AND (@IdEmpleado IS NULL OR E.ID = @IdEmpleado)
      AND (@EsDueno = 1 OR EXISTS
      (
          SELECT 1 FROM dbo.Tarea TP INNER JOIN dbo.Proyecto PP ON PP.ID = TP.IdProyecto
          WHERE PP.IdAgencia = @IdAgencia AND PP.IdPMResponsable = @IdPMResponsable AND TP.Activo = 1
            AND (TP.IdEmpleadoAsignado = E.ID OR EXISTS (SELECT 1 FROM dbo.AsignacionTarea ATP WHERE ATP.IdTarea = TP.ID AND ATP.IdEmpleado = E.ID AND ATP.Activa = 1))
      ))
      AND (@IdProyecto IS NULL OR EXISTS
      (
          SELECT 1 FROM dbo.Tarea TF WHERE TF.IdProyecto = @IdProyecto AND TF.Activo = 1
            AND (TF.IdEmpleadoAsignado = E.ID OR EXISTS (SELECT 1 FROM dbo.AsignacionTarea ATF WHERE ATF.IdTarea = TF.ID AND ATF.IdEmpleado = E.ID AND ATF.Activa = 1))
      ))
    ORDER BY U.Apellido, U.Nombre, FechaInicio;
END;
GO
