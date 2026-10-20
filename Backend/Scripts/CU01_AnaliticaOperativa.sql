/* CU01-001, CU01-004, CU01-005 y CU01-006 - datos para análisis gerenciales. */
GO

CREATE OR ALTER PROCEDURE dbo.usp_Analitica_ConsultarEmpleadosActivos
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT E.ID, CONCAT(U.Nombre, N' ', U.Apellido) AS Nombre, U.Email, E.Activo,
           E.CostoHora,
           COALESCE(D.HorasSemanales, E.HorasDisponiblesSemanales, 0) AS HorasDisponiblesSemanales,
           E.Seniority, E.EstadoLaboral, E.FechaIngreso
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    OUTER APPLY
    (
        SELECT TOP (1) DB.HorasSemanales
        FROM dbo.DisponibilidadBase DB
        WHERE DB.IdEmpleado = E.ID AND DB.Activo = 1
        ORDER BY DB.ID DESC
    ) D
    WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1
    ORDER BY U.Apellido, U.Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Analitica_ConsultarIdPM
    @IdUsuario INT,
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PM.ID
    FROM dbo.PM PM
    INNER JOIN dbo.Usuario U ON U.ID = PM.IdUsuario
    WHERE PM.IdUsuario = @IdUsuario AND U.IdAgencia = @IdAgencia;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Analitica_ConsultarDuenosAgencia
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT U.ID, CONCAT(U.Nombre, N' ', U.Apellido) AS Nombre, U.Email
    FROM dbo.Usuario U
    INNER JOIN dbo.UsuarioRol UR ON UR.IdUsuario = U.ID
    INNER JOIN dbo.Rol R ON R.ID = UR.IdRol
    WHERE U.IdAgencia = @IdAgencia
      AND U.Activo = 1
      AND R.TipoUsuario = N'Dueno';
END;
GO

/* CU01-006: el PM puede consultar el desvío de horas de sus propios proyectos. */
INSERT INTO dbo.RolPermiso(IdRol, IdPermiso)
SELECT rol.ID, permiso.ID
FROM dbo.Rol rol
INNER JOIN dbo.Permiso permiso ON permiso.Codigo = N'ConsultarTableroEjecutivo'
WHERE rol.TipoUsuario = N'PM'
  AND rol.EsRolBase = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.RolPermiso rolPermiso
      WHERE rolPermiso.IdRol = rol.ID
        AND rolPermiso.IdPermiso = permiso.ID
  );
GO

IF OBJECT_ID(N'dbo.SolicitudRevisionOperativa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SolicitudRevisionOperativa
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SolicitudRevisionOperativa PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdEmpleado INT NOT NULL,
        IdProyecto INT NULL,
        IdTarea INT NULL,
        IdPMResponsable INT NOT NULL,
        IdUsuarioSolicitante INT NOT NULL,
        CausaPrincipal NVARCHAR(1500) NOT NULL,
        AccionSugerida NVARCHAR(1500) NOT NULL,
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_SolicitudRevisionOperativa_Estado DEFAULT N'Pendiente',
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_SolicitudRevisionOperativa_FechaAlta DEFAULT SYSDATETIME(),
        CONSTRAINT FK_SolicitudRevisionOperativa_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_SolicitudRevisionOperativa_Empleado FOREIGN KEY(IdEmpleado) REFERENCES dbo.Empleado(ID),
        CONSTRAINT FK_SolicitudRevisionOperativa_Proyecto FOREIGN KEY(IdProyecto) REFERENCES dbo.Proyecto(ID),
        CONSTRAINT FK_SolicitudRevisionOperativa_Tarea FOREIGN KEY(IdTarea) REFERENCES dbo.Tarea(ID),
        CONSTRAINT FK_SolicitudRevisionOperativa_PM FOREIGN KEY(IdPMResponsable) REFERENCES dbo.PM(ID),
        CONSTRAINT FK_SolicitudRevisionOperativa_Usuario FOREIGN KEY(IdUsuarioSolicitante) REFERENCES dbo.Usuario(ID)
    );
    CREATE INDEX IX_SolicitudRevisionOperativa_PM_Estado ON dbo.SolicitudRevisionOperativa(IdPMResponsable, Estado, FechaAlta DESC);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SolicitudRevisionOperativa_Registrar
    @IdAgencia INT,
    @IdEmpleado INT,
    @IdProyecto INT = NULL,
    @IdTarea INT = NULL,
    @IdPMResponsable INT,
    @IdUsuarioSolicitante INT,
    @CausaPrincipal NVARCHAR(1500),
    @AccionSugerida NVARCHAR(1500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SolicitudRevisionOperativa(IdAgencia, IdEmpleado, IdProyecto, IdTarea, IdPMResponsable, IdUsuarioSolicitante, CausaPrincipal, AccionSugerida, Estado)
    VALUES(@IdAgencia, @IdEmpleado, @IdProyecto, @IdTarea, @IdPMResponsable, @IdUsuarioSolicitante, @CausaPrincipal, @AccionSugerida, N'Pendiente');
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SolicitudRevisionOperativa_ConsultarPendientesPM
    @IdAgencia INT,
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT S.*, CONCAT(UE.Nombre, N' ', UE.Apellido) AS NombreEmpleado,
           COALESCE(P.Nombre, N'Sin proyecto asociado') AS NombreProyecto,
           CONCAT(UP.Nombre, N' ', UP.Apellido) AS NombrePM
    FROM dbo.SolicitudRevisionOperativa S
    INNER JOIN dbo.Empleado E ON E.ID = S.IdEmpleado
    INNER JOIN dbo.Usuario UE ON UE.ID = E.IdUsuario
    INNER JOIN dbo.PM PM ON PM.ID = S.IdPMResponsable
    INNER JOIN dbo.Usuario UP ON UP.ID = PM.IdUsuario
    LEFT JOIN dbo.Proyecto P ON P.ID = S.IdProyecto
    WHERE S.IdAgencia = @IdAgencia AND S.IdPMResponsable = @IdPMResponsable AND S.Estado = N'Pendiente'
    ORDER BY S.FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Analitica_ConsultarDestinatarioPM
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT U.ID, CONCAT(U.Nombre, N' ', U.Apellido) AS Nombre, U.Email
    FROM dbo.PM PM
    INNER JOIN dbo.Usuario U ON U.ID = PM.IdUsuario
    WHERE PM.ID = @IdPMResponsable AND PM.Activo = 1 AND U.Activo = 1;
END;
GO
