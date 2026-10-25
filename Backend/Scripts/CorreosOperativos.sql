/* Preferencias, deduplicación y enlaces de un solo uso para correos operativos. */
IF OBJECT_ID(N'dbo.PreferenciaCorreoOperativo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PreferenciaCorreoOperativo
    (
        IdUsuario INT NOT NULL CONSTRAINT PK_PreferenciaCorreoOperativo PRIMARY KEY,
        ResumenDiarioActivo BIT NOT NULL CONSTRAINT DF_PreferenciaCorreo_ResumenDiario DEFAULT (1),
        HoraResumenDiario TIME(0) NOT NULL CONSTRAINT DF_PreferenciaCorreo_HoraDiaria DEFAULT ('09:00'),
        AlertasDeadlineActivo BIT NOT NULL CONSTRAINT DF_PreferenciaCorreo_Deadline DEFAULT (1),
        ResumenSemanalActivo BIT NOT NULL CONSTRAINT DF_PreferenciaCorreo_ResumenSemanal DEFAULT (1),
        FechaModificacion DATETIME2 NOT NULL CONSTRAINT DF_PreferenciaCorreo_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_PreferenciaCorreo_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuario(ID)
    );
END;
GO

IF OBJECT_ID(N'dbo.EnvioCorreoOperativo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnvioCorreoOperativo
    (
        ID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EnvioCorreoOperativo PRIMARY KEY,
        IdUsuario INT NOT NULL,
        IdAgencia INT NULL,
        Tipo NVARCHAR(40) NOT NULL,
        ClaveEnvio NVARCHAR(180) NOT NULL,
        Estado NVARCHAR(20) NOT NULL,
        CantidadIntentos INT NOT NULL CONSTRAINT DF_EnvioCorreo_Intentos DEFAULT (1),
        FechaIntento DATETIME2 NOT NULL CONSTRAINT DF_EnvioCorreo_Intento DEFAULT (SYSDATETIME()),
        FechaEnvio DATETIME2 NULL,
        DetalleError NVARCHAR(1000) NULL,
        CONSTRAINT UQ_EnvioCorreoOperativo_Clave UNIQUE(ClaveEnvio),
        CONSTRAINT FK_EnvioCorreoOperativo_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT FK_EnvioCorreoOperativo_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID)
    );
END;
GO

IF OBJECT_ID(N'dbo.AccionCorreoTarea', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccionCorreoTarea
    (
        ID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AccionCorreoTarea PRIMARY KEY,
        TokenHash CHAR(64) NOT NULL,
        IdUsuario INT NOT NULL,
        IdTarea INT NOT NULL,
        Accion NVARCHAR(40) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_AccionCorreo_Fecha DEFAULT (SYSDATETIME()),
        FechaExpiracion DATETIME2 NOT NULL,
        FechaUso DATETIME2 NULL,
        Activo BIT NOT NULL CONSTRAINT DF_AccionCorreo_Activo DEFAULT (1),
        CONSTRAINT UQ_AccionCorreoTarea_Token UNIQUE(TokenHash),
        CONSTRAINT FK_AccionCorreoTarea_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT FK_AccionCorreoTarea_Tarea FOREIGN KEY(IdTarea) REFERENCES dbo.Tarea(ID)
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PreferenciaCorreoOperativo_Consultar @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EsEmpleado BIT = CASE WHEN EXISTS(SELECT 1 FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID=UR.IdRol WHERE UR.IdUsuario=@IdUsuario AND R.TipoUsuario=N'Empleado') THEN 1 ELSE 0 END;
    DECLARE @EsDueno BIT = CASE WHEN EXISTS(SELECT 1 FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID=UR.IdRol WHERE UR.IdUsuario=@IdUsuario AND R.TipoUsuario=N'Dueno') THEN 1 ELSE 0 END;
    SELECT COALESCE(P.ResumenDiarioActivo,@EsEmpleado) ResumenDiarioActivo, COALESCE(P.HoraResumenDiario,CAST('09:00' AS TIME)) HoraResumenDiario,
           COALESCE(P.AlertasDeadlineActivo,@EsEmpleado) AlertasDeadlineActivo, COALESCE(P.ResumenSemanalActivo,@EsDueno) ResumenSemanalActivo
    FROM (SELECT 1 X) B LEFT JOIN dbo.PreferenciaCorreoOperativo P ON P.IdUsuario=@IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PreferenciaCorreoOperativo_Guardar @IdUsuario INT, @ResumenDiarioActivo BIT, @HoraResumenDiario TIME(0), @AlertasDeadlineActivo BIT, @ResumenSemanalActivo BIT AS
BEGIN
    SET NOCOUNT ON;
    MERGE dbo.PreferenciaCorreoOperativo AS T USING (SELECT @IdUsuario IdUsuario) AS S ON T.IdUsuario=S.IdUsuario
    WHEN MATCHED THEN UPDATE SET ResumenDiarioActivo=@ResumenDiarioActivo,HoraResumenDiario=@HoraResumenDiario,AlertasDeadlineActivo=@AlertasDeadlineActivo,ResumenSemanalActivo=@ResumenSemanalActivo,FechaModificacion=SYSDATETIME()
    WHEN NOT MATCHED THEN INSERT(IdUsuario,ResumenDiarioActivo,HoraResumenDiario,AlertasDeadlineActivo,ResumenSemanalActivo) VALUES(@IdUsuario,@ResumenDiarioActivo,@HoraResumenDiario,@AlertasDeadlineActivo,@ResumenSemanalActivo);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CorreoOperativo_ConsultarDestinatarios AS
BEGIN
    SET NOCOUNT ON;
    ;WITH Tipos AS
    (
        SELECT UR.IdUsuario,R.TipoUsuario,ROW_NUMBER() OVER(PARTITION BY UR.IdUsuario ORDER BY CASE R.TipoUsuario WHEN N'Dueno' THEN 1 WHEN N'Empleado' THEN 2 ELSE 3 END,R.ID) Posicion
        FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID=UR.IdRol AND R.Activo=1
    )
    SELECT U.ID IdUsuario,U.IdAgencia,U.Nombre,U.Email,T.TipoUsuario,
           COALESCE(P.ResumenDiarioActivo,CASE WHEN T.TipoUsuario=N'Empleado' THEN 1 ELSE 0 END) ResumenDiarioActivo,
           COALESCE(P.HoraResumenDiario,CAST('09:00' AS TIME)) HoraResumenDiario,
           COALESCE(P.AlertasDeadlineActivo,CASE WHEN T.TipoUsuario=N'Empleado' THEN 1 ELSE 0 END) AlertasDeadlineActivo,
           COALESCE(P.ResumenSemanalActivo,CASE WHEN T.TipoUsuario=N'Dueno' THEN 1 ELSE 0 END) ResumenSemanalActivo
    FROM dbo.Usuario U INNER JOIN Tipos T ON T.IdUsuario=U.ID AND T.Posicion=1
    LEFT JOIN dbo.PreferenciaCorreoOperativo P ON P.IdUsuario=U.ID
    WHERE U.Activo=1 AND U.Estado NOT IN (N'PendienteValidacion',N'Bloqueado') AND NULLIF(LTRIM(RTRIM(U.Email)),N'') IS NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CorreoOperativo_ConsultarTareasEmpleado @IdUsuario INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT T.ID IdTarea,T.Titulo,P.Nombre Proyecto,COALESCE(NULLIF(T.Estado,N''),N'Pendiente') Estado,T.Prioridad,T.Deadline,T.PorcentajeAvance
    FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto AND P.Activo=1
    INNER JOIN dbo.Empleado E ON E.IdUsuario=@IdUsuario AND E.Activo=1
    WHERE T.Activo=1 AND COALESCE(T.Estado,N'Pendiente') NOT IN (N'Finalizada',N'Finalizado',N'Completada',N'Completado')
      AND (EXISTS(SELECT 1 FROM dbo.AsignacionTarea A WHERE A.IdTarea=T.ID AND A.IdEmpleado=E.ID AND A.Activa=1)
           OR (NOT EXISTS(SELECT 1 FROM dbo.AsignacionTarea A WHERE A.IdTarea=T.ID AND A.Activa=1) AND T.IdEmpleadoAsignado=E.ID))
    ORDER BY CASE WHEN T.Deadline IS NULL THEN 1 ELSE 0 END,T.Deadline,T.ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CorreoOperativo_ReservarEnvio @ClaveEnvio NVARCHAR(180),@IdUsuario INT,@IdAgencia INT=NULL,@Tipo NVARCHAR(40) AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRAN;
    DECLARE @Reservado BIT=0;
    IF NOT EXISTS(SELECT 1 FROM dbo.EnvioCorreoOperativo WITH(UPDLOCK,HOLDLOCK) WHERE ClaveEnvio=@ClaveEnvio)
    BEGIN INSERT dbo.EnvioCorreoOperativo(IdUsuario,IdAgencia,Tipo,ClaveEnvio,Estado) VALUES(@IdUsuario,@IdAgencia,@Tipo,@ClaveEnvio,N'Procesando'); SET @Reservado=1; END
    ELSE IF EXISTS(SELECT 1 FROM dbo.EnvioCorreoOperativo WITH(UPDLOCK,HOLDLOCK) WHERE ClaveEnvio=@ClaveEnvio AND Estado=N'Error' AND CantidadIntentos<3 AND FechaIntento<DATEADD(MINUTE,-10,SYSDATETIME()))
    BEGIN UPDATE dbo.EnvioCorreoOperativo SET Estado=N'Procesando',CantidadIntentos=CantidadIntentos+1,FechaIntento=SYSDATETIME(),DetalleError=NULL WHERE ClaveEnvio=@ClaveEnvio; SET @Reservado=1; END
    COMMIT; SELECT @Reservado Reservado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CorreoOperativo_CompletarEnvio @ClaveEnvio NVARCHAR(180),@Enviado BIT,@DetalleError NVARCHAR(1000)=NULL AS
BEGIN
    SET NOCOUNT ON; UPDATE dbo.EnvioCorreoOperativo SET Estado=CASE WHEN @Enviado=1 THEN N'Enviado' ELSE N'Error' END,FechaEnvio=CASE WHEN @Enviado=1 THEN SYSDATETIME() ELSE NULL END,DetalleError=@DetalleError WHERE ClaveEnvio=@ClaveEnvio;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AccionCorreoTarea_Crear @TokenHash CHAR(64),@IdUsuario INT,@IdTarea INT,@Accion NVARCHAR(40),@FechaExpiracion DATETIME2 AS
BEGIN SET NOCOUNT ON; INSERT dbo.AccionCorreoTarea(TokenHash,IdUsuario,IdTarea,Accion,FechaExpiracion) VALUES(@TokenHash,@IdUsuario,@IdTarea,@Accion,@FechaExpiracion); END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AccionCorreoTarea_Consultar @TokenHash CHAR(64) AS
BEGIN
    SET NOCOUNT ON; SELECT A.IdUsuario,A.IdTarea,A.Accion,T.Titulo TituloTarea,P.Nombre Proyecto,A.FechaExpiracion,
    CAST(CASE WHEN A.Activo=1 AND A.FechaUso IS NULL AND A.FechaExpiracion>=SYSDATETIME() AND T.Activo=1 THEN 1 ELSE 0 END AS BIT) Disponible
    FROM dbo.AccionCorreoTarea A INNER JOIN dbo.Tarea T ON T.ID=A.IdTarea INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE A.TokenHash=@TokenHash;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AccionCorreoTarea_MarcarUsada @TokenHash CHAR(64) AS
BEGIN
    SET NOCOUNT ON; UPDATE dbo.AccionCorreoTarea SET FechaUso=SYSDATETIME(),Activo=0 WHERE TokenHash=@TokenHash AND Activo=1 AND FechaUso IS NULL AND FechaExpiracion>=SYSDATETIME(); SELECT CAST(CASE WHEN @@ROWCOUNT=1 THEN 1 ELSE 0 END AS BIT) Actualizada;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CorreoOperativo_ResumenSemanal @IdAgencia INT AS
BEGIN
    SET NOCOUNT ON;
    ;WITH Carga AS
    (
        SELECT E.ID,MAX(COALESCE(DB.HorasSemanales,E.HorasDisponiblesSemanales,0)) Capacidad,COALESCE(SUM(CASE WHEN T.Activo=1 AND COALESCE(T.Estado,N'') NOT IN(N'Finalizada',N'Finalizado',N'Completada',N'Completado') THEN A.HorasAsignadas ELSE 0 END),0) Carga
        FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario AND U.IdAgencia=@IdAgencia AND U.Activo=1
        LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado=E.ID AND DB.Activo=1 LEFT JOIN dbo.AsignacionTarea A ON A.IdEmpleado=E.ID AND A.Activa=1 LEFT JOIN dbo.Tarea T ON T.ID=A.IdTarea
        WHERE E.Activo=1 GROUP BY E.ID
    )
    SELECT (SELECT COUNT(*) FROM dbo.Proyecto WHERE IdAgencia=@IdAgencia AND Activo=1) ProyectosActivos,
           (SELECT COUNT(*) FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE P.IdAgencia=@IdAgencia AND P.Activo=1 AND T.Activo=1 AND COALESCE(T.Estado,N'') NOT IN(N'Finalizada',N'Finalizado',N'Completada',N'Completado')) TareasActivas,
           (SELECT COUNT(*) FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE P.IdAgencia=@IdAgencia AND P.Activo=1 AND T.Activo=1 AND T.Deadline<CAST(GETDATE() AS DATE) AND COALESCE(T.Estado,N'') NOT IN(N'Finalizada',N'Finalizado',N'Completada',N'Completado')) TareasVencidas,
           (SELECT COUNT(*) FROM Carga WHERE Capacidad>0 AND Carga>Capacidad) EmpleadosSobrecargados,
           CAST(COALESCE((SELECT AVG(CASE WHEN Capacidad>0 THEN Carga*100.0/Capacidad ELSE 0 END) FROM Carga),0) AS DECIMAL(10,2)) OcupacionPromedio;
END;
GO
