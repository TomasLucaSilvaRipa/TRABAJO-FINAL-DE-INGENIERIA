IF COL_LENGTH('dbo.Cliente', 'IdAgencia') IS NULL ALTER TABLE dbo.Cliente ADD IdAgencia INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Cliente_Agencia') ALTER TABLE dbo.Cliente ADD CONSTRAINT FK_Cliente_Agencia FOREIGN KEY (IdAgencia) REFERENCES dbo.Agencia(ID);
GO
CREATE OR ALTER PROCEDURE dbo.usp_EstadoTarea_CrearBase @IdAgencia INT AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoTarea WHERE IdAgencia = @IdAgencia)
    BEGIN
        INSERT INTO dbo.EstadoTarea (IdAgencia, Nombre, Orden, EsBase, EsFinal, Activo) VALUES
        (@IdAgencia, N'Pendiente', 1, 1, 0, 1), (@IdAgencia, N'En progreso', 2, 1, 0, 1), (@IdAgencia, N'Bloqueada', 3, 1, 0, 1), (@IdAgencia, N'Finalizada', 4, 1, 1, 1);
    END
END
GO
DECLARE @IdAgenciaExistente INT;
DECLARE cursorAgencias CURSOR LOCAL FAST_FORWARD FOR SELECT ID FROM dbo.Agencia;
OPEN cursorAgencias;
FETCH NEXT FROM cursorAgencias INTO @IdAgenciaExistente;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.usp_EstadoTarea_CrearBase @IdAgenciaExistente;
    FETCH NEXT FROM cursorAgencias INTO @IdAgenciaExistente;
