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
    SELECT T.*, COALESCE(NULLIF(LTRIM(RTRIM(T.Estado)), N''), ET.Nombre, N'Sin estado') AS NombreEstadoTarea
    FROM dbo.Tarea T
    LEFT JOIN dbo.EstadoTarea ET ON ET.ID = T.IdEstadoTarea
    WHERE T.IdProyecto = @IdProyecto AND T.Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_ConsultarEquipoProyecto @IdProyecto INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT E.ID, U.Nombre, U.Email, E.Activo, E.CostoHora,
        COALESCE(D.HorasSemanales, E.HorasDisponiblesSemanales, 0) AS HorasDisponiblesSemanales,
        E.Seniority, E.EstadoLaboral, E.FechaIngreso
    FROM dbo.Tarea T
    INNER JOIN dbo.Empleado E ON E.Activo = 1
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    OUTER APPLY (SELECT TOP (1) DB.HorasSemanales FROM dbo.DisponibilidadBase DB WHERE DB.IdEmpleado = E.ID AND DB.Activo = 1 ORDER BY DB.ID DESC) D
    WHERE T.IdProyecto = @IdProyecto AND T.Activo = 1 AND U.Activo = 1
      AND (EXISTS (SELECT 1 FROM dbo.AsignacionTarea AT WHERE AT.IdTarea = T.ID AND AT.IdEmpleado = E.ID AND AT.Activa = 1)
           OR (NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea AT WHERE AT.IdTarea = T.ID AND AT.Activa = 1) AND T.IdEmpleadoAsignado = E.ID));
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

IF OBJECT_ID(N'dbo.SeguimientoRiesgoRetraso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SeguimientoRiesgoRetraso
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeguimientoRiesgoRetraso PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdProyecto INT NOT NULL,
        IdTarea INT NULL,
        IdUsuario INT NOT NULL,
        Observacion NVARCHAR(1500) NOT NULL,
        ComunicarResponsable BIT NOT NULL CONSTRAINT DF_SeguimientoRiesgoRetraso_ComunicarResponsable DEFAULT 0,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_SeguimientoRiesgoRetraso_FechaAlta DEFAULT SYSDATETIME(),
        CONSTRAINT FK_SeguimientoRiesgoRetraso_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_SeguimientoRiesgoRetraso_Proyecto FOREIGN KEY(IdProyecto) REFERENCES dbo.Proyecto(ID),
        CONSTRAINT FK_SeguimientoRiesgoRetraso_Tarea FOREIGN KEY(IdTarea) REFERENCES dbo.Tarea(ID),
        CONSTRAINT FK_SeguimientoRiesgoRetraso_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuario(ID)
    );
    CREATE INDEX IX_SeguimientoRiesgoRetraso_Proyecto ON dbo.SeguimientoRiesgoRetraso(IdAgencia, IdProyecto, FechaAlta DESC);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_RegistrarSeguimiento
    @IdAgencia INT,
    @IdProyecto INT,
    @IdTarea INT = NULL,
    @IdUsuario INT,
    @Observacion NVARCHAR(1500),
    @ComunicarResponsable BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SeguimientoRiesgoRetraso(IdAgencia, IdProyecto, IdTarea, IdUsuario, Observacion, ComunicarResponsable)
    VALUES(@IdAgencia, @IdProyecto, @IdTarea, @IdUsuario, @Observacion, @ComunicarResponsable);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RiesgoRetraso_ConsultarDestinatarioSeguimiento
    @IdProyecto INT,
    @IdTarea INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) U.ID, CONCAT(U.Nombre, N' ', U.Apellido) AS Nombre, U.Email
    FROM dbo.Proyecto P
    INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable AND PM.Activo = 1
    OUTER APPLY
    (
        SELECT TOP (1) E.IdUsuario
        FROM dbo.Tarea T
        LEFT JOIN dbo.AsignacionTarea AT ON AT.IdTarea = T.ID AND AT.Activa = 1
        LEFT JOIN dbo.Empleado E ON E.ID = COALESCE(AT.IdEmpleado, T.IdEmpleadoAsignado) AND E.Activo = 1
        WHERE T.ID = @IdTarea AND T.IdProyecto = P.ID AND T.Activo = 1
        ORDER BY CASE WHEN AT.ID IS NULL THEN 1 ELSE 0 END, AT.ID
    ) RT
    INNER JOIN dbo.Usuario U ON U.ID = COALESCE(RT.IdUsuario, PM.IdUsuario)
    WHERE P.ID = @IdProyecto AND P.Activo = 1 AND U.Activo = 1;
END
GO
