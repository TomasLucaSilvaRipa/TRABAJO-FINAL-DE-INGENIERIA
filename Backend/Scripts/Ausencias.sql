CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_ConsultarPropias @IdUsuario INT AS
BEGIN
    SELECT A.* FROM dbo.AusenciaEmpleado A INNER JOIN dbo.Empleado E ON E.ID = A.IdEmpleado WHERE E.IdUsuario = @IdUsuario AND A.Activo = 1 ORDER BY A.FechaInicioSolicitada DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AusenciaEmpleado_Registrar @IdUsuario INT, @TipoPeriodo NVARCHAR(50), @FechaInicio DATETIME2, @FechaFin DATETIME2, @Motivo NVARCHAR(500) = NULL AS
BEGIN
    DECLARE @IdEmpleado INT; SELECT @IdEmpleado = ID FROM dbo.Empleado WHERE IdUsuario = @IdUsuario AND Activo = 1;
    IF @IdEmpleado IS NULL THROW 51000, 'El usuario no tiene un perfil de empleado activo.', 1;
    INSERT INTO dbo.AusenciaEmpleado(IdEmpleado,TipoPeriodo,FechaInicioSolicitada,FechaFinSolicitada,Motivo,Estado,FechaSolicitud,Activo) VALUES(@IdEmpleado,@TipoPeriodo,@FechaInicio,@FechaFin,@Motivo,N'Pendiente',SYSDATETIME(),1);
    SELECT * FROM dbo.AusenciaEmpleado WHERE ID = SCOPE_IDENTITY();
END;
