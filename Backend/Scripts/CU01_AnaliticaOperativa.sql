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

CREATE OR ALTER PROCEDURE dbo.usp_SolicitudRevisionOperativa_MarcarResuelta
    @IdSolicitud INT,
    @IdAgencia INT,
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SolicitudRevisionOperativa
    SET Estado = N'Resuelta'
    WHERE ID = @IdSolicitud
      AND IdAgencia = @IdAgencia
      AND IdPMResponsable = @IdPMResponsable
      AND Estado = N'Pendiente';

    SELECT CONVERT(bit, CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END) AS Actualizada;
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

IF OBJECT_ID(N'dbo.SeguimientoConvenienciaOperativa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SeguimientoConvenienciaOperativa
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeguimientoConvenienciaOperativa PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdCliente INT NOT NULL,
        IdProyecto INT NULL,
        IdUsuario INT NOT NULL,
        Observacion NVARCHAR(1500) NOT NULL,
        AccionSugerida NVARCHAR(1500) NOT NULL,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_SeguimientoConvenienciaOperativa_FechaAlta DEFAULT SYSDATETIME(),
        CONSTRAINT FK_SeguimientoConvenienciaOperativa_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_SeguimientoConvenienciaOperativa_Cliente FOREIGN KEY(IdCliente) REFERENCES dbo.Cliente(ID),
        CONSTRAINT FK_SeguimientoConvenienciaOperativa_Proyecto FOREIGN KEY(IdProyecto) REFERENCES dbo.Proyecto(ID),
        CONSTRAINT FK_SeguimientoConvenienciaOperativa_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuario(ID)
    );
    CREATE INDEX IX_SeguimientoConvenienciaOperativa_ClienteProyecto ON dbo.SeguimientoConvenienciaOperativa(IdAgencia, IdCliente, IdProyecto, FechaAlta DESC);
END;
GO

