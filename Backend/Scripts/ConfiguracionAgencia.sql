CREATE OR ALTER PROCEDURE dbo.usp_Agencia_Consultar @IdAgencia INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NombreComercial, RazonSocial, CUIT, CondicionFiscal, EmailContacto, TelefonoContacto, FechaAlta, Estado, Activo, FechaBaja FROM dbo.Agencia WHERE ID = @IdAgencia;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Agencia_Modificar @IdAgencia INT, @NombreComercial NVARCHAR(150), @RazonSocial NVARCHAR(200) = NULL, @CondicionFiscal NVARCHAR(80) = NULL, @EmailContacto NVARCHAR(150), @TelefonoContacto NVARCHAR(50) = NULL AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Agencia SET NombreComercial = @NombreComercial, RazonSocial = NULLIF(@RazonSocial, N''), CondicionFiscal = NULLIF(@CondicionFiscal, N''), EmailContacto = @EmailContacto, TelefonoContacto = NULLIF(@TelefonoContacto, N'') WHERE ID = @IdAgencia;
    EXEC dbo.usp_Agencia_Consultar @IdAgencia;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarActualPorAgencia @IdAgencia INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) S.ID, S.IdAgencia, S.IdPlanComercial, S.ReferenciaExterna, S.Estado, S.FechaAlta, S.FechaVencimiento, S.FechaProximaRenovacion, S.RenovacionAutomatica, S.ImporteVigente, S.Activo, S.FechaBaja, P.Nombre AS NombrePlan, P.Periodicidad AS PeriodicidadPlan, P.Moneda AS Moneda FROM dbo.Suscripcion S INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial WHERE S.IdAgencia = @IdAgencia ORDER BY S.Activo DESC, S.FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ConsultarPorEmpleado @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT T.* FROM dbo.Tarea T INNER JOIN dbo.Empleado E ON E.ID = T.IdEmpleadoAsignado WHERE E.IdUsuario = @IdUsuario AND E.Activo = 1 AND T.Activo = 1 ORDER BY T.Deadline, T.ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ActualizarComentarios @ID INT, @IdUsuario INT, @ComentariosJson NVARCHAR(MAX) AS
BEGIN
    SET NOCOUNT ON;
    UPDATE T SET ComentariosJson = @ComentariosJson FROM dbo.Tarea T INNER JOIN dbo.Empleado E ON E.ID = T.IdEmpleadoAsignado WHERE T.ID = @ID AND E.IdUsuario = @IdUsuario;
END;
GO
