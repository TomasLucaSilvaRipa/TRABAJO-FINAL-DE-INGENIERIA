/*
  Gestión y catálogo público de planes comerciales TeamBalance.
  Ejecutar sobre la base TeamBalance.
*/
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlanComercial_Consultar
    @SoloActivos BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID, Nombre, Descripcion, Periodicidad, PrecioVigente, Moneda, DuracionMeses, AlcanceFuncional, CondicionesRenovacion, Activo, FechaVigenciaDesde, FechaVigenciaHasta
    FROM dbo.PlanComercial
    WHERE @SoloActivos = 0 OR Activo = 1
    ORDER BY PrecioVigente, ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlanComercial_ConsultarPorId
    @ID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID, Nombre, Descripcion, Periodicidad, PrecioVigente, Moneda, DuracionMeses, AlcanceFuncional, CondicionesRenovacion, Activo, FechaVigenciaDesde, FechaVigenciaHasta
    FROM dbo.PlanComercial
    WHERE ID = @ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlanComercial_Registrar
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(500),
    @Periodicidad NVARCHAR(50),
    @PrecioVigente DECIMAL(18, 2),
    @Moneda NVARCHAR(10),
    @DuracionMeses INT,
    @AlcanceFuncional NVARCHAR(MAX),
    @CondicionesRenovacion NVARCHAR(500),
    @Activo BIT,
    @FechaVigenciaDesde DATETIME2,
    @FechaVigenciaHasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.PlanComercial WHERE Nombre = @Nombre AND Periodicidad = @Periodicidad)
        THROW 50011, 'Ya existe un plan con el mismo nombre y periodicidad.', 1;

    INSERT INTO dbo.PlanComercial (Nombre, Descripcion, Periodicidad, PrecioVigente, Moneda, DuracionMeses, AlcanceFuncional, CondicionesRenovacion, Activo, FechaVigenciaDesde, FechaVigenciaHasta)
    VALUES (@Nombre, NULLIF(@Descripcion, N''), @Periodicidad, @PrecioVigente, @Moneda, @DuracionMeses, NULLIF(@AlcanceFuncional, N''), NULLIF(@CondicionesRenovacion, N''), @Activo, @FechaVigenciaDesde, @FechaVigenciaHasta);

    DECLARE @IDNuevo INT = CONVERT(INT, SCOPE_IDENTITY());
    EXEC dbo.usp_PlanComercial_ConsultarPorId @ID = @IDNuevo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlanComercial_Modificar
    @ID INT,
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(500),
    @Periodicidad NVARCHAR(50),
    @PrecioVigente DECIMAL(18, 2),
    @Moneda NVARCHAR(10),
    @DuracionMeses INT,
    @AlcanceFuncional NVARCHAR(MAX),
    @CondicionesRenovacion NVARCHAR(500),
    @Activo BIT,
    @FechaVigenciaDesde DATETIME2,
    @FechaVigenciaHasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PlanComercial
    SET Nombre = @Nombre,
        Descripcion = NULLIF(@Descripcion, N''),
        Periodicidad = @Periodicidad,
        PrecioVigente = @PrecioVigente,
        Moneda = @Moneda,
        DuracionMeses = @DuracionMeses,
        AlcanceFuncional = NULLIF(@AlcanceFuncional, N''),
        CondicionesRenovacion = NULLIF(@CondicionesRenovacion, N''),
        Activo = @Activo,
        FechaVigenciaDesde = @FechaVigenciaDesde,
        FechaVigenciaHasta = @FechaVigenciaHasta
    WHERE ID = @ID;

    IF @@ROWCOUNT = 0
        THROW 50012, 'No existe el plan comercial indicado.', 1;

    EXEC dbo.usp_PlanComercial_ConsultarPorId @ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlanComercial_CambiarEstado
    @ID INT,
    @Activo BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PlanComercial
    SET Activo = @Activo,
        FechaVigenciaHasta = CASE WHEN @Activo = 0 THEN SYSDATETIME() ELSE NULL END
    WHERE ID = @ID;

    IF @@ROWCOUNT = 0
        THROW 50013, 'No existe el plan comercial indicado.', 1;

    EXEC dbo.usp_PlanComercial_ConsultarPorId @ID;
END;
GO

UPDATE dbo.PlanComercial
SET Activo = 0,
    FechaVigenciaHasta = COALESCE(FechaVigenciaHasta, SYSDATETIME())
