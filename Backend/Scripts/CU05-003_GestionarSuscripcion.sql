/*
  CU05-003 - Gestionar suscripción de agencia
  Ejecutar sobre TeamBalance después de ConfiguracionAgencia.sql.
  No persiste tarjetas, tokens ni otros datos sensibles de pago.
*/
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.OperacionSuscripcion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperacionSuscripcion
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperacionSuscripcion PRIMARY KEY,
        IdSuscripcion INT NOT NULL,
        IdPlanAnterior INT NULL,
        IdPlanNuevo INT NULL,
        TipoOperacion NVARCHAR(80) NOT NULL,
        ReferenciaInterna NVARCHAR(120) NULL,
        ReferenciaProveedor NVARCHAR(250) NULL,
        Proveedor NVARCHAR(100) NULL,
        Importe DECIMAL(18,2) NULL,
        Moneda NVARCHAR(20) NULL,
        Estado NVARCHAR(80) NOT NULL,
        Detalle NVARCHAR(1000) NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_OperacionSuscripcion_FechaCreacion DEFAULT SYSDATETIME(),
        FechaActualizacion DATETIME2 NULL,
        CONSTRAINT FK_OperacionSuscripcion_Suscripcion FOREIGN KEY (IdSuscripcion) REFERENCES dbo.Suscripcion(ID),
        CONSTRAINT FK_OperacionSuscripcion_PlanAnterior FOREIGN KEY (IdPlanAnterior) REFERENCES dbo.PlanComercial(ID),
        CONSTRAINT FK_OperacionSuscripcion_PlanNuevo FOREIGN KEY (IdPlanNuevo) REFERENCES dbo.PlanComercial(ID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OperacionSuscripcion') AND name = N'UX_OperacionSuscripcion_ReferenciaInterna')
    CREATE UNIQUE INDEX UX_OperacionSuscripcion_ReferenciaInterna ON dbo.OperacionSuscripcion(ReferenciaInterna) WHERE ReferenciaInterna IS NOT NULL;
GO

IF COL_LENGTH(N'dbo.Suscripcion', N'ReferenciaRenovacionProveedor') IS NULL
    ALTER TABLE dbo.Suscripcion ADD ReferenciaRenovacionProveedor NVARCHAR(250) NULL, EstadoRenovacionProveedor NVARCHAR(80) NULL, FechaSincronizacionRenovacion DATETIME2 NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarActualPorAgencia
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        S.ID, S.IdAgencia, S.IdPlanComercial, S.ReferenciaExterna, S.Estado, S.FechaAlta, S.FechaVencimiento,
        S.FechaProximaRenovacion, S.RenovacionAutomatica, S.ImporteVigente, S.Activo, S.FechaBaja,
        S.ReferenciaRenovacionProveedor, S.EstadoRenovacionProveedor, S.FechaSincronizacionRenovacion,
        P.Nombre AS NombrePlan, P.Periodicidad AS PeriodicidadPlan, P.Moneda
    FROM dbo.Suscripcion S
    INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial
    WHERE S.IdAgencia = @IdAgencia
    ORDER BY S.Activo DESC, S.FechaAlta DESC, S.ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_CrearSolicitudCambioPlan
    @IdSuscripcion INT,
    @IdPlanNuevo INT,
    @ReferenciaInterna NVARCHAR(120),
    @Proveedor NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdPlanAnterior INT, @Activo BIT, @Estado NVARCHAR(100), @Importe DECIMAL(18,2), @Moneda NVARCHAR(20);
    SELECT @IdPlanAnterior = IdPlanComercial, @Activo = Activo, @Estado = Estado
    FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;

    IF @IdPlanAnterior IS NULL OR @Activo = 0 OR @Estado <> N'Activa'
        THROW 53001, 'La suscripción no está activa para solicitar un cambio de modalidad.', 1;
    IF @IdPlanAnterior = @IdPlanNuevo
        THROW 53002, 'La modalidad seleccionada ya es la contratada.', 1;

    SELECT @Importe = PrecioVigente, @Moneda = Moneda
    FROM dbo.PlanComercial
    WHERE ID = @IdPlanNuevo AND Activo = 1 AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= SYSDATETIME());

    IF @Importe IS NULL
        THROW 53003, 'La modalidad seleccionada no se encuentra disponible.', 1;

    INSERT INTO dbo.OperacionSuscripcion
        (IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, ReferenciaInterna, Proveedor, Importe, Moneda, Estado, Detalle)
    VALUES
        (@IdSuscripcion, @IdPlanAnterior, @IdPlanNuevo, N'Actualización de modalidad', @ReferenciaInterna, @Proveedor, @Importe, @Moneda, N'Pendiente', N'Solicitud enviada al proveedor externo.');

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS IdOperacion, @IdSuscripcion AS IdSuscripcion, @IdPlanNuevo AS IdPlanNuevo,
           @ReferenciaInterna AS ReferenciaInterna, @Importe AS Importe, @Moneda AS Moneda, @Proveedor AS Proveedor;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarOperacionPendiente
    @ReferenciaInterna NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ID AS IdOperacion, IdSuscripcion, IdPlanNuevo, ReferenciaInterna, Importe, Moneda, Proveedor
    FROM dbo.OperacionSuscripcion
    WHERE ReferenciaInterna = @ReferenciaInterna AND Estado = N'Pendiente';
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_AplicarResultadoCambioPlan
    @ReferenciaInterna NVARCHAR(120),
    @ReferenciaProveedor NVARCHAR(250),
    @EstadoProveedor NVARCHAR(80),
    @Detalle NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdOperacion INT, @IdSuscripcion INT, @IdPlanNuevo INT, @EstadoOperacion NVARCHAR(80);
    SELECT @IdOperacion = ID, @IdSuscripcion = IdSuscripcion, @IdPlanNuevo = IdPlanNuevo, @EstadoOperacion = Estado
    FROM dbo.OperacionSuscripcion WHERE ReferenciaInterna = @ReferenciaInterna;

    IF @IdOperacion IS NULL
        THROW 53004, 'No existe la solicitud de actualización indicada.', 1;

    IF @EstadoOperacion <> N'Pendiente'
        THROW 53005, 'La solicitud de actualización ya fue procesada.', 1;

    DECLARE @EstadoResultado NVARCHAR(80) = CASE
        WHEN @EstadoProveedor = N'approved' THEN N'Aprobada'
        WHEN @EstadoProveedor IN (N'rejected', N'cancelled') THEN N'Rechazada'
        ELSE N'Pendiente'
    END;

    DECLARE @DetalleFinal NVARCHAR(1000) = @Detalle;
    IF @EstadoResultado = N'Aprobada'
    BEGIN
        DECLARE @Importe DECIMAL(18,2), @ReferenciaExterna NVARCHAR(500), @DuracionMeses INT,
                @VencimientoActual DATETIME2, @FechaBase DATETIME2, @NuevoVencimiento DATETIME2;
        SELECT @Importe = PrecioVigente, @DuracionMeses = DuracionMeses FROM dbo.PlanComercial WHERE ID = @IdPlanNuevo;
        SELECT @VencimientoActual = FechaVencimiento FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
        SET @FechaBase = CASE WHEN @VencimientoActual > SYSDATETIME() THEN @VencimientoActual ELSE SYSDATETIME() END;
        SET @NuevoVencimiento = DATEADD(MONTH, @DuracionMeses, @FechaBase);
        SET @ReferenciaExterna = @ReferenciaProveedor;

        UPDATE dbo.Suscripcion
        SET IdPlanComercial = @IdPlanNuevo,
            ImporteVigente = @Importe,
            ReferenciaExterna = @ReferenciaExterna,
            Estado = N'Activa',
            Activo = 1,
            FechaVencimiento = @NuevoVencimiento,
            FechaProximaRenovacion = @NuevoVencimiento
        WHERE ID = @IdSuscripcion;

        SET @DetalleFinal = LEFT(CONCAT(@Detalle, N' Vigencia extendida hasta ', CONVERT(NVARCHAR(10), @NuevoVencimiento, 103), N'.'), 1000);
    END;

    UPDATE dbo.OperacionSuscripcion
    SET ReferenciaProveedor = @ReferenciaProveedor,
        Estado = @EstadoResultado,
        Detalle = @DetalleFinal,
        FechaActualizacion = SYSDATETIME()
    WHERE ID = @IdOperacion;

    DECLARE @IdAgencia INT;
    SELECT @IdAgencia = IdAgencia FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    EXEC dbo.usp_Suscripcion_ConsultarActualPorAgencia @IdAgencia;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ActualizarRenovacion
    @IdSuscripcion INT,
    @Activa BIT,
    @Motivo NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdPlan INT, @Estado NVARCHAR(100), @FechaVencimiento DATETIME2;
    SELECT @IdPlan = IdPlanComercial, @Estado = Estado, @FechaVencimiento = FechaVencimiento
    FROM dbo.Suscripcion WHERE ID = @IdSuscripcion AND Activo = 1;

    IF @IdPlan IS NULL OR @Estado <> N'Activa'
        THROW 53006, 'La suscripción no está activa para modificar su renovación.', 1;
    IF @FechaVencimiento <= SYSDATETIME()
        THROW 53007, 'El período vigente ya finalizó.', 1;

    UPDATE dbo.Suscripcion SET RenovacionAutomatica = @Activa WHERE ID = @IdSuscripcion;

    INSERT INTO dbo.OperacionSuscripcion
        (IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, Importe, Moneda, Estado, Detalle)
    SELECT @IdSuscripcion, @IdPlan, @IdPlan,
        CASE WHEN @Activa = 1 THEN N'Reactivación de renovación automática' ELSE N'Cancelación de renovación automática' END,
        S.ImporteVigente, P.Moneda, N'Aplicada',
        CASE WHEN @Activa = 1 THEN N'El propietario reactivó la renovación automática.' ELSE COALESCE(NULLIF(@Motivo, N''), N'El propietario canceló la renovación automática.') END
    FROM dbo.Suscripcion S INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial
    WHERE S.ID = @IdSuscripcion;

    DECLARE @IdAgencia INT;
    SELECT @IdAgencia = IdAgencia FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    EXEC dbo.usp_Suscripcion_ConsultarActualPorAgencia @IdAgencia;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarHistorialPorAgencia
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM
    (
        SELECT OS.ID, OS.TipoOperacion, P.Nombre AS Modalidad, OS.Importe, OS.Moneda, OS.Proveedor,
               COALESCE(OS.ReferenciaProveedor, OS.ReferenciaInterna) AS Referencia, OS.Estado, OS.Detalle, OS.FechaCreacion AS Fecha
        FROM dbo.OperacionSuscripcion OS
        INNER JOIN dbo.Suscripcion S ON S.ID = OS.IdSuscripcion
        LEFT JOIN dbo.PlanComercial P ON P.ID = OS.IdPlanNuevo
        WHERE S.IdAgencia = @IdAgencia

        UNION ALL

        SELECT -OP.ID AS ID, N'Pago de contratación' AS TipoOperacion, P.Nombre AS Modalidad, OP.Importe, OP.Moneda,
               OP.Proveedor, OP.ReferenciaProveedor AS Referencia, OP.Estado, NULL AS Detalle, OP.FechaCreacion AS Fecha
        FROM dbo.OperacionPago OP
        INNER JOIN dbo.ContratacionServicio C ON C.ID = OP.IdContratacionServicio
        LEFT JOIN dbo.PlanComercial P ON P.ID = C.IdPlanComercial
        WHERE C.IdAgencia = @IdAgencia
    ) AS Historial
    ORDER BY Fecha DESC, ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_RegistrarRenovacionProveedor
    @IdSuscripcion INT, @ReferenciaProveedor NVARCHAR(250), @EstadoProveedor NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Suscripcion
    SET RenovacionAutomatica = 1, ReferenciaRenovacionProveedor = @ReferenciaProveedor,
        EstadoRenovacionProveedor = @EstadoProveedor, FechaSincronizacionRenovacion = SYSDATETIME()
    WHERE ID = @IdSuscripcion AND Activo = 1;
    IF @@ROWCOUNT <> 1 THROW 53030, 'No existe una suscripción activa para registrar la autorización recurrente.', 1;
    SELECT @IdSuscripcion AS IdSuscripcion, @ReferenciaProveedor AS ReferenciaProveedor, @EstadoProveedor AS EstadoProveedor;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarRenovacionesProveedor
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID AS IdSuscripcion, IdAgencia, IdPlanComercial, ReferenciaRenovacionProveedor, EstadoRenovacionProveedor
    FROM dbo.Suscripcion
    WHERE RenovacionAutomatica = 1 AND ReferenciaRenovacionProveedor IS NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ActualizarEstadoRenovacionProveedor
    @IdSuscripcion INT, @EstadoProveedor NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Suscripcion
    SET EstadoRenovacionProveedor = @EstadoProveedor, FechaSincronizacionRenovacion = SYSDATETIME()
    WHERE ID = @IdSuscripcion AND Activo = 1 AND ReferenciaRenovacionProveedor IS NOT NULL;
    IF @@ROWCOUNT <> 1 THROW 53032, 'No existe una autorización recurrente activa para actualizar.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_AplicarCobroRecurrente
    @IdSuscripcion INT, @ReferenciaPago NVARCHAR(250), @Importe DECIMAL(18,2), @Moneda NVARCHAR(20), @Detalle NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM dbo.OperacionSuscripcion WHERE IdSuscripcion = @IdSuscripcion AND TipoOperacion = N'Renovación automática' AND ReferenciaProveedor = @ReferenciaPago)
        RETURN;

    DECLARE @IdPlan INT, @Duracion INT, @Vencimiento DATETIME2, @Base DATETIME2, @Nuevo DATETIME2;
    SELECT @IdPlan = IdPlanComercial, @Vencimiento = FechaVencimiento FROM dbo.Suscripcion WHERE ID = @IdSuscripcion AND RenovacionAutomatica = 1;
    IF @IdPlan IS NULL THROW 53031, 'La suscripción no está habilitada para renovar.', 1;
    SELECT @Duracion = DuracionMeses FROM dbo.PlanComercial WHERE ID = @IdPlan;
    SET @Base = CASE WHEN @Vencimiento > SYSDATETIME() THEN @Vencimiento ELSE SYSDATETIME() END;
    SET @Nuevo = DATEADD(MONTH, @Duracion, @Base);
    UPDATE dbo.Suscripcion
    SET FechaVencimiento = @Nuevo, FechaProximaRenovacion = @Nuevo, Estado = N'Activa', Activo = 1, FechaBaja = NULL,
        FechaSincronizacionRenovacion = SYSDATETIME()
    WHERE ID = @IdSuscripcion;
    INSERT INTO dbo.OperacionSuscripcion(IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, ReferenciaProveedor, Proveedor, Importe, Moneda, Estado, Detalle)
    VALUES(@IdSuscripcion, @IdPlan, @IdPlan, N'Renovación automática', @ReferenciaPago, N'MercadoPago', @Importe, @Moneda, N'Aprobada', @Detalle);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_PuedeUsarAgencia
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.Suscripcion
        WHERE IdAgencia = @IdAgencia AND Activo = 1 AND Estado = N'Activa' AND FechaVencimiento >= SYSDATETIME()
    ) THEN 1 ELSE 0 END) AS Vigente;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_SincronizarVencimientos
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Actualizadas TABLE (IdSuscripcion INT, IdAgencia INT, Estado NVARCHAR(100));

    UPDATE dbo.Suscripcion
    SET Estado = N'Vencida', Activo = 0, FechaBaja = COALESCE(FechaBaja, SYSDATETIME()), FechaProximaRenovacion = NULL
    OUTPUT inserted.ID, inserted.IdAgencia, inserted.Estado INTO @Actualizadas(IdSuscripcion, IdAgencia, Estado)
    WHERE Activo = 1 AND FechaVencimiento < SYSDATETIME();

    INSERT INTO dbo.OperacionSuscripcion
        (IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, Importe, Moneda, Estado, Detalle)
    SELECT A.IdSuscripcion, S.IdPlanComercial, S.IdPlanComercial, N'Vencimiento', S.ImporteVigente, P.Moneda, N'Vencida',
           CASE WHEN S.RenovacionAutomatica = 1
                THEN N'La renovación automática no pudo cobrarse: no existe una autorización recurrente del proveedor.'
                ELSE N'El período contratado finalizó sin renovación automática.' END
    FROM @Actualizadas A
    INNER JOIN dbo.Suscripcion S ON S.ID = A.IdSuscripcion
    INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial;

    SELECT IdSuscripcion, IdAgencia, Estado FROM @Actualizadas;
END;
GO
