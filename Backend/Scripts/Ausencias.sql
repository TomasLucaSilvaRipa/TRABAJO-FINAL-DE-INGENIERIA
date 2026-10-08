USE [TeamBalance]
GO

IF COL_LENGTH(N'dbo.AusenciaEmpleado', N'ComprobanteUrl') IS NULL ALTER TABLE dbo.AusenciaEmpleado ADD ComprobanteUrl NVARCHAR(1000) NULL;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_ConsultarPropias @IdUsuario INT AS
BEGIN SET NOCOUNT ON; SELECT A.* FROM dbo.AusenciaEmpleado A INNER JOIN dbo.Empleado E ON E.ID = A.IdEmpleado WHERE E.IdUsuario = @IdUsuario AND A.Activo = 1 ORDER BY A.FechaInicioSolicitada DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_ConsultarPendientes @IdAgencia INT AS
BEGIN SET NOCOUNT ON; SELECT A.*, CONCAT(U.Nombre, N' ', U.Apellido) AS NombreEmpleado FROM dbo.AusenciaEmpleado A INNER JOIN dbo.Empleado E ON E.ID = A.IdEmpleado INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1 AND A.Activo = 1 AND A.Estado = N'Pendiente' ORDER BY A.FechaSolicitud ASC; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Disponibilidad_ConsultarEmpleados @IdAgencia INT AS
BEGIN SET NOCOUNT ON; SELECT U.ID AS IdUsuario, CONCAT(U.Nombre, N' ', U.Apellido) AS NombreCompleto FROM dbo.Usuario U INNER JOIN dbo.Empleado E ON E.IdUsuario = U.ID WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1 ORDER BY U.Apellido, U.Nombre; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_ConsultarPorId @IdAusencia INT, @IdAgencia INT AS
BEGIN SET NOCOUNT ON; SELECT A.*, CONCAT(U.Nombre, N' ', U.Apellido) AS NombreEmpleado FROM dbo.AusenciaEmpleado A INNER JOIN dbo.Empleado E ON E.ID = A.IdEmpleado INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario WHERE A.ID = @IdAusencia AND U.IdAgencia = @IdAgencia AND A.Activo = 1; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_ConsultarPorEmpleado @IdEmpleado INT, @IdAgencia INT AS
BEGIN SET NOCOUNT ON; SELECT A.* FROM dbo.AusenciaEmpleado A INNER JOIN dbo.Empleado E ON E.ID = A.IdEmpleado INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario WHERE A.IdEmpleado = @IdEmpleado AND U.IdAgencia = @IdAgencia AND A.Activo = 1 ORDER BY A.FechaInicioSolicitada DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_Registrar @IdUsuario INT, @TipoPeriodo NVARCHAR(80), @FechaInicio DATETIME2, @FechaFin DATETIME2, @HorasNoDisponibles DECIMAL(5,2) = NULL, @Motivo NVARCHAR(500), @ComprobanteUrl NVARCHAR(1000) = NULL AS
BEGIN
 SET NOCOUNT ON; DECLARE @IdEmpleado INT; SELECT @IdEmpleado = ID FROM dbo.Empleado WHERE IdUsuario = @IdUsuario AND Activo = 1;
 IF @IdEmpleado IS NULL THROW 51000, 'El usuario no tiene un perfil de empleado activo.', 1;
 INSERT INTO dbo.AusenciaEmpleado(IdEmpleado,TipoPeriodo,FechaInicioSolicitada,FechaFinSolicitada,HorasNoDisponiblesSolicitadas,Motivo,ComprobanteUrl,Estado,FechaSolicitud,Activo) VALUES(@IdEmpleado,@TipoPeriodo,@FechaInicio,@FechaFin,@HorasNoDisponibles,@Motivo,@ComprobanteUrl,N'Pendiente',SYSDATETIME(),1);
 SELECT * FROM dbo.AusenciaEmpleado WHERE ID = SCOPE_IDENTITY();
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_RegistrarDirecta @IdEmpleado INT, @IdAgencia INT, @IdUsuarioResolucion INT, @TipoPeriodo NVARCHAR(80), @FechaInicio DATETIME2, @FechaFin DATETIME2, @HorasNoDisponibles DECIMAL(5,2) = NULL, @Motivo NVARCHAR(500), @ComprobanteUrl NVARCHAR(1000) = NULL AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS (SELECT 1 FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario WHERE E.ID = @IdEmpleado AND E.Activo = 1 AND U.Activo = 1 AND U.IdAgencia = @IdAgencia) THROW 51001, 'El empleado seleccionado no está activo o no pertenece a la agencia.', 1;
 INSERT INTO dbo.AusenciaEmpleado(IdEmpleado,TipoPeriodo,FechaInicioSolicitada,FechaFinSolicitada,FechaInicioAprobada,FechaFinAprobada,HorasNoDisponiblesSolicitadas,HorasNoDisponiblesAprobadas,Motivo,ComprobanteUrl,IdUsuarioResolucion,Estado,FechaSolicitud,FechaResolucion,Activo) VALUES(@IdEmpleado,@TipoPeriodo,@FechaInicio,@FechaFin,@FechaInicio,@FechaFin,@HorasNoDisponibles,@HorasNoDisponibles,@Motivo,@ComprobanteUrl,@IdUsuarioResolucion,N'Aprobada',SYSDATETIME(),SYSDATETIME(),1);
 SELECT * FROM dbo.AusenciaEmpleado WHERE ID = SCOPE_IDENTITY();
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_Resolver @IdAusencia INT, @IdUsuarioResolucion INT, @Estado NVARCHAR(50), @FechaInicioAprobada DATETIME2 = NULL, @FechaFinAprobada DATETIME2 = NULL, @HorasNoDisponiblesAprobadas DECIMAL(5,2) = NULL, @MotivoResolucion NVARCHAR(500) = NULL AS
BEGIN
 SET NOCOUNT ON; IF NOT EXISTS (SELECT 1 FROM dbo.AusenciaEmpleado WHERE ID = @IdAusencia AND Activo = 1 AND Estado = N'Pendiente') THROW 51002, 'La solicitud no existe o ya fue resuelta.', 1;
 UPDATE dbo.AusenciaEmpleado SET Estado = @Estado, IdUsuarioResolucion = @IdUsuarioResolucion, FechaResolucion = SYSDATETIME(), FechaInicioAprobada = CASE WHEN @Estado = N'Rechazada' THEN NULL ELSE @FechaInicioAprobada END, FechaFinAprobada = CASE WHEN @Estado = N'Rechazada' THEN NULL ELSE @FechaFinAprobada END, HorasNoDisponiblesAprobadas = CASE WHEN @Estado = N'Rechazada' THEN NULL ELSE @HorasNoDisponiblesAprobadas END, MotivoResolucion = @MotivoResolucion WHERE ID = @IdAusencia;
 SELECT * FROM dbo.AusenciaEmpleado WHERE ID = @IdAusencia;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Disponibilidad_ConsultarTareasAfectadas @IdEmpleado INT, @FechaDesde DATETIME2, @FechaHasta DATETIME2, @IdAgencia INT AS
BEGIN SET NOCOUNT ON; SELECT T.ID AS IdTarea, T.Titulo, P.Nombre AS NombreProyecto, T.Deadline FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto WHERE T.IdEmpleadoAsignado = @IdEmpleado AND T.Activo = 1 AND P.Activo = 1 AND P.IdAgencia = @IdAgencia AND T.Estado NOT IN (N'Finalizada', N'Finalizado') AND T.Deadline >= @FechaDesde AND T.Deadline < DATEADD(DAY, 1, @FechaHasta) ORDER BY T.Deadline, T.ID; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Disponibilidad_EsGestor @IdUsuario INT, @IdAgencia INT AS
BEGIN SET NOCOUNT ON; SELECT CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID = UR.IdRol WHERE UR.IdUsuario = @IdUsuario AND R.Activo = 1 AND (R.TipoUsuario = N'Dueno' OR (R.TipoUsuario = N'PM' AND EXISTS (SELECT 1 FROM dbo.PM PM WHERE PM.IdUsuario = @IdUsuario AND PM.Activo = 1 AND PM.AutorizadoGestionRecursos = 1)))) THEN 1 ELSE 0 END) AS EsGestor; END;
