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

CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ActualizarEstadoEmpleado @ID INT, @IdUsuario INT, @Estado NVARCHAR(50) AS
BEGIN
    SET NOCOUNT ON;
    UPDATE T SET Estado = @Estado
    FROM dbo.Tarea T INNER JOIN dbo.Empleado E ON E.ID = T.IdEmpleadoAsignado
    WHERE T.ID = @ID AND E.IdUsuario = @IdUsuario AND T.Activo = 1;
    IF @@ROWCOUNT = 0 THROW 50022, 'No se pudo actualizar el estado de la tarea asignada.', 1;
END
GO
