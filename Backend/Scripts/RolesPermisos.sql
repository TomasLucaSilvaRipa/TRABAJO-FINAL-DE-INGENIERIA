/*
  Roles, permisos y usuarios de agencia.
  Los permisos son estáticos y representan URL navegables. Los roles son administrables por Soporte.
*/
GO

IF OBJECT_ID(N'dbo.UsuarioRol', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioRol
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsuarioRol PRIMARY KEY,
        IdUsuario INT NOT NULL,
        IdRol INT NOT NULL,
        CONSTRAINT UQ_UsuarioRol UNIQUE(IdUsuario, IdRol),
        CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY(IdRol) REFERENCES dbo.Rol(ID)
    );
END;
GO

IF COL_LENGTH(N'dbo.Rol', N'IdAgencia') IS NULL
    ALTER TABLE dbo.Rol ADD IdAgencia INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Rol_Agencia')
    ALTER TABLE dbo.Rol ADD CONSTRAINT FK_Rol_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID);
GO

IF COL_LENGTH(N'dbo.Rol', N'TipoUsuario') IS NULL
    ALTER TABLE dbo.Rol ADD TipoUsuario NVARCHAR(20) NULL;
GO

UPDATE dbo.Rol
SET TipoUsuario = CASE
    WHEN Nombre LIKE N'Due%' THEN N'Dueno'
    WHEN Nombre = N'PM' THEN N'PM'
    WHEN Nombre = N'Empleado' THEN N'Empleado'
    WHEN Nombre = N'Soporte' THEN N'Soporte'
    WHEN TipoUsuario IS NULL OR TipoUsuario = N'' THEN N'Empleado'
    ELSE TipoUsuario
END
WHERE EsRolBase = 1 OR TipoUsuario IS NULL OR TipoUsuario = N'';
GO

IF COL_LENGTH(N'dbo.Permiso', N'Codigo') IS NULL
    ALTER TABLE dbo.Permiso ADD Codigo NVARCHAR(100) NULL;
GO

IF COL_LENGTH(N'dbo.Permiso', N'Url') IS NULL
    ALTER TABLE dbo.Permiso ADD Url NVARCHAR(250) NULL;
GO

UPDATE dbo.Permiso
SET Codigo = Nombre
WHERE Codigo IS NULL;
GO

UPDATE dbo.Permiso
SET Url = CASE Codigo
    WHEN N'GestionarAgencia' THEN N'/dashboard/configuracion-agencia'
    WHEN N'GestionarUsuarios' THEN N'/dashboard/empleados'
    WHEN N'GestionarProyectos' THEN N'/dashboard/proyectos'
    WHEN N'GestionarTareas' THEN N'/dashboard/tareas'
    WHEN N'RegistrarHoras' THEN N'/dashboard/registrar-horas'
    WHEN N'ConsultarTableroEjecutivo' THEN N'/dashboard'
    WHEN N'UsarBestFit' THEN N'/dashboard/best-fit'
    WHEN N'GestionarDisponibilidad' THEN N'/dashboard/disponibilidad'
    WHEN N'ConsultarBitacora' THEN N'/dashboard/bitacora'
    WHEN N'GestionarSuscripcion' THEN N'/dashboard/suscripcion'
    WHEN N'SimularImpacto' THEN N'/dashboard/simulacion-impacto'
    WHEN N'ConsultarNotificaciones' THEN N'/dashboard/notificaciones'
    WHEN N'ConsultarAyuda' THEN N'/dashboard/ayuda'
    ELSE Url
END
WHERE Url IS NULL;
GO

