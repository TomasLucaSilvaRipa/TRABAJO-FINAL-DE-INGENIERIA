CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_ConsultarProyectosActivos @IdAgencia INT, @IdProyecto INT = NULL, @IdCliente INT = NULL, @IdPM INT = NULL AS
BEGIN
    SET NOCOUNT ON;
    SELECT P.*, C.Nombre AS NombreCliente, CONCAT(U.Nombre, N' ', U.Apellido) AS NombrePMResponsable
    FROM dbo.Proyecto P
    INNER JOIN dbo.Cliente C ON C.ID = P.IdCliente
    INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable
    INNER JOIN dbo.Usuario U ON U.ID = PM.IdUsuario
    WHERE P.IdAgencia = @IdAgencia AND P.Activo = 1
      AND (@IdProyecto IS NULL OR P.ID = @IdProyecto)
      AND (@IdCliente IS NULL OR P.IdCliente = @IdCliente)
      AND (@IdPM IS NULL OR P.IdPMResponsable = @IdPM);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_ConsultarTareasProyecto @IdProyecto INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT T.* FROM dbo.Tarea T WHERE T.IdProyecto = @IdProyecto AND T.Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_ConsultarEquipoProyecto @IdProyecto INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT E.ID, U.Nombre, U.Email, E.Activo, E.CostoHora,
        COALESCE(D.HorasSemanales, E.HorasDisponiblesSemanales, 0) AS HorasDisponiblesSemanales,
        E.Seniority, E.EstadoLaboral, E.FechaIngreso
    FROM dbo.Tarea T
    INNER JOIN dbo.Empleado E ON E.ID = T.IdEmpleadoAsignado
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    OUTER APPLY (SELECT TOP (1) DB.HorasSemanales FROM dbo.DisponibilidadBase DB WHERE DB.IdEmpleado = E.ID AND DB.Activo = 1 ORDER BY DB.ID DESC) D
    WHERE T.IdProyecto = @IdProyecto AND T.Activo = 1 AND E.Activo = 1 AND U.Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_ConsultarRegistrosHora @IdProyecto INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT RH.ID, RH.IdTarea, RH.IdEmpleado, RH.Fecha, RH.CantidadHoras, RH.Descripcion, RH.Activo
    FROM dbo.RegistroHora RH
    INNER JOIN dbo.Tarea T ON T.ID = RH.IdTarea
    WHERE T.IdProyecto = @IdProyecto AND RH.Activo = 1;
END
GO
