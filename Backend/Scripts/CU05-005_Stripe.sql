/*
  CU05-005 - Integracion Stripe
  Ejecutar despues de CU05-004_CuentaCorriente_Documentos.sql.
  Agrega el proveedor de renovacion sin modificar operaciones ni datos existentes.
*/
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.Suscripcion', N'ProveedorRenovacion') IS NULL
    ALTER TABLE dbo.Suscripcion ADD ProveedorRenovacion NVARCHAR(100) NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarActualPorAgencia
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        S.ID, S.IdAgencia, S.IdPlanComercial, S.ReferenciaExterna, S.Estado, S.FechaAlta, S.FechaVencimiento,
        S.FechaProximaRenovacion, S.RenovacionAutomatica, S.ImporteVigente, S.Activo, S.FechaBaja,
        S.ReferenciaRenovacionProveedor, S.EstadoRenovacionProveedor, S.ProveedorRenovacion, S.FechaSincronizacionRenovacion,
        P.Nombre AS NombrePlan, P.Periodicidad AS PeriodicidadPlan, P.Moneda
    FROM dbo.Suscripcion S
    INNER JOIN dbo.PlanComercial P ON P.ID = S.IdPlanComercial
    WHERE S.IdAgencia = @IdAgencia
    ORDER BY S.Activo DESC, S.FechaAlta DESC, S.ID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_RegistrarRenovacionProveedor
    @IdSuscripcion INT,
    @ReferenciaProveedor NVARCHAR(250),
    @EstadoProveedor NVARCHAR(80),
    @Proveedor NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Suscripcion
    SET RenovacionAutomatica = 1,
        ReferenciaRenovacionProveedor = @ReferenciaProveedor,
        EstadoRenovacionProveedor = @EstadoProveedor,
        ProveedorRenovacion = @Proveedor,
        FechaSincronizacionRenovacion = SYSDATETIME()
    WHERE ID = @IdSuscripcion AND Activo = 1;
    IF @@ROWCOUNT <> 1 THROW 53030, 'No existe una suscripcion activa para registrar la autorizacion recurrente.', 1;
    SELECT @IdSuscripcion AS IdSuscripcion, @ReferenciaProveedor AS ReferenciaProveedor, @EstadoProveedor AS EstadoProveedor, @Proveedor AS Proveedor;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_ConsultarRenovacionesProveedor
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID AS IdSuscripcion, IdAgencia, IdPlanComercial, ReferenciaRenovacionProveedor, EstadoRenovacionProveedor,
           COALESCE(ProveedorRenovacion, N'MercadoPago') AS ProveedorRenovacion
    FROM dbo.Suscripcion
    WHERE RenovacionAutomatica = 1 AND ReferenciaRenovacionProveedor IS NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Suscripcion_AplicarCobroRecurrente
    @IdSuscripcion INT,
    @ReferenciaPago NVARCHAR(250),
    @Importe DECIMAL(18,2),
    @Moneda NVARCHAR(20),
    @Detalle NVARCHAR(1000),
    @Proveedor NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM dbo.OperacionSuscripcion WHERE IdSuscripcion = @IdSuscripcion AND TipoOperacion = N'Renovación automática' AND ReferenciaProveedor = @ReferenciaPago)
        RETURN;

    DECLARE @IdPlan INT, @Duracion INT, @Vencimiento DATETIME2, @Base DATETIME2, @Nuevo DATETIME2;
    SELECT @IdPlan = IdPlanComercial, @Vencimiento = FechaVencimiento FROM dbo.Suscripcion WHERE ID = @IdSuscripcion AND RenovacionAutomatica = 1;
    IF @IdPlan IS NULL THROW 53031, 'La suscripcion no esta habilitada para renovar.', 1;
    SELECT @Duracion = DuracionMeses FROM dbo.PlanComercial WHERE ID = @IdPlan;
    SET @Base = CASE WHEN @Vencimiento > SYSDATETIME() THEN @Vencimiento ELSE SYSDATETIME() END;
    SET @Nuevo = DATEADD(MONTH, @Duracion, @Base);
    UPDATE dbo.Suscripcion
    SET FechaVencimiento = @Nuevo,
        FechaProximaRenovacion = @Nuevo,
        Estado = N'Activa',
        Activo = 1,
        FechaBaja = NULL,
        FechaSincronizacionRenovacion = SYSDATETIME()
    WHERE ID = @IdSuscripcion;
    INSERT INTO dbo.OperacionSuscripcion(IdSuscripcion, IdPlanAnterior, IdPlanNuevo, TipoOperacion, ReferenciaProveedor, Proveedor, Importe, Moneda, Estado, Detalle)
    VALUES(@IdSuscripcion, @IdPlan, @IdPlan, N'Renovación automática', @ReferenciaPago, @Proveedor, @Importe, @Moneda, N'Aprobada', @Detalle);
END;
GO