MERGE dbo.Permiso AS destino
USING (VALUES
    (N'VerDashboard', N'Consultar el panel principal.', N'/dashboard'),
    (N'GestionarAgencia', N'Administrar datos generales de la agencia.', N'/dashboard/configuracion-agencia'),
    (N'GestionarUsuarios', N'Administrar usuarios de la agencia.', N'/dashboard/empleados'),
    (N'GestionarProyectos', N'Crear, modificar, cerrar y consultar proyectos.', N'/dashboard/proyectos'),
    (N'GestionarTareas', N'Crear, modificar, asignar y consultar tareas.', N'/dashboard/tareas'),
    (N'RegistrarHoras', N'Registrar horas trabajadas sobre tareas.', N'/dashboard/registrar-horas'),
    (N'ConsultarTableroEjecutivo', N'Consultar indicadores ejecutivos.', N'/dashboard'),
    (N'UsarBestFit', N'Solicitar recomendaciones del motor Best Fit.', N'/dashboard/best-fit'),
    (N'GestionarDisponibilidad', N'Gestionar disponibilidad y ausencias.', N'/dashboard/disponibilidad'),
    (N'ConsultarBitacora', N'Consultar la bitácora de actividad y accesos.', N'/dashboard/bitacora'),
    (N'GestionarSuscripcion', N'Consultar y administrar la suscripción.', N'/dashboard/suscripcion'),
    (N'GestionarRoles', N'Crear, modificar y dar de baja roles.', N'/dashboard/roles'),
    (N'GestionarPlanes', N'Administrar planes comerciales.', N'/dashboard/planes'),
    (N'VerCalendarioEquipo', N'Consultar el calendario del equipo.', N'/dashboard/calendario-equipo'),
    (N'SimularImpacto', N'Simular el impacto operativo de cambios de planificación.', N'/dashboard/simulacion-impacto'),
    (N'ConsultarNotificaciones', N'Consultar notificaciones propias.', N'/dashboard/notificaciones'),
    (N'ConsultarAyuda', N'Consultar la ayuda de TeamBalance.', N'/dashboard/ayuda'),
    (N'VerKanban', N'Consultar el tablero Kanban personal.', N'/dashboard/kanban'),
    (N'VerCargaOperativa', N'Consultar la carga operativa personal.', N'/dashboard/carga-operativa'),
    (N'Perfil', N'Consultar y actualizar el perfil propio.', N'/dashboard/profile'),
    (N'SeguridadCuenta', N'Administrar la seguridad de la cuenta propia.', N'/dashboard/seguridad')
) AS origen(Codigo, Descripcion, Url)
ON destino.Codigo = origen.Codigo
WHEN MATCHED THEN UPDATE SET Nombre = origen.Codigo, Descripcion = origen.Descripcion, Url = origen.Url, Activo = 1
WHEN NOT MATCHED THEN INSERT(Nombre, Descripcion, Activo, Codigo, Url) VALUES(origen.Codigo, origen.Descripcion, 1, origen.Codigo, origen.Url);
GO

UPDATE permiso
SET Codigo = Nombre
FROM dbo.Permiso permiso
WHERE permiso.Codigo IS NULL;
GO

DELETE rolPermiso
FROM dbo.RolPermiso rolPermiso
INNER JOIN dbo.Rol rol ON rol.ID = rolPermiso.IdRol
INNER JOIN dbo.Permiso permiso ON permiso.ID = rolPermiso.IdPermiso
WHERE rol.TipoUsuario <> N'Soporte' AND permiso.Codigo = N'ConsultarBitacora';
GO

DELETE rolPermiso
FROM dbo.RolPermiso rolPermiso
INNER JOIN dbo.Rol rol ON rol.ID = rolPermiso.IdRol
WHERE rol.EsRolBase = 1;
GO

INSERT INTO dbo.RolPermiso(IdRol, IdPermiso)
SELECT rol.ID, permiso.ID
FROM dbo.Rol rol
INNER JOIN dbo.Permiso permiso ON
    (rol.TipoUsuario = N'Dueno' AND permiso.Codigo IN (N'VerDashboard', N'GestionarAgencia', N'GestionarUsuarios', N'GestionarProyectos', N'GestionarTareas', N'ConsultarTableroEjecutivo', N'GestionarSuscripcion', N'GestionarRoles', N'Perfil', N'SeguridadCuenta', N'ConsultarNotificaciones', N'ConsultarAyuda'))
    OR (rol.TipoUsuario = N'PM' AND permiso.Codigo IN (N'VerDashboard', N'GestionarProyectos', N'GestionarTareas', N'UsarBestFit', N'GestionarDisponibilidad', N'VerCalendarioEquipo', N'SimularImpacto', N'Perfil', N'SeguridadCuenta', N'ConsultarNotificaciones', N'ConsultarAyuda'))
    OR (rol.TipoUsuario = N'Empleado' AND permiso.Codigo IN (N'VerDashboard', N'RegistrarHoras', N'GestionarDisponibilidad', N'VerKanban', N'VerCargaOperativa', N'Perfil', N'SeguridadCuenta', N'ConsultarNotificaciones', N'ConsultarAyuda'))
    OR (rol.TipoUsuario = N'Soporte' AND permiso.Codigo IN (N'VerDashboard', N'GestionarRoles', N'GestionarPlanes', N'ConsultarBitacora', N'Perfil', N'SeguridadCuenta', N'ConsultarNotificaciones', N'ConsultarAyuda'))