WHERE Nombre NOT IN (N'Entry', N'Business', N'Corporate');
GO

IF EXISTS (SELECT 1 FROM dbo.PlanComercial WHERE Nombre = N'Entry')
BEGIN
    UPDATE dbo.PlanComercial SET Descripcion = N'Para agencias de hasta 20 usuarios.', Periodicidad = N'Mensual', PrecioVigente = 99, Moneda = N'USD', DuracionMeses = 1, AlcanceFuncional = N'projects-resources;best-fit;dashboards;alerts;standard-reports;documentation;standard-support;updates;onboarding', CondicionesRenovacion = N'Renovación mensual.', Activo = 1, FechaVigenciaDesde = SYSDATETIME(), FechaVigenciaHasta = NULL WHERE Nombre = N'Entry';
END
ELSE
BEGIN
    INSERT INTO dbo.PlanComercial (Nombre, Descripcion, Periodicidad, PrecioVigente, Moneda, DuracionMeses, AlcanceFuncional, CondicionesRenovacion, Activo, FechaVigenciaDesde) VALUES (N'Entry', N'Para agencias de hasta 20 usuarios.', N'Mensual', 99, N'USD', 1, N'projects-resources;best-fit;dashboards;alerts;standard-reports;documentation;standard-support;updates;onboarding', N'Renovación mensual.', 1, SYSDATETIME());
END;
GO

IF EXISTS (SELECT 1 FROM dbo.PlanComercial WHERE Nombre = N'Business')
BEGIN
    UPDATE dbo.PlanComercial SET Descripcion = N'Para agencias de 21 a 50 usuarios.', Periodicidad = N'Mensual', PrecioVigente = 249, Moneda = N'USD', DuracionMeses = 1, AlcanceFuncional = N'projects-resources;best-fit;dashboards;alerts;standard-reports;documentation;standard-support;updates;onboarding;extended-reports;capacity-tracking;priority-support;adoption-support', CondicionesRenovacion = N'Renovación mensual.', Activo = 1, FechaVigenciaDesde = SYSDATETIME(), FechaVigenciaHasta = NULL WHERE Nombre = N'Business';
END
ELSE
BEGIN
    INSERT INTO dbo.PlanComercial (Nombre, Descripcion, Periodicidad, PrecioVigente, Moneda, DuracionMeses, AlcanceFuncional, CondicionesRenovacion, Activo, FechaVigenciaDesde) VALUES (N'Business', N'Para agencias de 21 a 50 usuarios.', N'Mensual', 249, N'USD', 1, N'projects-resources;best-fit;dashboards;alerts;standard-reports;documentation;standard-support;updates;onboarding;extended-reports;capacity-tracking;priority-support;adoption-support', N'Renovación mensual.', 1, SYSDATETIME());
END;
GO

IF EXISTS (SELECT 1 FROM dbo.PlanComercial WHERE Nombre = N'Corporate')
BEGIN
    UPDATE dbo.PlanComercial SET Descripcion = N'Para agencias de 51 a 200 usuarios.', Periodicidad = N'Mensual', PrecioVigente = 499, Moneda = N'USD', DuracionMeses = 1, AlcanceFuncional = N'projects-resources;best-fit;dashboards;alerts;standard-reports;documentation;standard-support;updates;onboarding;extended-reports;capacity-tracking;priority-support;adoption-support;advanced-analytics;reinforced-support;commercial-follow-up', CondicionesRenovacion = N'Renovación mensual.', Activo = 1, FechaVigenciaDesde = SYSDATETIME(), FechaVigenciaHasta = NULL WHERE Nombre = N'Corporate';
END
ELSE
BEGIN
    INSERT INTO dbo.PlanComercial (Nombre, Descripcion, Periodicidad, PrecioVigente, Moneda, DuracionMeses, AlcanceFuncional, CondicionesRenovacion, Activo, FechaVigenciaDesde) VALUES (N'Corporate', N'Para agencias de 51 a 200 usuarios.', N'Mensual', 499, N'USD', 1, N'projects-resources;best-fit;dashboards;alerts;standard-reports;documentation;standard-support;updates;onboarding;extended-reports;capacity-tracking;priority-support;adoption-support;advanced-analytics;reinforced-support;commercial-follow-up', N'Renovación mensual.', 1, SYSDATETIME());
END;
GO
