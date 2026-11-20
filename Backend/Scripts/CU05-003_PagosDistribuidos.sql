/* Ejecutar después de CU05-004_CuentaCorriente_Documentos.sql en una misma sesión. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF COL_LENGTH('dbo.DocumentoComercial', 'IdOperacionSuscripcion') IS NULL
    ALTER TABLE dbo.DocumentoComercial ADD IdOperacionSuscripcion INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.DocumentoComercial') AND name='UX_Documento_OperacionSuscripcion')
    CREATE UNIQUE INDEX UX_Documento_OperacionSuscripcion ON dbo.DocumentoComercial(IdOperacionSuscripcion) WHERE IdOperacionSuscripcion IS NOT NULL;
GO
IF OBJECT_ID('dbo.PagoSuscripcion','U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoSuscripcion
    (
        ID INT IDENTITY PRIMARY KEY,
        IdOperacion INT NOT NULL REFERENCES dbo.OperacionSuscripcion(ID),
        ReferenciaInterna NVARCHAR(120) NOT NULL UNIQUE,
        Proveedor NVARCHAR(30) NOT NULL,
        MedioPago NVARCHAR(30) NOT NULL,
        ImportePlan DECIMAL(18,2) NOT NULL CHECK(ImportePlan>0),
        Importe DECIMAL(18,2) NOT NULL CHECK(Importe>0),
        Moneda NVARCHAR(10) NOT NULL,
        Estado NVARCHAR(30) NOT NULL DEFAULT N'Pendiente',
        ReferenciaProveedor NVARCHAR(250) NULL,
        UrlPago NVARCHAR(2048) NULL,
        Intento INT NOT NULL DEFAULT 0
    );
    CREATE UNIQUE INDEX UX_PagoSuscripcion_Proveedor ON dbo.PagoSuscripcion(Proveedor,ReferenciaProveedor) WHERE ReferenciaProveedor IS NOT NULL;
END;
GO
IF OBJECT_ID('dbo.ReservaCreditoSuscripcion','U') IS NULL
    CREATE TABLE dbo.ReservaCreditoSuscripcion
    (
        IdOperacion INT NOT NULL REFERENCES dbo.OperacionSuscripcion(ID),
        IdNotaCredito INT NOT NULL REFERENCES dbo.DocumentoComercial(ID),
        Importe DECIMAL(18,2) NOT NULL CHECK(Importe>0),
        Aplicada BIT NOT NULL DEFAULT 0,
        PRIMARY KEY(IdOperacion,IdNotaCredito)
    );
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarDistribucion
    @IdAgencia INT, @ReferenciaInterna NVARCHAR(120), @AplicadaAhora BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT O.ID AS IdOperacion,O.IdSuscripcion,O.IdPlanNuevo,S.IdAgencia,O.ReferenciaInterna,O.Importe,O.Moneda,O.Estado,@AplicadaAhora AS AplicadaAhora,
        COALESCE(O.CreditoAplicado,0) AS CreditoAplicado,
        (SELECT P.ID,P.ReferenciaInterna,P.Proveedor,P.MedioPago,P.ImportePlan,P.Importe,P.Moneda,P.Estado,
            COALESCE(P.ReferenciaProveedor,'') AS ReferenciaProveedor,COALESCE(P.UrlPago,'') AS UrlPago,P.Intento
         FROM dbo.PagoSuscripcion P WHERE P.IdOperacion=O.ID ORDER BY P.ID FOR JSON PATH) AS Pagos
    FROM dbo.OperacionSuscripcion O JOIN dbo.Suscripcion S ON S.ID=O.IdSuscripcion
    WHERE S.IdAgencia=@IdAgencia AND O.TipoOperacion=N'Actualización distribuida'
      AND (O.ReferenciaInterna=@ReferenciaInterna OR EXISTS(SELECT 1 FROM dbo.PagoSuscripcion P WHERE P.IdOperacion=O.ID AND P.ReferenciaInterna=@ReferenciaInterna));
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_CrearDistribucion
    @IdAgencia INT,@IdSuscripcion INT,@IdPlanNuevo INT,@ReferenciaInterna NVARCHAR(120),@Credito DECIMAL(18,2),@Pagos NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @Anterior INT,@Activo BIT,@Importe DECIMAL(18,2),@Moneda NVARCHAR(20),@IdOperacion INT;
    SELECT @Anterior=IdPlanComercial,@Activo=Activo FROM dbo.Suscripcion WITH(UPDLOCK,HOLDLOCK) WHERE ID=@IdSuscripcion AND IdAgencia=@IdAgencia;
    IF @Anterior IS NULL THROW 53201,'La suscripción no pertenece a esta agencia.',1;
    IF @Activo=1 AND @Anterior=@IdPlanNuevo THROW 53002,'La modalidad seleccionada ya es la contratada.',1;
    IF EXISTS(SELECT 1 FROM dbo.OperacionSuscripcion WITH(UPDLOCK,HOLDLOCK) WHERE IdSuscripcion=@IdSuscripcion AND TipoOperacion=N'Actualización distribuida' AND Estado=N'Pendiente')
        THROW 53202,'Ya hay una actualización pendiente. Continuá sus pagos desde el historial.',1;
    SELECT @Importe=PrecioVigente,@Moneda=Moneda FROM dbo.PlanComercial WHERE ID=@IdPlanNuevo AND Activo=1 AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta>=SYSDATETIME());
    IF @Importe IS NULL OR @Importe<=0 THROW 53003,'La modalidad seleccionada no se encuentra disponible.',1;
    DECLARE @Partes TABLE(ReferenciaInterna NVARCHAR(120),Proveedor NVARCHAR(30),MedioPago NVARCHAR(30),ImportePlan DECIMAL(18,2),Importe DECIMAL(18,2),Moneda NVARCHAR(10));
    INSERT @Partes SELECT ReferenciaInterna,Proveedor,MedioPago,ImportePlan,Importe,Moneda FROM OPENJSON(@Pagos)
    WITH(ReferenciaInterna NVARCHAR(120),Proveedor NVARCHAR(30),MedioPago NVARCHAR(30),ImportePlan DECIMAL(18,2),Importe DECIMAL(18,2),Moneda NVARCHAR(10));
    IF @Credito<0 OR @Credito+COALESCE((SELECT SUM(ImportePlan) FROM @Partes),0)<>@Importe
        THROW 53206,'La distribución no coincide con el importe del plan.',1;
    IF EXISTS(SELECT 1 FROM @Partes WHERE ImportePlan<=0 OR Importe<=0 OR Proveedor NOT IN('MercadoPago','Stripe') OR (Proveedor='Stripe' AND (Moneda<>@Moneda OR Importe<>ImportePlan)))
        THROW 53207,'Los importes o proveedores de la distribución no son válidos.',1;
    IF @Credito<0 OR COALESCE((SELECT SUM(ImportePlan) FROM @Partes),0)+@Credito<>@Importe OR EXISTS(SELECT 1 FROM @Partes WHERE Importe<=0 OR ImportePlan<=0 OR Proveedor NOT IN('MercadoPago','Stripe'))
        THROW 53203,'La distribución no coincide con el importe del plan.',1;
    DECLARE @Disponible DECIMAL(18,2);
    SELECT @Disponible=COALESCE(SUM(SaldoPendiente),0) FROM dbo.DocumentoComercial WITH(UPDLOCK,HOLDLOCK)
    WHERE IdAgencia=@IdAgencia AND Tipo=N'NotaCredito' AND Estado=N'Disponible' AND Moneda=@Moneda;
    IF @Credito>@Disponible THROW 53108,'El saldo a favor cambió. Actualizá la pantalla.',1;
    INSERT dbo.OperacionSuscripcion(IdSuscripcion,IdPlanAnterior,IdPlanNuevo,TipoOperacion,ReferenciaInterna,Proveedor,Importe,Moneda,Estado,Detalle,CreditoAplicado)
    VALUES(@IdSuscripcion,@Anterior,@IdPlanNuevo,N'Actualización distribuida',@ReferenciaInterna,CASE WHEN @Credito=0 AND (SELECT COUNT(*) FROM @Partes)=1 THEN (SELECT TOP(1) Proveedor FROM @Partes) ELSE N'Combinado' END,@Importe,@Moneda,N'Pendiente',N'Pago pendiente; el plan se actualizará al completar todas las partes.',@Credito);
    SET @IdOperacion=CONVERT(INT,SCOPE_IDENTITY());
    INSERT dbo.PagoSuscripcion(IdOperacion,ReferenciaInterna,Proveedor,MedioPago,ImportePlan,Importe,Moneda)
    SELECT @IdOperacion,ReferenciaInterna,Proveedor,MedioPago,ImportePlan,Importe,Moneda FROM @Partes;
    DECLARE @Pendiente DECIMAL(18,2)=@Credito,@IdNota INT,@Saldo DECIMAL(18,2),@Reservar DECIMAL(18,2);
    WHILE @Pendiente>0
    BEGIN
        SELECT TOP(1) @IdNota=ID,@Saldo=SaldoPendiente FROM dbo.DocumentoComercial
        WHERE IdAgencia=@IdAgencia AND Tipo=N'NotaCredito' AND Estado=N'Disponible' AND Moneda=@Moneda AND SaldoPendiente>0 ORDER BY FechaEmision,ID;
        SET @Reservar=CASE WHEN @Saldo>@Pendiente THEN @Pendiente ELSE @Saldo END;
        INSERT dbo.ReservaCreditoSuscripcion(IdOperacion,IdNotaCredito,Importe) VALUES(@IdOperacion,@IdNota,@Reservar);
        UPDATE dbo.DocumentoComercial SET SaldoPendiente=SaldoPendiente-@Reservar,Estado=CASE WHEN SaldoPendiente-@Reservar=0 THEN N'Reservada' ELSE N'Disponible' END WHERE ID=@IdNota;
        SET @Pendiente-=@Reservar;
    END;
    COMMIT;
    EXEC dbo.usp_Suscripcion_ConsultarDistribucion @IdAgencia,@ReferenciaInterna;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_DescartarSinCheckout
    @IdAgencia INT,@ReferenciaInterna NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @IdOperacion INT;
    SELECT @IdOperacion=O.ID FROM dbo.OperacionSuscripcion O WITH(UPDLOCK,HOLDLOCK) JOIN dbo.Suscripcion S ON S.ID=O.IdSuscripcion
    WHERE O.ReferenciaInterna=@ReferenciaInterna AND O.Estado=N'Pendiente' AND O.TipoOperacion=N'Actualización distribuida' AND S.IdAgencia=@IdAgencia;
    IF @IdOperacion IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.PagoSuscripcion WHERE IdOperacion=@IdOperacion AND (UrlPago IS NOT NULL OR Estado=N'Aprobada'))
    BEGIN
        UPDATE D SET SaldoPendiente=D.SaldoPendiente+R.Importe,Estado=N'Disponible'
        FROM dbo.DocumentoComercial D JOIN dbo.ReservaCreditoSuscripcion R ON R.IdNotaCredito=D.ID WHERE R.IdOperacion=@IdOperacion AND R.Aplicada=0;
        DELETE dbo.ReservaCreditoSuscripcion WHERE IdOperacion=@IdOperacion AND Aplicada=0;
        UPDATE dbo.OperacionSuscripcion SET Estado=N'Cancelada',Detalle=N'No se pudo preparar el checkout. El crédito reservado quedó disponible.',FechaActualizacion=SYSDATETIME() WHERE ID=@IdOperacion;
    END;
    COMMIT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_GuardarCheckout
    @IdAgencia INT,@ReferenciaParte NVARCHAR(120),@Intento INT,@UrlPago NVARCHAR(2048)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE P SET UrlPago=@UrlPago
    FROM dbo.PagoSuscripcion P JOIN dbo.OperacionSuscripcion O ON O.ID=P.IdOperacion JOIN dbo.Suscripcion S ON S.ID=O.IdSuscripcion
    WHERE P.ReferenciaInterna=@ReferenciaParte AND P.Intento=@Intento AND S.IdAgencia=@IdAgencia AND P.Estado<>N'Aprobada' AND P.UrlPago IS NULL;
END;
GO




CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_AplicarResultadoCambioPlan
    @ReferenciaInterna NVARCHAR(120),
    @ReferenciaProveedor NVARCHAR(250),
    @EstadoProveedor NVARCHAR(80),
    @Detalle NVARCHAR(1000),
    @DevolverResultado BIT = 1,
    @RegistrarCobro BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    DECLARE @IdOperacion INT, @IdSuscripcion INT, @IdPlanNuevo INT, @EstadoOperacion NVARCHAR(80), @TipoOperacion NVARCHAR(120), @ImporteOperacion DECIMAL(18,2), @MonedaOperacion NVARCHAR(20), @ProveedorOperacion NVARCHAR(100), @CreditoOperacion DECIMAL(18,2);
    DECLARE @IdAgencia INT, @IdDocumento INT;
    SELECT @IdOperacion = ID, @IdSuscripcion = IdSuscripcion, @IdPlanNuevo = IdPlanNuevo, @EstadoOperacion = Estado, @TipoOperacion = TipoOperacion, @ImporteOperacion = Importe, @MonedaOperacion = Moneda, @ProveedorOperacion = Proveedor, @CreditoOperacion = CreditoAplicado
    FROM dbo.OperacionSuscripcion WITH (UPDLOCK,HOLDLOCK)
    WHERE ReferenciaInterna = @ReferenciaInterna;

    IF @IdOperacion IS NULL
        THROW 53004, 'No existe la solicitud de actualización indicada.', 1;
    IF @EstadoOperacion <> N'Pendiente'
    BEGIN
        SELECT @IdAgencia=IdAgencia FROM dbo.Suscripcion WHERE ID=@IdSuscripcion;
        COMMIT;
        IF @DevolverResultado=1 EXEC dbo.usp_Suscripcion_ConsultarActualPorAgencia @IdAgencia;
        RETURN;
    END;

    DECLARE @EstadoResultado NVARCHAR(80) = CASE WHEN @EstadoProveedor = N'approved' THEN N'Aprobada' WHEN @EstadoProveedor IN (N'rejected', N'cancelled') THEN N'Rechazada' ELSE N'Pendiente' END;
    DECLARE @DetalleFinal NVARCHAR(1000) = @Detalle;

    IF @EstadoResultado = N'Aprobada'
    BEGIN
        DECLARE @Importe DECIMAL(18,2), @Moneda NVARCHAR(10), @ReferenciaExterna NVARCHAR(500), @DuracionMeses INT, @VencimientoActual DATETIME2, @FechaBase DATETIME2, @NuevoVencimiento DATETIME2;
        SELECT @Importe = PrecioVigente, @Moneda = Moneda, @DuracionMeses = DuracionMeses FROM dbo.PlanComercial WHERE ID = @IdPlanNuevo;
        IF @TipoOperacion=N'Actualización distribuida'
        BEGIN
            SET @Importe=@ImporteOperacion;
            SET @Moneda=@MonedaOperacion;
        END;
        SELECT @VencimientoActual = FechaVencimiento, @IdAgencia = IdAgencia FROM dbo.Suscripcion WITH(UPDLOCK,HOLDLOCK) WHERE ID = @IdSuscripcion;
        SET @FechaBase = CASE WHEN @VencimientoActual > SYSDATETIME() THEN @VencimientoActual ELSE SYSDATETIME() END;
        SET @NuevoVencimiento = DATEADD(MONTH, @DuracionMeses, @FechaBase);
        SET @ReferenciaExterna = @ReferenciaProveedor;

        UPDATE dbo.Suscripcion
        SET IdPlanComercial = @IdPlanNuevo, ImporteVigente = @Importe, ReferenciaExterna = @ReferenciaExterna, Estado = N'Activa', Activo = 1, FechaVencimiento = @NuevoVencimiento, FechaProximaRenovacion = @NuevoVencimiento, FechaBaja = NULL
        WHERE ID = @IdSuscripcion;

        INSERT INTO dbo.DocumentoComercial(IdAgencia, IdSuscripcion, Tipo, Numero, Concepto, Importe, SaldoPendiente, Moneda, Estado, FechaEmision, IdOperacionSuscripcion)
        VALUES(@IdAgencia, @IdSuscripcion, N'Factura', N'PENDIENTE', N'Actualización de suscripción TeamBalance', @Importe, 0, @Moneda, N'Pagada', SYSDATETIME(), @IdOperacion);
        SET @IdDocumento = CONVERT(INT, SCOPE_IDENTITY());
        UPDATE dbo.DocumentoComercial SET Numero = CONCAT(N'FC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdDocumento)), 8)) WHERE ID = @IdDocumento;
        IF @TipoOperacion IN (N'Regularización con crédito parcial', N'Actualización con crédito parcial', N'Actualización con crédito completo')
        BEGIN
            DECLARE @CreditoAAplicar DECIMAL(18,2), @CreditoDisponible DECIMAL(18,2);
            SELECT @CreditoDisponible = COALESCE(SUM(SaldoPendiente), 0) FROM dbo.DocumentoComercial WITH (UPDLOCK, HOLDLOCK) WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @Moneda;
            SET @CreditoAAplicar = @CreditoOperacion;
            IF @CreditoAAplicar <= 0 OR @CreditoAAplicar > @Importe OR @CreditoDisponible < @CreditoAAplicar OR (@CreditoAAplicar = @Importe AND @TipoOperacion <> N'Actualización con crédito completo') THROW 53108, 'El saldo a favor cambió antes de confirmar el pago. La nota de crédito no fue aplicada.', 1;
            WHILE @CreditoAAplicar > 0
            BEGIN
                DECLARE @IdNotaCredito INT, @SaldoNotaCredito DECIMAL(18,2), @ImporteAplicado DECIMAL(18,2);
                SELECT TOP (1) @IdNotaCredito = ID, @SaldoNotaCredito = SaldoPendiente FROM dbo.DocumentoComercial WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @Moneda AND SaldoPendiente > 0 ORDER BY FechaEmision, ID;
                SET @ImporteAplicado = CASE WHEN @SaldoNotaCredito >= @CreditoAAplicar THEN @CreditoAAplicar ELSE @SaldoNotaCredito END;
                UPDATE dbo.DocumentoComercial SET SaldoPendiente = SaldoPendiente - @ImporteAplicado, Estado = CASE WHEN SaldoPendiente - @ImporteAplicado = 0 THEN N'Aplicada' ELSE N'Disponible' END WHERE ID = @IdNotaCredito;
                INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto) VALUES(@IdAgencia, @IdNotaCredito, N'Aplicacion nota credito', @ImporteAplicado, @Moneda, CONCAT(N'Credito aplicado a factura FC-', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(20), @IdDocumento)), 8)));
                SET @CreditoAAplicar = @CreditoAAplicar - @ImporteAplicado;
            END;
            IF @ImporteOperacion > 0
                INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto) VALUES(@IdAgencia, @IdDocumento, CONCAT(N'Cobro ', @ProveedorOperacion), @ImporteOperacion, @MonedaOperacion, CONCAT(N'Pago de diferencia ', @ReferenciaProveedor));
        END
        ELSE IF @RegistrarCobro=1
            INSERT INTO dbo.MovimientoCuentaCorriente(IdAgencia, IdDocumentoComercial, Tipo, Importe, Moneda, Concepto) VALUES(@IdAgencia, @IdDocumento, N'Cobro con tarjeta', @Importe, @Moneda, CONCAT(N'Pago de actualización ', @ReferenciaProveedor));

        SET @DetalleFinal = LEFT(CONCAT(@Detalle, N' Vigencia extendida hasta ', CONVERT(NVARCHAR(10), @NuevoVencimiento, 103), N'.'), 1000);
    END;

    UPDATE dbo.OperacionSuscripcion SET ReferenciaProveedor = @ReferenciaProveedor, Estado = @EstadoResultado, Detalle = @DetalleFinal, FechaActualizacion = SYSDATETIME() WHERE ID = @IdOperacion;
    SELECT @IdAgencia = IdAgencia FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    COMMIT TRANSACTION;
    IF @DevolverResultado=1 EXEC dbo.usp_Suscripcion_ConsultarActualPorAgencia @IdAgencia;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarOperacionPendiente
    @ReferenciaInterna NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT O.ID AS IdOperacion,O.IdSuscripcion,O.IdPlanNuevo,O.ReferenciaInterna,O.Importe,O.Moneda,O.Proveedor,O.TipoOperacion,O.Estado,O.ReferenciaProveedor,S.IdAgencia
    FROM dbo.OperacionSuscripcion O JOIN dbo.Suscripcion S ON S.ID=O.IdSuscripcion WHERE O.ReferenciaInterna=@ReferenciaInterna;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConfirmarParte
    @IdAgencia INT,@ReferenciaInterna NVARCHAR(120),@ReferenciaParte NVARCHAR(120),@ReferenciaProveedor NVARCHAR(250),@EstadoProveedor NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @IdOperacion INT,@EstadoOperacion NVARCHAR(80),@IdSuscripcion INT,@AplicadaAhora BIT=0;
    SELECT @IdOperacion=O.ID,@EstadoOperacion=O.Estado,@IdSuscripcion=O.IdSuscripcion
    FROM dbo.OperacionSuscripcion O WITH(UPDLOCK,HOLDLOCK) JOIN dbo.Suscripcion S ON S.ID=O.IdSuscripcion
    WHERE O.ReferenciaInterna=@ReferenciaInterna AND O.TipoOperacion=N'Actualización distribuida' AND S.IdAgencia=@IdAgencia;
    IF @IdOperacion IS NULL THROW 53203,'La operación no pertenece a esta agencia.',1;
    IF @EstadoOperacion=N'Pendiente'
    BEGIN
        IF NULLIF(@ReferenciaParte,'') IS NOT NULL
        BEGIN
            DECLARE @IdParte INT,@EstadoParte NVARCHAR(30),@ReferenciaActual NVARCHAR(250);
            SELECT @IdParte=ID,@EstadoParte=Estado,@ReferenciaActual=ReferenciaProveedor
            FROM dbo.PagoSuscripcion WITH(UPDLOCK,HOLDLOCK) WHERE IdOperacion=@IdOperacion AND ReferenciaInterna=@ReferenciaParte;
            IF @IdParte IS NULL THROW 53204,'La parte no corresponde a esta actualización.',1;
            IF @EstadoParte<>N'Aprobada'
            BEGIN
                IF EXISTS(SELECT 1 FROM dbo.PagoSuscripcion WHERE IdOperacion=@IdOperacion AND ID<@IdParte AND Estado<>N'Aprobada')
                    THROW 53205,'Primero completá la parte anterior del pago.',1;
                DECLARE @Resultado NVARCHAR(30)=CASE WHEN @EstadoProveedor='approved' THEN N'Aprobada' WHEN @EstadoProveedor IN('rejected','cancelled') THEN N'Rechazada' ELSE N'Pendiente' END;
                UPDATE dbo.PagoSuscripcion SET Estado=@Resultado,ReferenciaProveedor=@ReferenciaProveedor,
                    UrlPago=CASE WHEN @Resultado=N'Rechazada' THEN NULL ELSE UrlPago END,
                    Intento=CASE WHEN @Resultado=N'Rechazada' THEN Intento+1 ELSE Intento END
                WHERE ID=@IdParte AND NOT (@Resultado=N'Rechazada' AND @EstadoParte=N'Rechazada'
                    AND COALESCE(@ReferenciaActual,N'')=@ReferenciaProveedor);
            END;
        END;
        IF NOT EXISTS(SELECT 1 FROM dbo.PagoSuscripcion WHERE IdOperacion=@IdOperacion AND Estado<>N'Aprobada')
        BEGIN
            EXEC dbo.usp_Suscripcion_AplicarResultadoCambioPlan @ReferenciaInterna,@ReferenciaInterna,N'approved',N'Todas las partes del pago fueron confirmadas.',0,0;
            DECLARE @IdFactura INT;
            SELECT @IdFactura=ID FROM dbo.DocumentoComercial WHERE IdOperacionSuscripcion=@IdOperacion;
            INSERT dbo.MovimientoCuentaCorriente(IdAgencia,IdDocumentoComercial,Tipo,Importe,Moneda,Concepto)
            SELECT @IdAgencia,@IdFactura,CONCAT(N'Cobro ',Proveedor),Importe,Moneda,LEFT(CONCAT(N'Pago ',ReferenciaProveedor),300)
            FROM dbo.PagoSuscripcion WHERE IdOperacion=@IdOperacion;
            INSERT dbo.MovimientoCuentaCorriente(IdAgencia,IdDocumentoComercial,Tipo,Importe,Moneda,Concepto)
            SELECT @IdAgencia,R.IdNotaCredito,N'Aplicacion nota credito',R.Importe,D.Moneda,CONCAT(N'Crédito aplicado a factura ',@IdFactura)
            FROM dbo.ReservaCreditoSuscripcion R JOIN dbo.DocumentoComercial D ON D.ID=R.IdNotaCredito WHERE R.IdOperacion=@IdOperacion AND R.Aplicada=0;
            UPDATE D SET Estado=CASE WHEN D.SaldoPendiente=0 THEN N'Aplicada' ELSE D.Estado END
            FROM dbo.DocumentoComercial D JOIN dbo.ReservaCreditoSuscripcion R ON R.IdNotaCredito=D.ID WHERE R.IdOperacion=@IdOperacion;
            UPDATE dbo.ReservaCreditoSuscripcion SET Aplicada=1 WHERE IdOperacion=@IdOperacion;
            SET @AplicadaAhora=1;
        END;
    END;
    COMMIT;
    EXEC dbo.usp_Suscripcion_ConsultarDistribucion @IdAgencia,@ReferenciaInterna,@AplicadaAhora;
END;
GO

-- Conserva la ruta de crédito de versiones anteriores con las opciones SET correctas.
CREATE OR ALTER PROCEDURE dbo.usp_CuentaCorriente_CrearSolicitudActualizacionConCredito
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
    SET XACT_ABORT ON;

    DECLARE @IdAgencia INT, @IdPlanAnterior INT, @Activo BIT, @ImportePlan DECIMAL(18,2), @MonedaPlan NVARCHAR(20), @CreditoDisponible DECIMAL(18,2);
    SELECT @IdAgencia = IdAgencia, @IdPlanAnterior = IdPlanComercial, @Activo = Activo FROM dbo.Suscripcion WHERE ID = @IdSuscripcion;
    IF @IdAgencia IS NULL OR @Activo <> 1 THROW 53111, 'La suscripción debe estar activa para actualizarse con saldo a favor.', 1;
    IF @IdPlanAnterior = @IdPlanNuevo THROW 53002, 'La modalidad seleccionada ya es la contratada.', 1;

    SELECT @ImportePlan = PrecioVigente, @MonedaPlan = Moneda FROM dbo.PlanComercial
    WHERE ID = @IdPlanNuevo AND Activo = 1 AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= SYSDATETIME());
    IF @ImportePlan IS NULL THROW 53003, 'La modalidad seleccionada no se encuentra disponible.', 1;

    SELECT @CreditoDisponible = COALESCE(SUM(SaldoPendiente), 0) FROM dbo.DocumentoComercial
    WHERE IdAgencia = @IdAgencia AND Tipo = N'NotaCredito' AND Estado = N'Disponible' AND Moneda = @MonedaPlan;
    IF @CreditoAplicado <= 0 OR @CreditoAplicado <> CASE WHEN @CreditoDisponible >= @ImportePlan THEN @ImportePlan ELSE @CreditoDisponible END
        THROW 53108, 'El saldo a favor disponible cambió. Actualizá la pantalla e intentá nuevamente.', 1;

    IF @CreditoAplicado = @ImportePlan
    BEGIN
        IF @Proveedor <> N'SaldoAFavor' OR @ImportePago <> 0 OR @MonedaPago <> @MonedaPlan
            THROW 53112, 'El pago cubierto por saldo a favor no requiere proveedor externo.', 1;
    END
    ELSE
    BEGIN
        IF @Proveedor NOT IN (N'MercadoPago', N'Stripe') OR @ImportePago <= 0
            THROW 53113, 'Seleccioná un proveedor válido para pagar la diferencia.', 1;
        IF @Proveedor = N'Stripe' AND (@MonedaPago <> @MonedaPlan OR @ImportePago <> @ImportePlan - @CreditoAplicado)
            THROW 53114, 'El importe de Stripe no coincide con la diferencia.', 1;
        IF @Proveedor = N'MercadoPago' AND @MonedaPago <> N'ARS'
            THROW 53115, 'La moneda de Mercado Pago debe ser ARS.', 1;
    END;

    INSERT INTO dbo.OperacionSuscripcion(IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, ReferenciaInterna, Proveedor, Importe, Moneda, Estado, Detalle, CreditoAplicado)
    VALUES(@IdSuscripcion, @IdPlanAnterior, @IdPlanNuevo,
        CASE WHEN @CreditoAplicado = @ImportePlan THEN N'Actualización con crédito completo' ELSE N'Actualización con crédito parcial' END,
        @ReferenciaInterna, @Proveedor, @ImportePago, @MonedaPago, N'Pendiente',
        CASE WHEN @CreditoAplicado = @ImportePlan THEN N'Actualización cubierta con saldo a favor.' ELSE N'Solicitud enviada al proveedor para pagar la diferencia.' END,
        @CreditoAplicado);

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS IdOperacion, @IdSuscripcion AS IdSuscripcion, @IdPlanNuevo AS IdPlanNuevo,
        @ReferenciaInterna AS ReferenciaInterna, @ImportePago AS Importe, @MonedaPago AS Moneda, @Proveedor AS Proveedor;
END;
GO
