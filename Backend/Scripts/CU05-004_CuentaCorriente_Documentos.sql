/*
  CU05-004 - Cuenta corriente y documentos comerciales internos.
  No integra facturación fiscal ni ejecuta reintegros en Mercado Pago.
  Registra los comprobantes funcionales de TeamBalance y el crédito utilizable
  por la agencia luego de una cancelación.
*/
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.DocumentoComercial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentoComercial
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentoComercial PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdSuscripcion INT NULL,
        IdDocumentoOrigen INT NULL,
        Tipo NVARCHAR(30) NOT NULL,
        Numero NVARCHAR(30) NOT NULL CONSTRAINT UQ_DocumentoComercial_Numero UNIQUE,
        Concepto NVARCHAR(300) NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        SaldoPendiente DECIMAL(18,2) NOT NULL,
        Moneda NVARCHAR(10) NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        FechaEmision DATETIME2 NOT NULL CONSTRAINT DF_DocumentoComercial_FechaEmision DEFAULT SYSDATETIME(),
        FechaVencimiento DATETIME2 NULL,
        Motivo NVARCHAR(1000) NULL,
        CONSTRAINT FK_DocumentoComercial_Agencia FOREIGN KEY (IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_DocumentoComercial_Suscripcion FOREIGN KEY (IdSuscripcion) REFERENCES dbo.Suscripcion(ID),
        CONSTRAINT FK_DocumentoComercial_Origen FOREIGN KEY (IdDocumentoOrigen) REFERENCES dbo.DocumentoComercial(ID)
    );
END;
GO

IF OBJECT_ID(N'dbo.MovimientoCuentaCorriente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientoCuentaCorriente
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientoCuentaCorriente PRIMARY KEY,
        IdAgencia INT NOT NULL,
        IdDocumentoComercial INT NULL,
        Tipo NVARCHAR(30) NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Moneda NVARCHAR(10) NOT NULL,
        Concepto NVARCHAR(300) NOT NULL,
        Fecha DATETIME2 NOT NULL CONSTRAINT DF_MovimientoCuentaCorriente_Fecha DEFAULT SYSDATETIME(),
        CONSTRAINT FK_MovimientoCuentaCorriente_Agencia FOREIGN KEY (IdAgencia) REFERENCES dbo.Agencia(ID),
        CONSTRAINT FK_MovimientoCuentaCorriente_Documento FOREIGN KEY (IdDocumentoComercial) REFERENCES dbo.DocumentoComercial(ID)
    );
END;
GO

IF COL_LENGTH(N'dbo.OperacionSuscripcion', N'CreditoAplicado') IS NULL
    ALTER TABLE dbo.OperacionSuscripcion ADD CreditoAplicado DECIMAL(18,2) NULL;
GO

