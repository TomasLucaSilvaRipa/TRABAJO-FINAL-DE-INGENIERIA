/*
  CU05-001 - Contratar servicio TeamBalance
  Ejecutar una sola vez sobre la base TeamBalance antes de usar el flujo.
*/
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
    @Periodicidad NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Periodicidad NOT IN (N'Mensual', N'Anual')
        THROW 50001, 'La periodicidad seleccionada no está disponible.', 1;

    DECLARE @IdPlanComercial INT;
    DECLARE @Importe DECIMAL(18, 2);
    DECLARE @Moneda NVARCHAR(10);

    SELECT TOP (1)
        @IdPlanComercial = ID,
        @Importe = PrecioVigente,
        @Moneda = Moneda
    FROM dbo.PlanComercial
    WHERE Periodicidad = @Periodicidad
      AND Activo = 1
    ORDER BY ID;

    IF @IdPlanComercial IS NULL
        THROW 50002, 'No existe un plan activo para la periodicidad seleccionada.', 1;

    -- Checkout Pro TEST: mantener el importe temporal dentro del límite de las
    -- cuentas de prueba. El plan comercial real se aplicará al pasar a producción.
    SET @Importe = 1000.00;

    BEGIN TRANSACTION;

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

    COMMIT TRANSACTION;

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

    SET @EstadoContratacion = CASE
        WHEN @EstadoProveedor = N'approved' THEN N'Aprobada'
        WHEN @EstadoProveedor IN (N'rejected', N'cancelled') THEN N'Rechazada'
        ELSE N'Pendiente'
    END;

    BEGIN TRANSACTION;

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

    COMMIT TRANSACTION;

    EXEC dbo.usp_Contratacion_ConsultarEstado @ReferenciaContratacion;
END;
GO
