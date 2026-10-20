/*
  CU02-008 - Consultar carga operativa mediante Slack.
  Asociación mínima Slack User ID -> Usuario TeamBalance y consultas de lectura.
*/
GO

IF OBJECT_ID(N'dbo.UsuarioSlack', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioSlack
    (
        IdUsuario INT NOT NULL PRIMARY KEY,
        SlackUserId NVARCHAR(50) NOT NULL,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_UsuarioSlack_FechaAlta DEFAULT SYSDATETIME(),
        CONSTRAINT FK_UsuarioSlack_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UsuarioSlack_SlackUserId' AND object_id = OBJECT_ID(N'dbo.UsuarioSlack'))
BEGIN
    CREATE UNIQUE INDEX UX_UsuarioSlack_SlackUserId ON dbo.UsuarioSlack(SlackUserId);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Slack_VincularUsuario
    @IdUsuario INT,
    @SlackUserId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SET @SlackUserId = LTRIM(RTRIM(@SlackUserId));
    IF @SlackUserId = N'' THROW 53001, 'Indicá un Slack User ID válido.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE ID = @IdUsuario AND Activo = 1) THROW 53002, 'No existe un usuario activo para asociar con Slack.', 1;
    IF EXISTS (SELECT 1 FROM dbo.UsuarioSlack WHERE SlackUserId = @SlackUserId AND IdUsuario <> @IdUsuario) THROW 53003, 'Ese Slack User ID ya está asociado a otro usuario.', 1;

    IF EXISTS (SELECT 1 FROM dbo.UsuarioSlack WHERE IdUsuario = @IdUsuario)
        UPDATE dbo.UsuarioSlack SET SlackUserId = @SlackUserId WHERE IdUsuario = @IdUsuario;
    ELSE
        INSERT INTO dbo.UsuarioSlack(IdUsuario, SlackUserId) VALUES(@IdUsuario, @SlackUserId);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Slack_ConsultarUsuario
    @SlackUserId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT U.ID, U.IdAgencia, U.Nombre, U.Apellido, U.Email, U.PasswordHash, U.Estado, U.FechaAlta, U.Activo, U.FechaBaja
    FROM dbo.Usuario U
    INNER JOIN dbo.UsuarioSlack US ON US.IdUsuario = U.ID
    WHERE US.SlackUserId = @SlackUserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Slack_ConsultarUsuariosSinVincular
AS
BEGIN
    SET NOCOUNT ON;

    SELECT U.ID, U.IdAgencia, U.Nombre, U.Apellido, U.Email, U.PasswordHash, U.Estado, U.FechaAlta, U.Activo, U.FechaBaja
    FROM dbo.Usuario U
    LEFT JOIN dbo.UsuarioSlack US ON US.IdUsuario = U.ID
    WHERE US.IdUsuario IS NULL
      AND U.IdAgencia IS NOT NULL
      AND U.Activo = 1
      AND U.Estado = N'Activo'
    ORDER BY U.Apellido, U.Nombre, U.Email;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Slack_ConsultarCargaEmpleadoPM
    @IdUsuarioPM INT,
    @IdAgencia INT,
    @NombreEmpleado NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT E.ID AS IdEmpleado, U.Nombre, U.Apellido,
           ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS CapacidadSemanal,
           ISNULL(Carga.CargaActual, 0) AS CargaActual,
           ISNULL(Carga.TareasActivas, 0) AS TareasActivas
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    OUTER APPLY
    (
        SELECT TOP (1) D.HorasSemanales
        FROM dbo.DisponibilidadBase D
        WHERE D.IdEmpleado = E.ID AND D.Activo = 1
        ORDER BY D.ID DESC
    ) DB
    OUTER APPLY
    (
        SELECT SUM(CASE WHEN T.HorasEstimadas - ISNULL(Horas.HorasReales, 0) > 0 THEN T.HorasEstimadas - ISNULL(Horas.HorasReales, 0) ELSE 0 END) AS CargaActual,
               COUNT(*) AS TareasActivas
        FROM dbo.Tarea T
        OUTER APPLY
        (
            SELECT SUM(R.CantidadHoras) AS HorasReales
            FROM dbo.RegistroHora R
            WHERE R.IdTarea = T.ID AND R.Activo = 1
        ) Horas
        WHERE T.IdEmpleadoAsignado = E.ID
          AND T.Activo = 1
          AND ISNULL(T.Estado, N'Pendiente') NOT IN (N'Finalizada', N'Finalizado', N'Completada', N'Completado')
    ) Carga
    WHERE U.IdAgencia = @IdAgencia
      AND U.Activo = 1
      AND E.Activo = 1
      AND CONCAT(U.Nombre, N' ', U.Apellido) LIKE N'%' + LTRIM(RTRIM(@NombreEmpleado)) + N'%'
      AND EXISTS
      (
          SELECT 1
          FROM dbo.Tarea TAlcance
          INNER JOIN dbo.Proyecto P ON P.ID = TAlcance.IdProyecto
          INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable
          WHERE TAlcance.IdEmpleadoAsignado = E.ID
            AND TAlcance.Activo = 1
            AND P.Activo = 1
            AND PM.Activo = 1
            AND PM.IdUsuario = @IdUsuarioPM
      )
    ORDER BY U.Apellido, U.Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Slack_ConsultarCargaEquipoPM
    @IdUsuarioPM INT,
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT E.ID AS IdEmpleado, U.Nombre, U.Apellido,
           ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS CapacidadSemanal,
           ISNULL(Carga.CargaActual, 0) AS CargaActual,
           ISNULL(Carga.TareasActivas, 0) AS TareasActivas
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    OUTER APPLY
    (
        SELECT TOP (1) D.HorasSemanales
        FROM dbo.DisponibilidadBase D
        WHERE D.IdEmpleado = E.ID AND D.Activo = 1
        ORDER BY D.ID DESC
    ) DB
    OUTER APPLY
    (
        SELECT SUM(CASE WHEN T.HorasEstimadas - ISNULL(Horas.HorasReales, 0) > 0 THEN T.HorasEstimadas - ISNULL(Horas.HorasReales, 0) ELSE 0 END) AS CargaActual,
               COUNT(*) AS TareasActivas
        FROM dbo.Tarea T
        OUTER APPLY
        (
            SELECT SUM(R.CantidadHoras) AS HorasReales
            FROM dbo.RegistroHora R
            WHERE R.IdTarea = T.ID AND R.Activo = 1
        ) Horas
        WHERE T.IdEmpleadoAsignado = E.ID
          AND T.Activo = 1
          AND ISNULL(T.Estado, N'Pendiente') NOT IN (N'Finalizada', N'Finalizado', N'Completada', N'Completado')
    ) Carga
    WHERE U.IdAgencia = @IdAgencia
      AND U.Activo = 1
      AND E.Activo = 1
      AND EXISTS
      (
          SELECT 1
          FROM dbo.Tarea TAlcance
          INNER JOIN dbo.Proyecto P ON P.ID = TAlcance.IdProyecto
          INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable
          WHERE TAlcance.IdEmpleadoAsignado = E.ID
            AND TAlcance.Activo = 1
            AND P.Activo = 1
            AND PM.Activo = 1
            AND PM.IdUsuario = @IdUsuarioPM
      )
    ORDER BY U.Apellido, U.Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Slack_ConsultarProyectosAccesibles
    @IdUsuario INT,
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT P.ID, P.Nombre
    FROM dbo.Proyecto P
    WHERE P.IdAgencia = @IdAgencia
      AND P.Activo = 1
      AND
      (
          EXISTS
          (
              SELECT 1
              FROM dbo.PM PM
              WHERE PM.ID = P.IdPMResponsable
                AND PM.Activo = 1
                AND PM.IdUsuario = @IdUsuario
          )
          OR EXISTS
          (
              SELECT 1
              FROM dbo.Tarea T
              INNER JOIN dbo.Empleado E ON E.ID = T.IdEmpleadoAsignado
              WHERE T.IdProyecto = P.ID
                AND T.Activo = 1
                AND E.IdUsuario = @IdUsuario
          )
      )
    ORDER BY P.Nombre;
END;
GO