/* Corrige textos ya guardados con UTF-8 interpretado por una página de códigos distinta. */
UPDATE dbo.DocumentoComercial
SET Concepto = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Concepto, NCHAR(195) + NCHAR(179), NCHAR(243)), NCHAR(195) + NCHAR(169), NCHAR(233)), NCHAR(195) + NCHAR(161), NCHAR(225)), NCHAR(195) + NCHAR(173), NCHAR(237)), NCHAR(194) + NCHAR(183), NCHAR(183))
WHERE CHARINDEX(NCHAR(195), Concepto) > 0 OR CHARINDEX(NCHAR(194), Concepto) > 0;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CuentaCorriente_Consultar
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    /* Backfill seguro para agencias creadas antes de este módulo. */
    INSERT INTO dbo.DocumentoComercial(IdAgencia, IdSuscripcion, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision)
    SELECT S.IdAgencia, S.ID, N'Factura', CONCAT(N'FC-S', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), S.ID)), 8)),
           CONCAT(N'Suscripcion TeamBalance - ', P.Nombre), S.ImporteVigente, 0, P.Moneda, N'Pagada', S.FechaAlta
    FROM dbo.Suscripcion S
    INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial
    WHERE S.IdAgencia = @IdAgencia
      AND NOT EXISTS (SELECT 1 FROM dbo.DocumentoComercial D WHERE D.IdSuscripcion = S.ID AND D.Tipo = N'Factura');

    INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto, Fecha)
    SELECT D.IdAgencia, D.ID, N'Cobro con tarjeta', D.Importe, D.Moneda, CONCAT(N'Pago aplicado a ', D.Numero), D.FechaEmision
    FROM dbo.DocumentoComercial D
    WHERE D.IdAgencia = @IdAgencia AND D.Tipo = N'Factura'
      AND NOT EXISTS (SELECT 1 FROM dbo.MovimientoCuentaCorriente M WHERE M.IdDocumentoComercial = D.ID AND M.Tipo = N'Cobro con tarjeta');

    COMMIT TRANSACTION;

    SELECT ID, IdAgencia, IdSuscripcion, IdDocumentoOrigen, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision, FechaVencimiento, Motivo
    FROM dbo.DocumentoComercial
    WHERE IdAgencia = @IdAgencia
    ORDER BY FechaEmision DESC, ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CuentaCorriente_CancelarSuscripcion
    @IdAgencia INT,
    @IdSuscripcion INT,
    @Motivo NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Importe DECIMAL(18,2);
    DECLARE @Moneda NVARCHAR(10);
    DECLARE @IdFactura INT;
    DECLARE @IdNotaCredito INT;

    BEGIN TRANSACTION;

    SELECT @Importe = S.ImporteVigente, @Moneda = P.Moneda
    FROM dbo.Suscripcion S WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial
    WHERE S.ID = @IdSuscripcion AND S.IdAgencia = @IdAgencia AND S.Activo = 1;

    IF @Importe IS NULL
        THROW 53101, 'La suscripción no está activa o no pertenece a esta agencia.', 1;

    SELECT TOP (1) @IdFactura = ID
    FROM dbo.DocumentoComercial
    WHERE IdSuscripcion = @IdSuscripcion AND Tipo = N'Factura'
    ORDER BY ID DESC;

    UPDATE dbo.Suscripcion
    SET Activo = 0,
        Estado = N'Cancelada',
        FechaBaja = SYSDATETIME(),
        RenovacionAutomatica = 0,
        FechaProximaRenovacion = NULL
    WHERE ID = @IdSuscripcion;

    INSERT INTO dbo.DocumentoComercial(IdAgencia, IdSuscripcion, IdDocumentoOrigen, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision, Motivo)
    VALUES(@IdAgencia, @IdSuscripcion, @IdFactura, N'NotaCredito', N'PENDIENTE', N'Credito por cancelacion del servicio TeamBalance', @Importe, @Importe, @Moneda, N'Disponible', SYSDATETIME(), @Motivo);

    SET @IdNotaCredito = CONVERT(INT, SCOPE_IDENTITY());

    UPDATE dbo.DocumentoComercial
    SET Numero = CONCAT(N'NC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdNotaCredito)), 8))
    WHERE ID = @IdNotaCredito;

    INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto)
    VALUES(@IdAgencia, @IdNotaCredito, N'Nota de credito', @Importe, @Moneda, N'Credito disponible por cancelacion de suscripcion');

    COMMIT TRANSACTION;

    SELECT ID, IdAgencia, IdSuscripcion, IdDocumentoOrigen, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision, FechaVencimiento, Motivo
    FROM dbo.DocumentoComercial
    WHERE ID = @IdNotaCredito;
END;
GO

