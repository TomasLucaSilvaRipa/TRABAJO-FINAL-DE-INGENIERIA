CREATE OR ALTER PROCEDURE dbo.usp_Skill_Consultar AS
BEGIN
    SELECT S.ID, S.Nombre, COALESCE(A.Nombre, S.Categoria) AS Categoria, S.IdAreaSkill, A.Nombre AS NombreArea, S.Activo
    FROM dbo.Skill S LEFT JOIN dbo.AreaSkill A ON A.ID = S.IdAreaSkill
    ORDER BY S.Activo DESC, COALESCE(A.Nombre, S.Categoria), S.Nombre;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Skill_Registrar @Nombre NVARCHAR(150), @IdAreaSkill INT AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.AreaSkill WHERE ID = @IdAreaSkill AND Activo = 1) THROW 51020, 'El área de la skill no es válida.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Skill WHERE Nombre = @Nombre) THROW 51021, 'Ya existe una skill con ese nombre.', 1;
    DECLARE @Categoria NVARCHAR(150) = (SELECT Nombre FROM dbo.AreaSkill WHERE ID = @IdAreaSkill);
    INSERT INTO dbo.Skill (Nombre, Categoria, IdAreaSkill, Activo) VALUES (@Nombre, @Categoria, @IdAreaSkill, 1);
    SELECT S.ID, S.Nombre, S.Categoria, S.IdAreaSkill, A.Nombre AS NombreArea, S.Activo FROM dbo.Skill S INNER JOIN dbo.AreaSkill A ON A.ID=S.IdAreaSkill WHERE S.ID = SCOPE_IDENTITY();
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Skill_CambiarEstado @ID INT, @Activo BIT AS
BEGIN UPDATE dbo.Skill SET Activo=@Activo WHERE ID=@ID; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Skill_Modificar @ID INT, @Nombre NVARCHAR(150), @IdAreaSkill INT AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Skill WHERE ID = @ID) THROW 51022, 'La skill seleccionada no existe.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.AreaSkill WHERE ID = @IdAreaSkill AND Activo = 1) THROW 51020, 'El área de la skill no es válida.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Skill WHERE Nombre = @Nombre AND ID <> @ID) THROW 51021, 'Ya existe una skill con ese nombre.', 1;
    DECLARE @Categoria NVARCHAR(150) = (SELECT Nombre FROM dbo.AreaSkill WHERE ID = @IdAreaSkill);
    UPDATE dbo.Skill SET Nombre = @Nombre, Categoria = @Categoria, IdAreaSkill = @IdAreaSkill WHERE ID = @ID;
    SELECT S.ID, S.Nombre, S.Categoria, S.IdAreaSkill, A.Nombre AS NombreArea, S.Activo FROM dbo.Skill S INNER JOIN dbo.AreaSkill A ON A.ID = S.IdAreaSkill WHERE S.ID = @ID;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Empleado_ConsultarIdPorUsuarioAgencia @IdUsuario INT, @IdAgencia INT AS
