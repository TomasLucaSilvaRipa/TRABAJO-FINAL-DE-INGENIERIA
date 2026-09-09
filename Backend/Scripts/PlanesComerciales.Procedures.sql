/*
  Solo infraestructura de gestión de planes.
  No inserta, actualiza ni da de baja planes existentes.
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
    @Nombre NVARCHAR(100), @Descripcion NVARCHAR(500), @Periodicidad NVARCHAR(50), @PrecioVigente DECIMAL(18, 2), @Moneda NVARCHAR(10), @DuracionMeses INT,
    @AlcanceFuncional NVARCHAR(MAX), @CondicionesRenovacion NVARCHAR(500), @Activo BIT, @FechaVigenciaDesde DATETIME2, @FechaVigenciaHasta DATETIME2
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
    @ID INT, @Nombre NVARCHAR(100), @Descripcion NVARCHAR(500), @Periodicidad NVARCHAR(50), @PrecioVigente DECIMAL(18, 2), @Moneda NVARCHAR(10), @DuracionMeses INT,
    @AlcanceFuncional NVARCHAR(MAX), @CondicionesRenovacion NVARCHAR(500), @Activo BIT, @FechaVigenciaDesde DATETIME2, @FechaVigenciaHasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PlanComercial
    SET Nombre = @Nombre, Descripcion = NULLIF(@Descripcion, N''), Periodicidad = @Periodicidad, PrecioVigente = @PrecioVigente, Moneda = @Moneda,
        DuracionMeses = @DuracionMeses, AlcanceFuncional = NULLIF(@AlcanceFuncional, N''), CondicionesRenovacion = NULLIF(@CondicionesRenovacion, N''),
        Activo = @Activo, FechaVigenciaDesde = @FechaVigenciaDesde, FechaVigenciaHasta = @FechaVigenciaHasta
    WHERE ID = @ID;

    IF @@ROWCOUNT = 0
        THROW 50012, 'No existe el plan comercial indicado.', 1;

    EXEC dbo.usp_PlanComercial_ConsultarPorId @ID = @ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlanComercial_CambiarEstado
    @ID INT, @Activo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PlanComercial
    SET Activo = @Activo, FechaVigenciaHasta = CASE WHEN @Activo = 0 THEN SYSDATETIME() ELSE NULL END
    WHERE ID = @ID;

    IF @@ROWCOUNT = 0
        THROW 50013, 'No existe el plan comercial indicado.', 1;

    EXEC dbo.usp_PlanComercial_ConsultarPorId @ID = @ID;
END;
GO