/* Cada actualización aprobada conserva además su factura y movimiento de cobro. */
CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_AplicarResultadoCambioPlan
    @ReferenciaInterna NVARCHAR(120),
    @ReferenciaProveedor NVARCHAR(250),
    @EstadoProveedor NVARCHAR(80),
    @Detalle NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdOperacion INT, @IdSuscripcion INT, @IdPlanNuevo INT, @EstadoOperacion NVARCHAR(80), @TipoOperacion NVARCHAR(120), @ImporteOperacion DECIMAL(18,2), @MonedaOperacion NVARCHAR(20), @ProveedorOperacion NVARCHAR(100), @CreditoOperacion DECIMAL(18,2);
    DECLARE @IdAgencia INT, @IdDocumento INT;
    SELECT @IdOperacion = ID, @IdSuscripcion = IdSuscripcion, @IdPlanNuevo = IdPlanNuevo, @EstadoOperacion = Estado, @TipoOperacion = TipoOperacion, @ImporteOperacion = Importe, @MonedaOperacion = Moneda, @ProveedorOperacion = Proveedor, @CreditoOperacion = CreditoAplicado
    FROM dbo.OperacionSuscripcion
    WHERE ReferenciaInterna = @ReferenciaInterna;

    IF @IdOperacion IS NULL
        THROW 53004, 'No existe la solicitud de actualización indicada.', 1;
    IF @EstadoOperacion <> N'Pendiente'
        THROW 53005, 'La solicitud de actualización ya fue procesada.', 1;

    DECLARE @EstadoResultado NVARCHAR(80) = CASE WHEN @EstadoProveedor = N'approved' THEN N'Aprobada' WHEN @EstadoProveedor IN (N'rejected', N'cancelled') THEN N'Rechazada' ELSE N'Pendiente' END;
    DECLARE @DetalleFinal NVARCHAR(1000) = @Detalle;

    BEGIN TRANSACTION;
    IF @EstadoResultado = N'Aprobada'
    BEGIN
        DECLARE @Importe DECIMAL(18,2), @Moneda NVARCHAR(10), @ReferenciaExterna NVARCHAR(500), @DuracionMeses INT, @VencimientoActual DATETIME2, @FechaBase DATETIME2, @NuevoVencimiento DATETIME2;
        SELECT @Importe = PrecioVigente, @Moneda = Moneda, @DuracionMeses = DuracionMeses FROM dbo.PlanComercial WHERE ID = @IdPlanNuevo;
        SELECT @VencimientoActual = FechaVencimiento, @IdAgencia = IdAgencia FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
        SET @FechaBase = CASE WHEN @VencimientoActual > SYSDATETIME() THEN @VencimientoActual ELSE SYSDATETIME() END;
        SET @NuevoVencimiento = DATEADD(MONTH, @DuracionMeses, @FechaBase);
        SET @ReferenciaExterna = @ReferenciaProveedor;

        UPDATE dbo.Suscripcion
        SET IdPlanComercial = @IdPlanNuevo, ImporteVigente = @Importe, ReferenciaExterna = @ReferenciaExterna, Estado = N'Activa', Activo = 1, FechaVencimiento = @NuevoVencimiento, FechaProximaRenovacion = @NuevoVencimiento, FechaBaja = NULL
        WHERE ID = @IdSuscripcion;

        INSERT INTO dbo.DocumentoComercial(IdAgencia, IdSuscripcion, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision)
        VALUES(@IdAgencia, @IdSuscripcion, N'Factura', N'PENDIENTE', N'Actualización de suscripción TeamBalance', @Importe, 0, @Moneda, N'Pagada', SYSDATETIME());
        SET @IdDocumento = CONVERT(INT, SCOPE_IDENTITY());
        UPDATE dbo.DocumentoComercial SET Numero = CONCAT(N'FC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdDocumento)), 8)) WHERE ID = @IdDocumento;
        IF @TipoOperacion = N'Regularización con crédito parcial'
        BEGIN
            DECLARE @CreditoAAplicar DECIMAL(18,2), @CreditoDisponible DECIMAL(18,2);
            SELECT @CreditoDisponible = COALESCE(SUM(SaldoPendiente), 0) FROM dbo.DocumentoComercial WITH (UPDLOCK, HOLDLOCK) WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @Moneda;
            SET @CreditoAAplicar = @CreditoOperacion;
            IF @CreditoAAplicar <= 0 OR @CreditoAAplicar >= @Importe OR @CreditoDisponible < @CreditoAAplicar THROW 53108, 'El saldo a favor cambió antes de confirmar el pago. La nota de crédito no fue aplicada.', 1;
            WHILE @CreditoAAplicar > 0
            BEGIN
                DECLARE @IdNotaCredito INT, @SaldoNotaCredito DECIMAL(18,2), @ImporteAplicado DECIMAL(18,2);
                SELECT TOP (1) @IdNotaCredito = ID, @SaldoNotaCredito = SaldoPendiente FROM dbo.DocumentoComercial WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @Moneda AND SaldoPendiente > 0 ORDER BY FechaEmision, ID;
                SET @ImporteAplicado = CASE WHEN @SaldoNotaCredito >= @CreditoAAplicar THEN @CreditoAAplicar ELSE @SaldoNotaCredito END;
                UPDATE dbo.DocumentoComercial SET SaldoPendiente = SaldoPendiente - @ImporteAplicado, Estado = CASE WHEN SaldoPendiente - @ImporteAplicado = 0 THEN N'Aplicada' ELSE N'Disponible' END WHERE ID = @IdNotaCredito;
                INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto) VALUES(@IdAgencia, @IdNotaCredito, N'Aplicacion nota credito', @ImporteAplicado, @Moneda, CONCAT(N'Credito aplicado a factura FC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdDocumento)), 8)));
                SET @CreditoAAplicar = @CreditoAAplicar - @ImporteAplicado;
            END;
            INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto) VALUES(@IdAgencia, @IdDocumento, CONCAT(N'Cobro ', @ProveedorOperacion), @ImporteOperacion, @MonedaOperacion, CONCAT(N'Pago de diferencia ', @ReferenciaProveedor));
        END
        ELSE
            INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto) VALUES(@IdAgencia, @IdDocumento, N'Cobro con tarjeta', @Importe, @Moneda, CONCAT(N'Pago de actualización ', @ReferenciaProveedor));

        SET @DetalleFinal = LEFT(CONCAT(@Detalle, N' Vigencia extendida hasta ', CONVERT(NVARCHAR(10), @NuevoVencimiento, 103), N'.'), 1000);
    END;

    UPDATE dbo.OperacionSuscripcion SET ReferenciaProveedor = @ReferenciaProveedor, Estado = @EstadoResultado, Detalle = @DetalleFinal, FechaActualizacion = SYSDATETIME() WHERE ID = @IdOperacion;
    SELECT @IdAgencia = IdAgencia FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    COMMIT TRANSACTION;
    EXEC dbo.usp_Suscripcion_ConsultarActualPorAgencia @IdAgencia;
