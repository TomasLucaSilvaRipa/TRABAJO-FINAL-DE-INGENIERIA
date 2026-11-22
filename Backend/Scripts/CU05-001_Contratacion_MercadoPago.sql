/*
  CU05-001 - Contratar servicio TeamBalance
  Ejecutar una sola vez sobre la base TeamBalance antes de usar el flujo.
*/
GO

IF COL_LENGTH('dbo.OperacionPago', 'OrdenCombinado') IS NULL
    ALTER TABLE dbo.OperacionPago ADD OrdenCombinado INT NULL, MedioPagoCombinado NVARCHAR(30) NULL,
        UrlPagoCombinado NVARCHAR(2048) NULL, IntentoCombinado INT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_CrearPendiente
    @ReferenciaContratacion NVARCHAR(250),
    @ReferenciaOperacion NVARCHAR(250),
    @NombreComercialAgencia NVARCHAR(150),
    @RazonSocial NVARCHAR(200),
    @CUIT NVARCHAR(20),
    @CondicionFiscal NVARCHAR(80),
    @EmailFacturacion NVARCHAR(150),
    @TelefonoContacto NVARCHAR(50),
    @NombreResponsable NVARCHAR(100),
    @ApellidoResponsable NVARCHAR(100),
    @EmailLaboralResponsable NVARCHAR(150),
    @CargoResponsable NVARCHAR(100),
    @ProveedorPagoSeleccionado NVARCHAR(100),
    @IdPlanComercial INT,
    @PagosCombinados NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Importe DECIMAL(18, 2);
    DECLARE @Moneda NVARCHAR(10);
    DECLARE @NombrePlan NVARCHAR(100);

    SELECT TOP (1)
        @Importe = PrecioVigente,
        @Moneda = Moneda,
        @NombrePlan = Nombre
    FROM dbo.PlanComercial
    WHERE ID = @IdPlanComercial
      AND Activo = 1

    IF @Importe IS NULL
        THROW 50002, 'No existe un plan comercial activo con el identificador indicado.', 1;

    -- Mercado Pago de prueba trabaja con los importes ARS configurados. Stripe
    -- usa la cotizacion real del plan, que en TeamBalance es USD.
    IF @ProveedorPagoSeleccionado = N'MercadoPago'
    BEGIN
        SET @Importe = CASE @NombrePlan
            WHEN N'Entry' THEN 1000.00
            WHEN N'Business' THEN 1500.00
            WHEN N'Corporate' THEN 2000.00
            ELSE 1000.00
        END;
        SET @Moneda = N'ARS';
    END;

    IF @PagosCombinados IS NOT NULL BEGIN TRANSACTION;

    INSERT INTO dbo.ContratacionServicio
    (
        IdPlanComercial,
        ReferenciaContratacion,
        NombreComercialAgencia,
        RazonSocial,
        CUIT,
        CondicionFiscal,
        EmailFacturacion,
        TelefonoContacto,
        NombreResponsable,
        ApellidoResponsable,
        EmailLaboralResponsable,
        CargoResponsable,
        ProveedorPagoSeleccionado,
        EstadoContratacion,
        FechaSolicitud,
        Activo
    )
    VALUES
    (
        @IdPlanComercial,
        @ReferenciaContratacion,
        @NombreComercialAgencia,
        NULLIF(@RazonSocial, N''),
        @CUIT,
        NULLIF(@CondicionFiscal, N''),
        NULLIF(@EmailFacturacion, N''),
        NULLIF(@TelefonoContacto, N''),
        @NombreResponsable,
        @ApellidoResponsable,
        @EmailLaboralResponsable,
        NULLIF(@CargoResponsable, N''),
        @ProveedorPagoSeleccionado,
        N'Pendiente',
        SYSDATETIME(),
        1
    );

    DECLARE @IdContratacion INT = CONVERT(INT, SCOPE_IDENTITY());

    IF @PagosCombinados IS NOT NULL
    BEGIN
        INSERT INTO dbo.OperacionPago
            (IdContratacionServicio, ReferenciaInterna, Proveedor, Importe, Moneda, Estado, FechaCreacion,
             OrdenCombinado, MedioPagoCombinado, IntentoCombinado)
        SELECT @IdContratacion, parte.ReferenciaInterna, parte.Proveedor, parte.Importe, parte.Moneda,
               N'Pendiente', SYSDATETIME(), CONVERT(INT, entrada.[key]) + 1, parte.MedioPago, 1
        FROM OPENJSON(@PagosCombinados) AS entrada
        CROSS APPLY OPENJSON(entrada.value) WITH (
            ReferenciaInterna NVARCHAR(250), Proveedor NVARCHAR(100), Importe DECIMAL(18, 2),
            Moneda NVARCHAR(10), MedioPago NVARCHAR(30)
        ) AS parte;
        IF @@ROWCOUNT < 2 THROW 50005, 'La contratación combinada requiere al menos dos pagos.', 1;
        COMMIT TRANSACTION;
    END
    ELSE
    INSERT INTO dbo.OperacionPago
    (
        IdContratacionServicio,
        ReferenciaInterna,
        Proveedor,
        Importe,
        Moneda,
        Estado,
        FechaCreacion
    )
    VALUES
    (
        @IdContratacion,
        @ReferenciaOperacion,
        @ProveedorPagoSeleccionado,
        @Importe,
        @Moneda,
        N'Pendiente',
        SYSDATETIME()
    );

    SELECT
        @IdContratacion AS IdContratacion,
        @IdPlanComercial AS IdPlanComercial,
        @ReferenciaContratacion AS ReferenciaContratacion,
        @ReferenciaOperacion AS ReferenciaOperacion,
        @Importe AS Importe,
        @Moneda AS Moneda;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_ConsultarEstado
    @ReferenciaContratacion NVARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        contratacion.ReferenciaContratacion,
        contratacion.EstadoContratacion,
        contratacion.ProveedorPagoSeleccionado,
        contratacion.IdAgencia,
        contratacion.IdUsuario,
        operacion.Importe,
        operacion.Moneda
    FROM dbo.ContratacionServicio AS contratacion
    INNER JOIN dbo.OperacionPago AS operacion
        ON operacion.IdContratacionServicio = contratacion.ID
    WHERE contratacion.ReferenciaContratacion = @ReferenciaContratacion
    ORDER BY operacion.ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_ActualizarResultadoPago
    @ReferenciaContratacion NVARCHAR(250),
    @ReferenciaProveedor NVARCHAR(250),
    @EstadoProveedor NVARCHAR(50),
    @MensajeRespuesta NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdContratacion INT;
    DECLARE @EstadoContratacion NVARCHAR(50);

    SELECT @IdContratacion = ID
    FROM dbo.ContratacionServicio
    WHERE ReferenciaContratacion = @ReferenciaContratacion;

    IF @IdContratacion IS NULL
        THROW 50003, 'No existe una contratación con la referencia indicada.', 1;

    IF EXISTS (SELECT 1 FROM dbo.ContratacionServicio WHERE ID = @IdContratacion AND ProveedorPagoSeleccionado = N'Combinado')
        THROW 50006, 'Usá la verificación de la parte para una contratación combinada.', 1;

    SET @EstadoContratacion = CASE
        WHEN @EstadoProveedor = N'approved' THEN N'Aprobada'
        WHEN @EstadoProveedor IN (N'rejected', N'cancelled') THEN N'Rechazada'
        ELSE N'Pendiente'
    END;

    UPDATE dbo.OperacionPago
    SET ReferenciaProveedor = @ReferenciaProveedor,
        Estado = @EstadoProveedor,
        FechaActualizacion = SYSDATETIME(),
        FechaAprobacion = CASE WHEN @EstadoProveedor = N'approved' THEN SYSDATETIME() ELSE NULL END
    WHERE ID =
    (
        SELECT MAX(ID)
        FROM dbo.OperacionPago
        WHERE IdContratacionServicio = @IdContratacion
    );

    UPDATE dbo.ContratacionServicio
    SET EstadoContratacion = @EstadoContratacion,
        FechaRespuesta = SYSDATETIME(),
        MensajeRespuesta = @MensajeRespuesta
    WHERE ID = @IdContratacion;

    EXEC dbo.usp_Contratacion_ConsultarEstado @ReferenciaContratacion;