BEGIN SELECT E.ID FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE E.IdUsuario=@IdUsuario AND U.IdAgencia=@IdAgencia; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmpleadoSkill_Consultar @IdEmpleado INT AS
BEGIN SELECT ID, IdEmpleado, IdSkill, Nivel, Activo FROM dbo.EmpleadoSkill WHERE IdEmpleado=@IdEmpleado AND Activo=1; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmpleadoSkill_Limpiar @IdEmpleado INT AS
BEGIN UPDATE dbo.EmpleadoSkill SET Activo=0 WHERE IdEmpleado=@IdEmpleado; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmpleadoSkill_Registrar @IdEmpleado INT, @IdSkill INT, @Nivel NVARCHAR(50) = NULL AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.EmpleadoSkill WHERE IdEmpleado=@IdEmpleado AND IdSkill=@IdSkill) UPDATE dbo.EmpleadoSkill SET Nivel=@Nivel, Activo=1 WHERE IdEmpleado=@IdEmpleado AND IdSkill=@IdSkill;
    ELSE INSERT INTO dbo.EmpleadoSkill(IdEmpleado, IdSkill, Nivel, Activo) VALUES(@IdEmpleado, @IdSkill, @Nivel, 1);
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_DisponibilidadBase_Consultar @IdEmpleado INT AS
BEGIN SELECT ID, IdEmpleado, HoraInicio, HoraFin, HorasSemanales, Observacion, Activo FROM dbo.DisponibilidadBase WHERE IdEmpleado=@IdEmpleado AND Activo=1; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_DisponibilidadBase_RegistrarActualizar @IdEmpleado INT, @HoraInicio TIME, @HoraFin TIME, @HorasSemanales DECIMAL(10,2), @Observacion NVARCHAR(MAX) = NULL AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.DisponibilidadBase WHERE IdEmpleado=@IdEmpleado) UPDATE dbo.DisponibilidadBase SET HoraInicio=@HoraInicio, HoraFin=@HoraFin, HorasSemanales=@HorasSemanales, Observacion=@Observacion, Activo=1 WHERE IdEmpleado=@IdEmpleado;
    ELSE INSERT INTO dbo.DisponibilidadBase(IdEmpleado, HoraInicio, HoraFin, HorasSemanales, Observacion, Activo) VALUES(@IdEmpleado, @HoraInicio, @HoraFin, @HorasSemanales, @Observacion, 1);
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Empleado_ConsultarRendimiento
    @IdAgencia INT,
    @IdUsuarioSolicitante INT,
    @IdUsuarioEmpleado INT,
    @Meses INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdEmpleado INT;
    DECLARE @EsDueno BIT = CASE WHEN EXISTS (SELECT 1 FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID = UR.IdRol WHERE UR.IdUsuario = @IdUsuarioSolicitante AND R.Activo = 1 AND R.TipoUsuario = N'Dueno') THEN 1 ELSE 0 END;
    SELECT @IdEmpleado = E.ID FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID = E.IdUsuario WHERE U.ID = @IdUsuarioEmpleado AND U.IdAgencia = @IdAgencia AND U.Activo = 1 AND E.Activo = 1;
    IF @IdEmpleado IS NULL THROW 51030, 'El empleado no existe o no pertenece a la agencia.', 1;
    IF @EsDueno = 0 AND NOT EXISTS
    (
        SELECT 1 FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
        WHERE P.IdAgencia = @IdAgencia AND P.IdPMResponsable = @IdUsuarioSolicitante
          AND (T.IdEmpleadoAsignado = @IdEmpleado OR EXISTS (SELECT 1 FROM dbo.AsignacionTarea A WHERE A.IdTarea = T.ID AND A.IdEmpleado = @IdEmpleado))
    ) THROW 51031, 'No tenés autorización para consultar el rendimiento de este empleado.', 1;

    DECLARE @Inicio DATE = DATEADD(MONTH, 1 - @Meses, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));
    ;WITH Meses AS
    (
        SELECT @Inicio AS Periodo
        UNION ALL SELECT DATEADD(MONTH, 1, Periodo) FROM Meses WHERE DATEADD(MONTH, 1, Periodo) <= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
    ), TareasEmpleado AS
    (
        SELECT DISTINCT T.ID, T.Deadline, T.FechaFinReal, T.HorasEstimadas,
               COALESCE((SELECT MAX(NULLIF(AH.HorasAsignadas, 0)) FROM dbo.AsignacionTarea AH WHERE AH.IdTarea = T.ID AND AH.IdEmpleado = @IdEmpleado), T.HorasEstimadas) AS HorasPlanificadas,
               ISNULL((SELECT SUM(RH.CantidadHoras) FROM dbo.RegistroHora RH WHERE RH.IdTarea = T.ID AND RH.IdEmpleado = @IdEmpleado AND RH.Activo = 1), 0) AS HorasTrabajadas
        FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
        WHERE P.IdAgencia = @IdAgencia AND (T.IdEmpleadoAsignado = @IdEmpleado OR EXISTS (SELECT 1 FROM dbo.AsignacionTarea A WHERE A.IdTarea = T.ID AND A.IdEmpleado = @IdEmpleado))
    ), Reasignaciones AS
    (
        SELECT DATEFROMPARTS(YEAR(A.FechaHasta), MONTH(A.FechaHasta), 1) AS Periodo, COUNT(*) AS Cantidad
        FROM dbo.AsignacionTarea A WHERE A.IdEmpleado = @IdEmpleado AND A.FechaHasta IS NOT NULL AND A.FechaHasta >= @Inicio
        GROUP BY DATEFROMPARTS(YEAR(A.FechaHasta), MONTH(A.FechaHasta), 1)
    )
    SELECT M.Periodo,
           CAST(CASE WHEN COUNT(T.ID) = 0 THEN 0 ELSE 100.0 * SUM(CASE WHEN T.FechaFinReal <= T.Deadline THEN 1 ELSE 0 END) / COUNT(T.ID) END AS DECIMAL(10,2)) AS CumplimientoPlazos,
           CAST(CASE WHEN SUM(ISNULL(T.HorasPlanificadas,0)) = 0 THEN 0 ELSE 100.0 * (SUM(T.HorasTrabajadas) - SUM(T.HorasPlanificadas)) / SUM(T.HorasPlanificadas) END AS DECIMAL(10,2)) AS DesvioHoras,
           COUNT(T.ID) AS TareasFinalizadas, ISNULL(R.Cantidad, 0) AS Reasignaciones, COUNT(T.ID) AS Observaciones,
           CAST(ISNULL(SUM(T.HorasPlanificadas),0) AS DECIMAL(18,2)) AS HorasEstimadas, CAST(ISNULL(SUM(T.HorasTrabajadas),0) AS DECIMAL(18,2)) AS HorasTrabajadas
    FROM Meses M
    LEFT JOIN TareasEmpleado T ON T.FechaFinReal >= M.Periodo AND T.FechaFinReal < DATEADD(MONTH, 1, M.Periodo)
    LEFT JOIN Reasignaciones R ON R.Periodo = M.Periodo
    GROUP BY M.Periodo, R.Cantidad
    HAVING COUNT(T.ID) > 0 OR ISNULL(R.Cantidad, 0) > 0
    ORDER BY M.Periodo OPTION (MAXRECURSION 12);
END
GO
