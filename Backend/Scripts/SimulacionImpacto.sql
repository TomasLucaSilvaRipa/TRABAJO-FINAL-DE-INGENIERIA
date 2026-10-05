ALTER TABLE dbo.SimulacionImpacto ALTER COLUMN CargaActual DECIMAL(18,2) NOT NULL;
ALTER TABLE dbo.SimulacionImpacto ALTER COLUMN CargaProyectada DECIMAL(18,2) NOT NULL;
ALTER TABLE dbo.SimulacionImpacto ALTER COLUMN DisponibilidadRestante DECIMAL(18,2) NOT NULL;
ALTER TABLE dbo.SimulacionImpacto ALTER COLUMN PorcentajeOcupacionActual DECIMAL(18,2) NOT NULL;
ALTER TABLE dbo.SimulacionImpacto ALTER COLUMN PorcentajeOcupacionProyectado DECIMAL(18,2) NOT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SimulacionImpacto_Crear
    @IdTarea INT,
    @IdEmpleadoCandidato INT,
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InicioSemana DATE = CAST(GETDATE() AS DATE);
    DECLARE @FinSemana DATE = DATEADD(DAY, 6, @InicioSemana);

    SELECT
        T.HorasEstimadas AS HorasTarea,
        T.Deadline AS DeadlineTarea,
        T.Prioridad AS PrioridadTarea,
        T.Titulo AS NombreTarea,
        P.Nombre AS NombreProyecto,
        ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) AS CapacidadSemanal,
        ISNULL(Carga.CargaActual, 0) AS CargaActual,
        ISNULL(Ausencias.DiasAusenciaProximaSemana, 0) AS DiasAusenciaProximaSemana
    FROM dbo.Tarea T
    INNER JOIN dbo.Proyecto P ON P.ID = T.IdProyecto
    INNER JOIN dbo.PM PM ON PM.ID = P.IdPMResponsable AND PM.Activo = 1
    INNER JOIN dbo.Usuario UPM ON UPM.ID = PM.IdUsuario AND UPM.ID = @IdUsuario AND UPM.Activo = 1
    INNER JOIN dbo.Empleado E ON E.ID = @IdEmpleadoCandidato AND E.Activo = 1
    INNER JOIN dbo.Usuario UE ON UE.ID = E.IdUsuario AND UE.IdAgencia = UPM.IdAgencia AND UE.Activo = 1
    OUTER APPLY
    (
        SELECT TOP 1 Disponibilidad.HorasSemanales
        FROM dbo.DisponibilidadBase Disponibilidad
        WHERE Disponibilidad.IdEmpleado = E.ID AND Disponibilidad.Activo = 1
        ORDER BY Disponibilidad.ID DESC
    ) DB
    OUTER APPLY
    (
        SELECT SUM(CASE WHEN TareaActiva.HorasEstimadas - ISNULL(HorasReales.HorasReales, 0) > 0
            THEN TareaActiva.HorasEstimadas - ISNULL(HorasReales.HorasReales, 0)
            ELSE 0 END) AS CargaActual
        FROM dbo.Tarea TareaActiva
        OUTER APPLY
        (
            SELECT SUM(Registro.CantidadHoras) AS HorasReales
            FROM dbo.RegistroHora Registro
            WHERE Registro.IdTarea = TareaActiva.ID AND Registro.Activo = 1
        ) HorasReales
        WHERE TareaActiva.IdEmpleadoAsignado = E.ID
            AND TareaActiva.Activo = 1
            AND TareaActiva.ID <> @IdTarea
            AND ISNULL(TareaActiva.Estado, N'Pendiente') NOT IN (N'Finalizada', N'Finalizado')
    ) Carga
    OUTER APPLY
    (
        SELECT CASE WHEN SUM(DATEDIFF(DAY,
                CASE WHEN Ausencia.FechaInicioAprobada < @InicioSemana THEN @InicioSemana ELSE CAST(Ausencia.FechaInicioAprobada AS DATE) END,
                CASE WHEN Ausencia.FechaFinAprobada > @FinSemana THEN @FinSemana ELSE CAST(Ausencia.FechaFinAprobada AS DATE) END) + 1) > 5
            THEN 5
            ELSE ISNULL(SUM(DATEDIFF(DAY,
                CASE WHEN Ausencia.FechaInicioAprobada < @InicioSemana THEN @InicioSemana ELSE CAST(Ausencia.FechaInicioAprobada AS DATE) END,
                CASE WHEN Ausencia.FechaFinAprobada > @FinSemana THEN @FinSemana ELSE CAST(Ausencia.FechaFinAprobada AS DATE) END) + 1), 0)
            END AS DiasAusenciaProximaSemana
        FROM dbo.AusenciaEmpleado Ausencia
        WHERE Ausencia.IdEmpleado = E.ID
            AND Ausencia.Activo = 1
            AND Ausencia.Estado IN (N'Aprobada', N'Aprobado')
            AND Ausencia.FechaInicioAprobada <= @FinSemana
            AND Ausencia.FechaFinAprobada >= @InicioSemana
    ) Ausencias
    WHERE T.ID = @IdTarea
        AND T.Activo = 1
        AND T.HorasEstimadas > 0
        AND T.Deadline IS NOT NULL
        AND ISNULL(T.Estado, N'Pendiente') NOT IN (N'Finalizada', N'Finalizado')
        AND P.Activo = 1
        AND P.IdAgencia = UPM.IdAgencia
        AND ISNULL(DB.HorasSemanales, E.HorasDisponiblesSemanales) > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SimulacionImpacto_Registrar
    @IdTarea INT,
    @IdEmpleadoCandidato INT,
    @IdUsuario INT,
    @CargaActual DECIMAL(18,2),
    @CargaProyectada DECIMAL(18,2),
    @DisponibilidadRestante DECIMAL(18,2),
    @PorcentajeOcupacionActual DECIMAL(18,2),
    @PorcentajeOcupacionProyectado DECIMAL(18,2),
    @GeneraSobrecarga BIT,
    @AdvertenciasJson NVARCHAR(MAX),
    @ImpactoOperativo NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SimulacionImpacto
    (
        IdTarea, IdEmpleadoCandidato, IdUsuarioCreador, CargaActual, CargaProyectada,
        DisponibilidadRestante, PorcentajeOcupacionActual, PorcentajeOcupacionProyectado,
        GeneraSobrecarga, AdvertenciasJson, ImpactoOperativo, FechaCreacion, FechaExpiracion, Activo
    )
    OUTPUT inserted.*
    VALUES
    (
        @IdTarea, @IdEmpleadoCandidato, @IdUsuario, @CargaActual, @CargaProyectada,
        @DisponibilidadRestante, @PorcentajeOcupacionActual, @PorcentajeOcupacionProyectado,
        @GeneraSobrecarga, @AdvertenciasJson, @ImpactoOperativo, SYSDATETIME(), DATEADD(DAY, 1, SYSDATETIME()), 1
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SimulacionImpacto_Descartar
    @ID INT,
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SimulacionImpacto
    SET Activo = 0,
        FechaUltimaModificacion = SYSDATETIME()
    WHERE ID = @ID
        AND IdUsuarioCreador = @IdUsuario
        AND Activo = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51000, 'No se encontró una simulación activa para descartar.', 1;
    END;
END;
GO
