USE [TeamBalance]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.RecomendacionSkill', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecomendacionSkill
    (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdSkill INT NOT NULL,
        IdUsuarioCreador INT NOT NULL,
        TipoAccion NVARCHAR(100) NOT NULL,
        Observacion NVARCHAR(1000) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_RecomendacionSkill_FechaCreacion DEFAULT SYSDATETIME(),
        Activo BIT NOT NULL CONSTRAINT DF_RecomendacionSkill_Activo DEFAULT 1,
        CONSTRAINT FK_RecomendacionSkill_Agencia FOREIGN KEY (IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_RecomendacionSkill_Skill FOREIGN KEY (IdSkill) REFERENCES dbo.Skill(ID),
        CONSTRAINT FK_RecomendacionSkill_Usuario FOREIGN KEY (IdUsuarioCreador) REFERENCES dbo.Usuario(ID)
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SkillsDesiertos_ConsultarTareas
    @IdAgencia INT,
    @FechaDesde DATETIME2,
    @FechaHasta DATETIME2,
    @IdProyecto INT = NULL,
    @IdCliente INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT T.ID AS IdTarea, S.ID AS IdSkill, S.Nombre AS NombreSkill, COALESCE(A.Nombre, S.Categoria) AS CategoriaSkill,
           T.Titulo AS TituloTarea, T.HorasEstimadas, T.Deadline, P.ID AS IdProyecto,
           P.Nombre AS NombreProyecto, C.Nombre AS NombreCliente
    FROM dbo.Tarea T
    INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
    INNER JOIN dbo.Cliente C ON C.ID = P.IdCliente
    INNER JOIN dbo.Skill S ON S.ID = T.IdSkillRequerido
    LEFT JOIN dbo.AreaSkill A ON A.ID = S.IdAreaSkill
    WHERE P.IdAgencia = @IdAgencia AND P.Activo = 1 AND T.Activo = 1
      AND T.Estado NOT IN (N'Finalizada', N'Finalizado')
      AND T.Deadline IS NOT NULL AND T.HorasEstimadas > 0
      AND T.Deadline >= @FechaDesde AND T.Deadline < DATEADD(DAY, 1, @FechaHasta)
      AND (@IdProyecto IS NULL OR P.ID = @IdProyecto)
      AND (@IdCliente IS NULL OR P.IdCliente = @IdCliente)
    ORDER BY T.Deadline, T.ID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SkillsDesiertos_SugerirEmpleados
    @IdAgencia INT,
    @IdSkill INT,
    @IdProyecto INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdAreaSkill INT = (SELECT IdAreaSkill FROM dbo.Skill WHERE ID = @IdSkill AND Activo = 1);
    IF @IdAreaSkill IS NULL THROW 51010, 'La skill solicitada no posee un área válida.', 1;

    SELECT E.ID AS IdEmpleado, CONCAT(U.Nombre, N' ', U.Apellido) AS NombreEmpleado, E.Seniority,
           ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS HorasSemanales,
           ISNULL(Carga.HorasEstimadas, 0) AS CargaActual,
           ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) - ISNULL(Carga.HorasEstimadas, 0) AS HorasDisponibles,
           CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.Tarea TP WHERE TP.IdProyecto=@IdProyecto AND TP.IdEmpleadoAsignado=E.ID AND TP.Activo=1 AND TP.Estado NOT IN (N'Finalizada', N'Finalizado')) THEN 1 ELSE 0 END) AS EsDelProyecto,
           CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.EmpleadoSkill ESO WHERE ESO.IdEmpleado=E.ID AND ESO.IdSkill=@IdSkill AND ESO.Activo=1) THEN 1 ELSE 0 END) AS TieneSkillObjetivo,
           ISNULL(Afinidad.CantidadSkillsMismaArea, 0) AS CantidadSkillsMismaArea,
           ISNULL(Afinidad.SkillsRelacionadas, N'') AS SkillsRelacionadas
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario
    LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado=E.ID AND DB.Activo=1
    OUTER APPLY (SELECT SUM(T.HorasEstimadas) AS HorasEstimadas FROM dbo.Tarea T WHERE T.IdEmpleadoAsignado=E.ID AND T.Activo=1 AND T.Estado NOT IN (N'Finalizada', N'Finalizado')) Carga
    OUTER APPLY
    (
        SELECT COUNT(*) AS CantidadSkillsMismaArea, STRING_AGG(SA.Nombre, N', ') AS SkillsRelacionadas
        FROM dbo.EmpleadoSkill ESA INNER JOIN dbo.Skill SA ON SA.ID=ESA.IdSkill
        WHERE ESA.IdEmpleado=E.ID AND ESA.Activo=1 AND SA.IdAreaSkill=@IdAreaSkill
    ) Afinidad
    WHERE U.IdAgencia=@IdAgencia AND U.Activo=1 AND E.Activo=1
      AND ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) > ISNULL(Carga.HorasEstimadas, 0)
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.AusenciaEmpleado AE
          WHERE AE.IdEmpleado=E.ID AND AE.Activo=1 AND AE.Estado IN (N'Aprobada', N'Aprobada parcialmente')
            AND CAST(GETDATE() AS DATE) BETWEEN CAST(COALESCE(AE.FechaInicioAprobada, AE.FechaInicioSolicitada) AS DATE) AND CAST(COALESCE(AE.FechaFinAprobada, AE.FechaFinSolicitada) AS DATE)
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SkillsDesiertos_ConsultarEmpleados
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT E.ID AS IdEmpleado, ES.IdSkill, CONCAT(U.Nombre, N' ', U.Apellido) AS NombreEmpleado,
           E.Seniority, ISNULL(ES.Nivel, N'Intermedio') AS NivelSkill,
           ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS HorasSemanales,
           ISNULL(Carga.HorasEstimadas, 0) AS CargaActual
    FROM dbo.Empleado E
    INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario
    INNER JOIN dbo.EmpleadoSkill ES ON ES.IdEmpleado = E.ID AND ES.Activo = 1
    LEFT JOIN dbo.DisponibilidadBase DB ON DB.IdEmpleado = E.ID AND DB.Activo = 1
    OUTER APPLY
    (
        SELECT SUM(T.HorasEstimadas) AS HorasEstimadas
        FROM dbo.Tarea T
        WHERE T.IdEmpleadoAsignado = E.ID AND T.Activo = 1 AND T.Estado NOT IN (N'Finalizada', N'Finalizado')
    ) Carga
    WHERE U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RecomendacionSkill_Registrar
    @IdAgencia INT,
    @IdSkill INT,
    @IdUsuarioCreador INT,
    @TipoAccion NVARCHAR(100),
    @Observacion NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Skill WHERE ID = @IdSkill AND Activo = 1)
        THROW 51001, 'La skill seleccionada no está activa.', 1;
    INSERT INTO dbo.RecomendacionSkill(IdAgencia, IdSkill, IdUsuarioCreador, TipoAccion, Observacion)
    VALUES(@IdAgencia, @IdSkill, @IdUsuarioCreador, @TipoAccion, @Observacion);
END
GO