IF OBJECT_ID(N'dbo.ClienteRevisionConveniencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClienteRevisionConveniencia
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClienteRevisionConveniencia PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdCliente INT NOT NULL,
        IdUsuarioMarcador INT NOT NULL,
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_ClienteRevisionConveniencia_Estado DEFAULT N'Marcado',
        FechaMarcado DATETIME2 NOT NULL CONSTRAINT DF_ClienteRevisionConveniencia_FechaMarcado DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_ClienteRevisionConveniencia UNIQUE(IdAgencia, IdCliente),
        CONSTRAINT FK_ClienteRevisionConveniencia_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_ClienteRevisionConveniencia_Cliente FOREIGN KEY(IdCliente) REFERENCES dbo.Cliente(ID),
        CONSTRAINT FK_ClienteRevisionConveniencia_Usuario FOREIGN KEY(IdUsuarioMarcador) REFERENCES dbo.Usuario(ID)
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_ConsultarClientesEnRevision
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdCliente
    FROM dbo.ClienteRevisionConveniencia
    WHERE IdAgencia = @IdAgencia AND Estado = N'Marcado';
END;
GO

/* Revisiones derivadas desde Control de horas y Riesgo de retraso. */
IF OBJECT_ID(N'dbo.SolicitudRevisionGestion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SolicitudRevisionGestion
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SolicitudRevisionGestion PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdProyecto INT NOT NULL,
        IdTarea INT NULL,
        IdPMResponsable INT NOT NULL,
        IdUsuarioSolicitante INT NOT NULL,
        Tipo NVARCHAR(40) NOT NULL,
        CausaPrincipal NVARCHAR(1500) NOT NULL,
        AccionSugerida NVARCHAR(1500) NOT NULL,
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_SolicitudRevisionGestion_Estado DEFAULT N'Pendiente',
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_SolicitudRevisionGestion_FechaAlta DEFAULT SYSDATETIME(),
        FechaResolucion DATETIME2 NULL,
        CONSTRAINT FK_SolicitudRevisionGestion_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_SolicitudRevisionGestion_Proyecto FOREIGN KEY(IdProyecto) REFERENCES dbo.Proyecto(ID),
        CONSTRAINT FK_SolicitudRevisionGestion_Tarea FOREIGN KEY(IdTarea) REFERENCES dbo.Tarea(ID),
        CONSTRAINT FK_SolicitudRevisionGestion_PM FOREIGN KEY(IdPMResponsable) REFERENCES dbo.PM(ID),
        CONSTRAINT FK_SolicitudRevisionGestion_Usuario FOREIGN KEY(IdUsuarioSolicitante) REFERENCES dbo.Usuario(ID),
        CONSTRAINT CK_SolicitudRevisionGestion_Tipo CHECK (Tipo IN (N'ControlHoras', N'RiesgoRetraso'))
    );
    CREATE INDEX IX_SolicitudRevisionGestion_PendientesPM ON dbo.SolicitudRevisionGestion(IdAgencia, IdPMResponsable, Estado, FechaAlta DESC);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SolicitudRevisionGestion_Registrar
    @IdAgencia INT,
    @IdProyecto INT,
    @IdTarea INT = NULL,
    @IdPMResponsable INT,
    @IdUsuarioSolicitante INT,
    @Tipo NVARCHAR(40),
    @CausaPrincipal NVARCHAR(1500),
    @AccionSugerida NVARCHAR(1500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SolicitudRevisionGestion(IdAgencia, IdProyecto, IdTarea, IdPMResponsable, IdUsuarioSolicitante, Tipo, CausaPrincipal, AccionSugerida)
    VALUES(@IdAgencia, @IdProyecto, @IdTarea, @IdPMResponsable, @IdUsuarioSolicitante, @Tipo, @CausaPrincipal, @AccionSugerida);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SolicitudRevisionGestion_ConsultarPendientesPM
    @IdAgencia INT,
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT S.*, P.Nombre AS NombreProyecto, T.Titulo AS TituloTarea
    FROM dbo.SolicitudRevisionGestion S
    INNER JOIN dbo.Proyecto P ON P.ID = S.IdProyecto
    LEFT JOIN dbo.Tarea T ON T.ID = S.IdTarea
    WHERE S.IdAgencia = @IdAgencia AND S.IdPMResponsable = @IdPMResponsable AND S.Estado = N'Pendiente'
    ORDER BY S.FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SolicitudRevisionGestion_MarcarResuelta
    @IdSolicitud INT,
    @IdAgencia INT,
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SolicitudRevisionGestion
    SET Estado = N'Resuelta', FechaResolucion = SYSDATETIME()
    WHERE ID = @IdSolicitud AND IdAgencia = @IdAgencia AND IdPMResponsable = @IdPMResponsable AND Estado = N'Pendiente';
    SELECT CONVERT(bit, CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END) AS Actualizada;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_RegistrarSeguimiento
    @IdAgencia INT,
    @IdCliente INT,
    @IdProyecto INT = NULL,
    @IdUsuario INT,
    @Observacion NVARCHAR(1500),
    @AccionSugerida NVARCHAR(1500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SeguimientoConvenienciaOperativa(IdAgencia, IdCliente, IdProyecto, IdUsuario, Observacion, AccionSugerida)
    VALUES(@IdAgencia, @IdCliente, @IdProyecto, @IdUsuario, @Observacion, @AccionSugerida);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_ConsultarSeguimientos
    @IdAgencia INT,
    @IdCliente INT,
    @IdProyecto INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT S.ID, S.IdCliente, S.IdProyecto, S.Observacion, S.AccionSugerida, S.FechaAlta,
           CONCAT(U.Nombre, N' ', U.Apellido) AS Usuario
    FROM dbo.SeguimientoConvenienciaOperativa S
    INNER JOIN dbo.Usuario U ON U.ID = S.IdUsuario
    WHERE S.IdAgencia = @IdAgencia
      AND S.IdCliente = @IdCliente
      AND (S.IdProyecto IS NULL OR S.IdProyecto = @IdProyecto)
    ORDER BY S.FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_MarcarClienteRevision
    @IdAgencia INT,
    @IdCliente INT,
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ClienteRevisionConveniencia
    SET Estado = N'Marcado', IdUsuarioMarcador = @IdUsuario, FechaMarcado = SYSDATETIME()
    WHERE IdAgencia = @IdAgencia AND IdCliente = @IdCliente;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.ClienteRevisionConveniencia(IdAgencia, IdCliente, IdUsuarioMarcador, Estado)
        VALUES(@IdAgencia, @IdCliente, @IdUsuario, N'Marcado');
END;
GO

/* Las revisiones de conveniencia son de cliente/PM, no de una asignación puntual.
   Se guardan por separado para no forzar un empleado ficticio en la bandeja del PM. */
IF OBJECT_ID(N'dbo.SolicitudRevisionConveniencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SolicitudRevisionConveniencia
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SolicitudRevisionConveniencia PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdCliente INT NOT NULL,
        IdPMResponsable INT NOT NULL,
        IdUsuarioSolicitante INT NOT NULL,
        CausaPrincipal NVARCHAR(1500) NOT NULL,
        AccionSugerida NVARCHAR(1500) NOT NULL,
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_SolicitudRevisionConveniencia_Estado DEFAULT N'Pendiente',
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_SolicitudRevisionConveniencia_FechaAlta DEFAULT SYSDATETIME(),
        FechaResolucion DATETIME2 NULL,
        CONSTRAINT FK_SolicitudRevisionConveniencia_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_SolicitudRevisionConveniencia_Cliente FOREIGN KEY(IdCliente) REFERENCES dbo.Cliente(ID),
        CONSTRAINT FK_SolicitudRevisionConveniencia_PM FOREIGN KEY(IdPMResponsable) REFERENCES dbo.PM(ID),
        CONSTRAINT FK_SolicitudRevisionConveniencia_Usuario FOREIGN KEY(IdUsuarioSolicitante) REFERENCES dbo.Usuario(ID)
    );
    CREATE INDEX IX_SolicitudRevisionConveniencia_PM_Estado ON dbo.SolicitudRevisionConveniencia(IdPMResponsable, Estado, FechaAlta DESC);
    CREATE INDEX IX_SolicitudRevisionConveniencia_Cliente_Estado ON dbo.SolicitudRevisionConveniencia(IdAgencia, IdCliente, Estado);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_RegistrarSolicitudRevision
    @IdAgencia INT,
    @IdCliente INT,
    @IdPMResponsable INT,
    @IdUsuarioSolicitante INT,
    @CausaPrincipal NVARCHAR(1500),
    @AccionSugerida NVARCHAR(1500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdExistente INT =
    (
        SELECT TOP (1) ID
        FROM dbo.SolicitudRevisionConveniencia
        WHERE IdAgencia = @IdAgencia AND IdCliente = @IdCliente
          AND IdPMResponsable = @IdPMResponsable AND Estado = N'Pendiente'
        ORDER BY FechaAlta DESC
    );

    IF @IdExistente IS NOT NULL
    BEGIN
        SELECT @IdExistente AS ID, CONVERT(bit, 0) AS Creada;
        RETURN;
    END;

    INSERT INTO dbo.SolicitudRevisionConveniencia(IdAgencia, IdCliente, IdPMResponsable, IdUsuarioSolicitante, CausaPrincipal, AccionSugerida)
    VALUES(@IdAgencia, @IdCliente, @IdPMResponsable, @IdUsuarioSolicitante, @CausaPrincipal, @AccionSugerida);

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID, CONVERT(bit, 1) AS Creada;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_ConsultarSolicitudesPendientesPM
    @IdAgencia INT,
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT S.*, C.Nombre AS NombreCliente, CONCAT(U.Nombre, N' ', U.Apellido) AS NombrePM
    FROM dbo.SolicitudRevisionConveniencia S
    INNER JOIN dbo.Cliente C ON C.ID = S.IdCliente
    INNER JOIN dbo.PM PM ON PM.ID = S.IdPMResponsable
    INNER JOIN dbo.Usuario U ON U.ID = PM.IdUsuario
    WHERE S.IdAgencia = @IdAgencia AND S.IdPMResponsable = @IdPMResponsable AND S.Estado = N'Pendiente'
    ORDER BY S.FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_MarcarSolicitudRevisionResuelta
    @IdSolicitud INT,
    @IdAgencia INT,
    @IdPMResponsable INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdCliente INT;
    SELECT @IdCliente = IdCliente
    FROM dbo.SolicitudRevisionConveniencia
    WHERE ID = @IdSolicitud AND IdAgencia = @IdAgencia AND IdPMResponsable = @IdPMResponsable AND Estado = N'Pendiente';

    UPDATE dbo.SolicitudRevisionConveniencia
    SET Estado = N'Resuelta', FechaResolucion = SYSDATETIME()
    WHERE ID = @IdSolicitud AND IdAgencia = @IdAgencia AND IdPMResponsable = @IdPMResponsable AND Estado = N'Pendiente';

    DECLARE @Actualizada bit = CONVERT(bit, CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END);

    IF @Actualizada = 1
       AND NOT EXISTS
       (
           SELECT 1 FROM dbo.SolicitudRevisionConveniencia
           WHERE IdAgencia = @IdAgencia AND IdCliente = @IdCliente AND Estado = N'Pendiente'
       )
    BEGIN
        UPDATE dbo.ClienteRevisionConveniencia
        SET Estado = N'Resuelta', FechaMarcado = SYSDATETIME()
        WHERE IdAgencia = @IdAgencia AND IdCliente = @IdCliente;
    END;

    SELECT @Actualizada AS Actualizada;
END;
GO

/* Un cliente aparece "en revisión" sólo mientras al menos un PM tenga una solicitud pendiente. */
CREATE OR ALTER PROCEDURE dbo.usp_Conveniencia_ConsultarClientesEnRevision
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT C.IdCliente
    FROM dbo.ClienteRevisionConveniencia C
    INNER JOIN dbo.SolicitudRevisionConveniencia S ON S.IdAgencia = C.IdAgencia AND S.IdCliente = C.IdCliente AND S.Estado = N'Pendiente'
    WHERE C.IdAgencia = @IdAgencia AND C.Estado = N'Marcado';
END;
GO