END;
GO

/* Crea una operación por el importe que resta luego de aplicar el crédito disponible.
   La nota no se consume aquí: se aplica únicamente cuando Mercado Pago aprueba. */
CREATE OR ALTER PROCEDURE dbo.usp_CuentaCorriente_CrearSolicitudReactivacionConCreditoParcial
    @IdSuscripcion INT,
    @IdPlanNuevo INT,
    @ReferenciaInterna NVARCHAR(120),
    @Proveedor NVARCHAR(100),
    @ImportePago DECIMAL(18,2),
    @MonedaPago NVARCHAR(20),
    @CreditoAplicado DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdAgencia INT, @Activo BIT, @ImportePlan DECIMAL(18,2), @CreditoDisponible DECIMAL(18,2), @MonedaPlan NVARCHAR(20);
    SELECT @IdAgencia = IdAgencia, @Activo = Activo FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    IF @IdAgencia IS NULL OR @Activo = 1 THROW 53105, 'La suscripción no está disponible para reactivarse.', 1;
    SELECT @ImportePlan = PrecioVigente, @MonedaPlan = Moneda FROM dbo.PlanComercial WHERE ID = @IdPlanNuevo AND Activo = 1 AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= SYSDATETIME());
    IF @ImportePlan IS NULL THROW 53106, 'La modalidad elegida no está disponible para regularizar con crédito.', 1;
    IF @Proveedor NOT IN (N'MercadoPago', N'Stripe') OR @ImportePago <= 0 OR NULLIF(@MonedaPago, N'') IS NULL THROW 53109, 'El medio de pago o el importe de la diferencia no son válidos.', 1;
    SELECT @CreditoDisponible = COALESCE(SUM(SaldoPendiente), 0) FROM dbo.DocumentoComercial WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @MonedaPlan;
    IF @CreditoDisponible <= 0 OR @CreditoDisponible >= @ImportePlan OR @CreditoAplicado <= 0 OR @CreditoAplicado > @CreditoDisponible OR @CreditoAplicado >= @ImportePlan THROW 53107, 'El crédito disponible no requiere un pago de diferencia para esta modalidad.', 1;
    INSERT INTO dbo.OperacionSuscripcion(IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, ReferenciaInterna, Proveedor, Importe, Moneda, Estado, Detalle, CreditoAplicado)
    SELECT @IdSuscripcion, IdPlanComercial, @IdPlanNuevo, N'Regularización con crédito parcial', @ReferenciaInterna, @Proveedor, @ImportePago, @MonedaPago, N'Pendiente', N'Pendiente de aprobación; la nota de crédito se aplicará al confirmarse el pago.', @CreditoAplicado FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS IdOperacion, @IdSuscripcion AS IdSuscripcion, @IdPlanNuevo AS IdPlanNuevo, @ReferenciaInterna AS ReferenciaInterna, @ImportePago AS Importe, @MonedaPago AS Moneda, @Proveedor AS Proveedor;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CuentaCorriente_ReactivarConNotaCredito
    @IdAgencia INT,
    @IdSuscripcion INT,
    @IdPlanComercial INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ImportePlan DECIMAL(18,2), @Moneda NVARCHAR(10), @DuracionMeses INT, @CreditoDisponible DECIMAL(18,2), @IdFactura INT, @ImportePendiente DECIMAL(18,2);
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Suscripcion WITH (UPDLOCK, HOLDLOCK) WHERE ID = @IdSuscripcion AND IdAgencia = @IdAgencia AND Activo = 0)
        THROW 53102, 'La suscripción no está disponible para reactivarse con crédito.', 1;

    SELECT @ImportePlan = PrecioVigente, @Moneda = Moneda, @DuracionMeses = DuracionMeses
    FROM dbo.PlanComercial
    WHERE ID = @IdPlanComercial AND Activo = 1 AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= CONVERT(DATE, SYSDATETIME()));

    IF @ImportePlan IS NULL
        THROW 53103, 'La modalidad elegida no se encuentra disponible.', 1;

    SELECT @CreditoDisponible = COALESCE(SUM(SaldoPendiente), 0)
    FROM dbo.DocumentoComercial WITH (UPDLOCK, HOLDLOCK)
    WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @Moneda;

    IF @CreditoDisponible < @ImportePlan
        THROW 53104, 'El saldo a favor no alcanza para esta modalidad. Podés regularizarla con tarjeta o elegir una modalidad de menor importe.', 1;

    INSERT INTO dbo.DocumentoComercial(IdAgencia, IdSuscripcion, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision)
        VALUES(@IdAgencia, @IdSuscripcion, N'Factura', N'PENDIENTE', N'Reactivacion de suscripcion TeamBalance con nota de credito', @ImportePlan, 0, @Moneda, N'Pagada', SYSDATETIME());
    SET @IdFactura = CONVERT(INT, SCOPE_IDENTITY());
    UPDATE dbo.DocumentoComercial SET Numero = CONCAT(N'FC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdFactura)), 8)) WHERE ID = @IdFactura;

    SET @ImportePendiente = @ImportePlan;
    WHILE @ImportePendiente > 0
    BEGIN
        DECLARE @IdNotaCredito INT, @SaldoNotaCredito DECIMAL(18,2), @ImporteAplicado DECIMAL(18,2);
        SELECT TOP (1) @IdNotaCredito = ID, @SaldoNotaCredito = SaldoPendiente
        FROM dbo.DocumentoComercial
        WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @Moneda AND SaldoPendiente > 0
        ORDER BY FechaEmision, ID;

        SET @ImporteAplicado = CASE WHEN @SaldoNotaCredito >= @ImportePendiente THEN @ImportePendiente ELSE @SaldoNotaCredito END;
        UPDATE dbo.DocumentoComercial
        SET SaldoPendiente = SaldoPendiente - @ImporteAplicado,
            Estado = CASE WHEN SaldoPendiente - @ImporteAplicado = 0 THEN N'Aplicada' ELSE N'Disponible' END
        WHERE ID = @IdNotaCredito;
        INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto)
        VALUES(@IdAgencia, @IdNotaCredito, N'Aplicacion nota credito', @ImporteAplicado, @Moneda, CONCAT(N'Credito aplicado a factura FC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdFactura)), 8)));
        SET @ImportePendiente = @ImportePendiente - @ImporteAplicado;
    END;

    UPDATE dbo.Suscripcion
    SET IdPlanComercial = @IdPlanComercial,
        ImporteVigente = @ImportePlan,
        Estado = N'Activa',
        Activo = 1,
        FechaAlta = SYSDATETIME(),
        FechaVencimiento = DATEADD(MONTH, @DuracionMeses, SYSDATETIME()),
        FechaProximaRenovacion = DATEADD(MONTH, @DuracionMeses, SYSDATETIME()),
        FechaBaja = NULL
    WHERE ID = @IdSuscripcion;

    INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto)
    VALUES(@IdAgencia, @IdFactura, N'Cobro nota credito', @ImportePlan, @Moneda, N'Reactivacion cubierta con saldo a favor');

    COMMIT TRANSACTION;
    SELECT ID, IdAgencia, IdSuscripcion, IdDocumentoOrigen, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision, FechaVencimiento, Motivo
    FROM dbo.DocumentoComercial
    WHERE ID = @IdFactura;
END;
GO

/* Permite regularizar una suscripción cancelada con tarjeta; el pago sigue siendo validado por Mercado Pago. */
CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_CrearSolicitudCambioPlan
    @IdSuscripcion INT,
    @IdPlanNuevo INT,
    @ReferenciaInterna NVARCHAR(120),
    @Proveedor NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdPlanAnterior INT, @Activo BIT, @Importe DECIMAL(18,2), @Moneda NVARCHAR(20);
    SELECT @IdPlanAnterior = IdPlanComercial, @Activo = Activo FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    IF @IdPlanAnterior IS NULL
        THROW 53001, 'No existe la suscripción a regularizar.', 1;
    IF @Activo = 1 AND @IdPlanAnterior = @IdPlanNuevo
        THROW 53002, 'La modalidad seleccionada ya es la contratada.', 1;

    SELECT @Importe = PrecioVigente, @Moneda = Moneda
    FROM dbo.PlanComercial
    WHERE ID = @IdPlanNuevo AND Activo = 1 AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= SYSDATETIME());
    IF @Importe IS NULL
        THROW 53003, 'La modalidad seleccionada no se encuentra disponible.', 1;

    INSERT INTO dbo.OperacionSuscripcion(IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, ReferenciaInterna, Proveedor, Importe, Moneda, Estado, Detalle)
    VALUES(@IdSuscripcion, @IdPlanAnterior, @IdPlanNuevo, CASE WHEN @Activo = 1 THEN N'Actualización de modalidad' ELSE N'Regularización con tarjeta' END, @ReferenciaInterna, @Proveedor, @Importe, @Moneda, N'Pendiente', N'Solicitud enviada al proveedor externo.');

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS IdOperacion, @IdSuscripcion AS IdSuscripcion, @IdPlanNuevo AS IdPlanNuevo, @ReferenciaInterna AS ReferenciaInterna, @Importe AS Importe, @Moneda AS Moneda, @Proveedor AS Proveedor;
END;
GO