END;
GO


CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_ConsultarPartes
    @ReferenciaContratacion NVARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.IdPlanComercial, p.ReferenciaInterna, p.Proveedor, p.MedioPagoCombinado AS MedioPago,
           p.Importe, p.Moneda, p.Estado, p.UrlPagoCombinado AS UrlPago, p.ReferenciaProveedor, p.IntentoCombinado AS Intento
    FROM dbo.ContratacionServicio AS c
    INNER JOIN dbo.OperacionPago AS p ON p.IdContratacionServicio = c.ID
    WHERE c.ReferenciaContratacion = @ReferenciaContratacion AND c.ProveedorPagoSeleccionado = N'Combinado'
    ORDER BY p.OrdenCombinado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_GuardarCheckoutParte
    @ReferenciaContratacion NVARCHAR(250),
    @ReferenciaParte NVARCHAR(250),
    @Intento INT,
    @UrlPago NVARCHAR(2048),
    @ReferenciaProveedor NVARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE p
    SET UrlPagoCombinado = @UrlPago, ReferenciaProveedor = NULLIF(@ReferenciaProveedor, N'')
    FROM dbo.OperacionPago AS p
    INNER JOIN dbo.ContratacionServicio AS c ON c.ID = p.IdContratacionServicio
    WHERE c.ReferenciaContratacion = @ReferenciaContratacion AND c.ProveedorPagoSeleccionado = N'Combinado'
      AND p.ReferenciaInterna = @ReferenciaParte AND p.IntentoCombinado = @Intento
      AND p.Estado <> N'approved' AND p.UrlPagoCombinado IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_ConfirmarParte
    @ReferenciaContratacion NVARCHAR(250),
    @ReferenciaParte NVARCHAR(250),
    @Intento INT,
    @ReferenciaProveedor NVARCHAR(250),
    @EstadoProveedor NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @IdContratacion INT;
    DECLARE @AprobadaAhora BIT = 0;
    BEGIN TRANSACTION;

    SELECT @IdContratacion = ID
    FROM dbo.ContratacionServicio WITH (UPDLOCK, HOLDLOCK)
    WHERE ReferenciaContratacion = @ReferenciaContratacion AND ProveedorPagoSeleccionado = N'Combinado';

    IF @IdContratacion IS NULL
        THROW 50004, 'No encontramos esa contratación con pago combinado.', 1;

    UPDATE p
    SET ReferenciaProveedor = CASE WHEN @EstadoProveedor = N'cancelled' AND p.Proveedor = N'Stripe' THEN NULL ELSE @ReferenciaProveedor END,
        Estado = CASE WHEN @EstadoProveedor = N'cancelled' AND p.Proveedor = N'Stripe' THEN N'Pendiente' ELSE @EstadoProveedor END,
        FechaActualizacion = SYSDATETIME(),
        FechaAprobacion = CASE WHEN @EstadoProveedor = N'approved' THEN SYSDATETIME() END,
        UrlPagoCombinado = CASE WHEN @EstadoProveedor = N'cancelled' AND p.Proveedor = N'Stripe' THEN NULL ELSE p.UrlPagoCombinado END,
        IntentoCombinado = p.IntentoCombinado + CASE WHEN @EstadoProveedor = N'cancelled' AND p.Proveedor = N'Stripe' THEN 1 ELSE 0 END
    FROM dbo.OperacionPago AS p
    WHERE p.IdContratacionServicio = @IdContratacion AND p.ReferenciaInterna = @ReferenciaParte
      AND p.IntentoCombinado = @Intento AND p.Estado <> N'approved'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.OperacionPago AS anterior
          WHERE anterior.IdContratacionServicio = @IdContratacion AND anterior.OrdenCombinado < p.OrdenCombinado
            AND anterior.Estado <> N'approved'
      );

    IF NOT EXISTS (SELECT 1 FROM dbo.OperacionPago WHERE IdContratacionServicio = @IdContratacion AND Estado <> N'approved')
    BEGIN
        UPDATE dbo.ContratacionServicio
        SET EstadoContratacion = N'Aprobada', FechaRespuesta = SYSDATETIME(),
            MensajeRespuesta = N'Se confirmaron todas las partes del pago.'
        WHERE ID = @IdContratacion AND EstadoContratacion = N'Pendiente';
        IF @@ROWCOUNT = 1 SET @AprobadaAhora = 1;
    END;

    COMMIT TRANSACTION;
    SELECT @AprobadaAhora AS AprobadaAhora;
END;
GO