END
CLOSE cursorAgencias;
DEALLOCATE cursorAgencias;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Cliente_ConsultarPorAgencia @IdAgencia INT AS SELECT ID, Nombre, RazonSocial, Email, Telefono, Activo FROM dbo.Cliente WHERE IdAgencia = @IdAgencia ORDER BY Activo DESC, Nombre;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Cliente_Registrar @Nombre NVARCHAR(150), @RazonSocial NVARCHAR(200) = NULL, @Email NVARCHAR(200) = NULL, @Telefono NVARCHAR(50) = NULL, @IdAgencia INT AS
BEGIN INSERT INTO dbo.Cliente (Nombre, RazonSocial, Email, Telefono, Activo, IdAgencia) VALUES (@Nombre, @RazonSocial, @Email, @Telefono, 1, @IdAgencia); SELECT ID, Nombre, RazonSocial, Email, Telefono, Activo FROM dbo.Cliente WHERE ID = SCOPE_IDENTITY(); END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Cliente_Modificar @ID INT, @Nombre NVARCHAR(150), @RazonSocial NVARCHAR(200) = NULL, @Email NVARCHAR(200) = NULL, @Telefono NVARCHAR(50) = NULL, @IdAgencia INT AS
BEGIN UPDATE dbo.Cliente SET Nombre=@Nombre, RazonSocial=@RazonSocial, Email=@Email, Telefono=@Telefono WHERE ID=@ID AND IdAgencia=@IdAgencia; SELECT ID, Nombre, RazonSocial, Email, Telefono, Activo FROM dbo.Cliente WHERE ID=@ID AND IdAgencia=@IdAgencia; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Cliente_CambiarEstado @ID INT, @IdAgencia INT, @Activo BIT AS
BEGIN UPDATE dbo.Cliente SET Activo=@Activo WHERE ID=@ID AND IdAgencia=@IdAgencia; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Proyecto_ConsultarResponsables @IdAgencia INT AS SELECT P.ID, U.IdAgencia, U.Nombre, U.Apellido, U.Email FROM dbo.PM P INNER JOIN dbo.Usuario U ON U.ID = P.IdUsuario WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND P.Activo = 1;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Proyecto_Consultar @IdAgencia INT AS SELECT * FROM dbo.Proyecto WHERE IdAgencia = @IdAgencia ORDER BY Activo DESC, FechaAlta DESC;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Proyecto_Registrar @ID INT = 0, @IdAgencia INT, @IdCliente INT, @IdPMResponsable INT, @Nombre NVARCHAR(200), @Descripcion NVARCHAR(MAX) = NULL, @FechaInicio DATETIME2 = NULL, @Deadline DATETIME2 = NULL, @HorasEstimadasTotales DECIMAL(18,2), @Estado NVARCHAR(50) AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PM P INNER JOIN dbo.Usuario U ON U.ID=P.IdUsuario WHERE P.ID=@IdPMResponsable AND U.IdAgencia=@IdAgencia)
        SELECT @IdPMResponsable=P.ID FROM dbo.PM P INNER JOIN dbo.Usuario U ON U.ID=P.IdUsuario WHERE P.IdUsuario=@IdPMResponsable AND U.IdAgencia=@IdAgencia;
    IF @IdPMResponsable IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.PM WHERE ID=@IdPMResponsable) THROW 50001, 'El responsable seleccionado no es un PM activo de la agencia.', 1;
    INSERT INTO dbo.Proyecto (IdAgencia, IdCliente, IdPMResponsable, Nombre, Descripcion, FechaInicio, Deadline, HorasEstimadasTotales, Estado, Activo, FechaAlta) VALUES (@IdAgencia,@IdCliente,@IdPMResponsable,@Nombre,@Descripcion,@FechaInicio,@Deadline,@HorasEstimadasTotales,@Estado,1,GETDATE()); SELECT * FROM dbo.Proyecto WHERE ID=SCOPE_IDENTITY();
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Proyecto_Modificar @ID INT, @IdAgencia INT, @IdCliente INT, @IdPMResponsable INT, @Nombre NVARCHAR(200), @Descripcion NVARCHAR(MAX) = NULL, @FechaInicio DATETIME2 = NULL, @Deadline DATETIME2 = NULL, @HorasEstimadasTotales DECIMAL(18,2), @Estado NVARCHAR(50) AS
BEGIN UPDATE dbo.Proyecto SET IdCliente=@IdCliente,IdPMResponsable=@IdPMResponsable,Nombre=@Nombre,Descripcion=@Descripcion,FechaInicio=@FechaInicio,Deadline=@Deadline,HorasEstimadasTotales=@HorasEstimadasTotales,Estado=@Estado WHERE ID=@ID AND IdAgencia=@IdAgencia; SELECT * FROM dbo.Proyecto WHERE ID=@ID AND IdAgencia=@IdAgencia; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Proyecto_CambiarEstado @ID INT,@IdAgencia INT,@Activo BIT AS UPDATE dbo.Proyecto SET Activo=@Activo,FechaBaja=CASE WHEN @Activo=0 THEN GETDATE() ELSE NULL END WHERE ID=@ID AND IdAgencia=@IdAgencia;
GO
CREATE OR ALTER PROCEDURE dbo.usp_EstadoTarea_Consultar @IdAgencia INT AS SELECT * FROM dbo.EstadoTarea WHERE IdAgencia=@IdAgencia AND Activo=1 ORDER BY Orden;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ConsultarEmpleados @IdAgencia INT AS SELECT E.ID,U.IdAgencia,U.Nombre,U.Apellido,U.Email FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE U.IdAgencia=@IdAgencia AND U.Activo=1 AND E.Activo=1;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Tarea_Consultar @IdAgencia INT AS SELECT T.* FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE P.IdAgencia=@IdAgencia ORDER BY T.Activo DESC,T.Deadline;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Tarea_Registrar @ID INT=0,@IdAgencia INT,@IdProyecto INT,@IdEmpleadoAsignado INT=NULL,@IdSkillRequerido INT,@IdEstadoTarea INT,@Titulo NVARCHAR(200),@Descripcion NVARCHAR(MAX)=NULL,@Prioridad NVARCHAR(50)=NULL,@Complejidad NVARCHAR(50)=NULL,@FechaInicio DATETIME2=NULL,@Deadline DATETIME2=NULL,@SeniorityRequerido NVARCHAR(50)=NULL,@PorcentajeAvance DECIMAL(5,2)=0,@HorasEstimadas DECIMAL(18,2)=0 AS
BEGIN
    IF @IdEmpleadoAsignado IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE E.ID=@IdEmpleadoAsignado AND U.IdAgencia=@IdAgencia)
        SELECT @IdEmpleadoAsignado=E.ID FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE E.IdUsuario=@IdEmpleadoAsignado AND U.IdAgencia=@IdAgencia;
    IF @IdEmpleadoAsignado IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Empleado WHERE ID=@IdEmpleadoAsignado) THROW 50002, 'El empleado asignado no pertenece a la agencia.', 1;
    INSERT INTO dbo.Tarea (IdProyecto,IdEmpleadoAsignado,IdSkillRequerido,IdEstadoTarea,Titulo,Descripcion,Prioridad,Complejidad,FechaInicio,Deadline,SeniorityRequerido,PorcentajeAvance,HorasEstimadas,Activo,Bloqueada) VALUES (@IdProyecto,@IdEmpleadoAsignado,@IdSkillRequerido,@IdEstadoTarea,@Titulo,@Descripcion,@Prioridad,@Complejidad,@FechaInicio,@Deadline,@SeniorityRequerido,@PorcentajeAvance,@HorasEstimadas,1,0); SELECT T.* FROM dbo.Tarea T WHERE T.ID=SCOPE_IDENTITY();
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Tarea_Modificar @ID INT,@IdAgencia INT,@IdProyecto INT,@IdEmpleadoAsignado INT=NULL,@IdSkillRequerido INT,@IdEstadoTarea INT,@Titulo NVARCHAR(200),@Descripcion NVARCHAR(MAX)=NULL,@Prioridad NVARCHAR(50)=NULL,@Complejidad NVARCHAR(50)=NULL,@FechaInicio DATETIME2=NULL,@Deadline DATETIME2=NULL,@SeniorityRequerido NVARCHAR(50)=NULL,@PorcentajeAvance DECIMAL(5,2)=0,@HorasEstimadas DECIMAL(18,2)=0 AS
BEGIN
    IF @IdEmpleadoAsignado IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE E.ID=@IdEmpleadoAsignado AND U.IdAgencia=@IdAgencia)
        SELECT @IdEmpleadoAsignado=E.ID FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE E.IdUsuario=@IdEmpleadoAsignado AND U.IdAgencia=@IdAgencia;
    IF @IdEmpleadoAsignado IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Empleado WHERE ID=@IdEmpleadoAsignado) THROW 50002, 'El empleado asignado no pertenece a la agencia.', 1;
    UPDATE T SET IdProyecto=@IdProyecto,IdEmpleadoAsignado=@IdEmpleadoAsignado,IdSkillRequerido=@IdSkillRequerido,IdEstadoTarea=@IdEstadoTarea,Titulo=@Titulo,Descripcion=@Descripcion,Prioridad=@Prioridad,Complejidad=@Complejidad,FechaInicio=@FechaInicio,Deadline=@Deadline,SeniorityRequerido=@SeniorityRequerido,PorcentajeAvance=@PorcentajeAvance,HorasEstimadas=@HorasEstimadas FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE T.ID=@ID AND P.IdAgencia=@IdAgencia; SELECT T.* FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE T.ID=@ID AND P.IdAgencia=@IdAgencia;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Tarea_CambiarEstado @ID INT,@IdAgencia INT,@Activo BIT AS UPDATE T SET Activo=@Activo,FechaBaja=CASE WHEN @Activo=0 THEN GETDATE() ELSE NULL END FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE T.ID=@ID AND P.IdAgencia=@IdAgencia;
GO