WHERE rol.EsRolBase = 1;
GO

INSERT INTO dbo.UsuarioRol(IdUsuario, IdRol)
SELECT usuario.ID, usuario.IdRol
FROM dbo.Usuario usuario
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.UsuarioRol usuarioRol
    WHERE usuarioRol.IdUsuario = usuario.ID AND usuarioRol.IdRol = usuario.IdRol
);
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_ConsultarActivos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, IdAgencia, Nombre, Descripcion, TipoUsuario, EsRolBase, Activo, FechaBaja
    FROM dbo.Rol
    WHERE Activo = 1
    ORDER BY EsRolBase DESC, Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_ConsultarPorUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT rol.ID, rol.IdAgencia, rol.Nombre, rol.Descripcion, rol.TipoUsuario, rol.EsRolBase, rol.Activo, rol.FechaBaja
    FROM dbo.UsuarioRol usuarioRol
    INNER JOIN dbo.Rol rol ON rol.ID = usuarioRol.IdRol
    WHERE usuarioRol.IdUsuario = @IdUsuario AND rol.Activo = 1
    ORDER BY rol.Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Permiso_ConsultarPorUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT permiso.ID, permiso.Nombre, permiso.Descripcion, permiso.Activo, permiso.Codigo, permiso.Url
    FROM dbo.UsuarioRol usuarioRol
    INNER JOIN dbo.RolPermiso rolPermiso ON rolPermiso.IdRol = usuarioRol.IdRol
    INNER JOIN dbo.Permiso permiso ON permiso.ID = rolPermiso.IdPermiso
    WHERE usuarioRol.IdUsuario = @IdUsuario AND permiso.Activo = 1
    ORDER BY permiso.Codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Permiso_ConsultarActivos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Nombre, Descripcion, Activo, Codigo, Url
    FROM dbo.Permiso
    WHERE Activo = 1
    ORDER BY Codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Permiso_ConsultarPorRol
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT permiso.ID, permiso.Nombre, permiso.Descripcion, permiso.Activo, permiso.Codigo, permiso.Url
    FROM dbo.RolPermiso rolPermiso
    INNER JOIN dbo.Permiso permiso ON permiso.ID = rolPermiso.IdPermiso
    WHERE rolPermiso.IdRol = @IdRol AND permiso.Activo = 1
    ORDER BY permiso.Codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_Registrar
    @IdAgencia INT = NULL,
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(500),
    @TipoUsuario NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM dbo.Rol WHERE Nombre = @Nombre)
        THROW 52001, 'Ya existe un rol con ese nombre.', 1;
    INSERT INTO dbo.Rol(IdAgencia, Nombre, Descripcion, TipoUsuario, EsRolBase, Activo) VALUES(@IdAgencia, @Nombre, NULLIF(@Descripcion, N''), @TipoUsuario, 0, 1);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_Modificar
    @IdRol INT,
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(500),
    @TipoUsuario NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM dbo.Rol WHERE Nombre = @Nombre AND ID <> @IdRol)
        THROW 52002, 'Ya existe otro rol con ese nombre.', 1;
    UPDATE dbo.Rol SET Nombre = @Nombre, Descripcion = NULLIF(@Descripcion, N''), TipoUsuario = @TipoUsuario WHERE ID = @IdRol AND EsRolBase = 0;
    IF @@ROWCOUNT = 0 THROW 52003, 'El rol no existe o es un rol base no modificable.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_CambiarEstado
    @IdRol INT,
    @Activo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Rol SET Activo = @Activo, FechaBaja = CASE WHEN @Activo = 0 THEN SYSDATETIME() ELSE NULL END WHERE ID = @IdRol AND EsRolBase = 0;
    IF @@ROWCOUNT = 0 THROW 52004, 'El rol no existe o es un rol base no modificable.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_LimpiarPermisos
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.RolPermiso WHERE IdRol = @IdRol;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_AsignarPermiso
    @IdRol INT,
    @IdPermiso INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.RolPermiso WHERE IdRol = @IdRol AND IdPermiso = @IdPermiso)
        INSERT INTO dbo.RolPermiso(IdRol, IdPermiso) VALUES(@IdRol, @IdPermiso);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarPorAgencia
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT usuario.ID, usuario.IdAgencia, usuario.IdRol, usuario.Nombre, usuario.Apellido, usuario.Email, usuario.PasswordHash, usuario.Estado, usuario.FechaAlta, usuario.Activo, usuario.FechaBaja,
           empleado.CostoHora, empleado.HorasDisponiblesSemanales, empleado.Seniority, empleado.EstadoLaboral, empleado.FechaIngreso
    FROM dbo.Usuario usuario
    LEFT JOIN dbo.Empleado empleado ON empleado.IdUsuario = usuario.ID
    WHERE usuario.IdAgencia = @IdAgencia
    ORDER BY usuario.Activo DESC, usuario.Apellido, usuario.Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_RegistrarGestion
    @IdAgencia INT = NULL,
    @IdRolPrincipal INT,
    @Nombre NVARCHAR(100),
    @Apellido NVARCHAR(100),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(1000),
    @Estado NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM dbo.Usuario WHERE Email = @Email)
        THROW 52005, 'Ya existe un usuario con ese email.', 1;
    INSERT INTO dbo.Usuario(IdAgencia, IdRol, Nombre, Apellido, Email, PasswordHash, Estado, FechaAlta, Activo)
    VALUES(@IdAgencia, @IdRolPrincipal, @Nombre, @Apellido, @Email, @PasswordHash, @Estado, SYSDATETIME(), 1);
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ModificarGestion
    @IdUsuario INT,
    @Nombre NVARCHAR(100),
    @Apellido NVARCHAR(100),
    @Email NVARCHAR(150),
    @IdRolPrincipal INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM dbo.Usuario WHERE Email = @Email AND ID <> @IdUsuario)
        THROW 52006, 'Ya existe otro usuario con ese email.', 1;
    UPDATE dbo.Usuario SET Nombre = @Nombre, Apellido = @Apellido, Email = @Email, IdRol = @IdRolPrincipal WHERE ID = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_LimpiarRoles
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.UsuarioRol WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_AsignarRol
    @IdUsuario INT,
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.UsuarioRol WHERE IdUsuario = @IdUsuario AND IdRol = @IdRol)
        INSERT INTO dbo.UsuarioRol(IdUsuario, IdRol) VALUES(@IdUsuario, @IdRol);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_CambiarEstado
    @IdUsuario INT,
    @Activo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Usuario SET Activo = @Activo, FechaBaja = CASE WHEN @Activo = 0 THEN SYSDATETIME() ELSE NULL END WHERE ID = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Empleado_RegistrarActualizar
    @IdUsuario INT,
    @CostoHora DECIMAL(18,2),
    @HorasDisponiblesSemanales DECIMAL(10,2),
    @Seniority NVARCHAR(100),
    @EstadoLaboral NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM dbo.Empleado WHERE IdUsuario = @IdUsuario)
        UPDATE dbo.Empleado SET CostoHora = @CostoHora, HorasDisponiblesSemanales = @HorasDisponiblesSemanales, Seniority = @Seniority, EstadoLaboral = @EstadoLaboral, Activo = 1, FechaBaja = NULL WHERE IdUsuario = @IdUsuario;
    ELSE
        INSERT INTO dbo.Empleado(IdUsuario, CostoHora, HorasDisponiblesSemanales, Seniority, EstadoLaboral, FechaIngreso, Activo) VALUES(@IdUsuario, @CostoHora, @HorasDisponiblesSemanales, @Seniority, @EstadoLaboral, SYSDATETIME(), 1);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PM_Registrar
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.PM WHERE IdUsuario = @IdUsuario)
        INSERT INTO dbo.PM(IdUsuario, AutorizadoGestionRecursos, PuedeExportarLegajos, Activo) VALUES(@IdUsuario, 1, 0, 1);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Soporte_Existe
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(bit, CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.UsuarioRol usuarioRol INNER JOIN dbo.Rol rol ON rol.ID = usuarioRol.IdRol
        WHERE rol.Nombre = N'Soporte' AND rol.Activo = 1
    ) THEN 1 ELSE 0 END) AS Existe;
END;
GO
