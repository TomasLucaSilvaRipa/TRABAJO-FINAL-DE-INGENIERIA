IF COL_LENGTH('dbo.PlantillaTarea', 'MotivoBaja') IS NULL
BEGIN
    ALTER TABLE dbo.PlantillaTarea ADD MotivoBaja NVARCHAR(500) NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlantillaTarea_Consultar
    @IdAgencia INT,
    @IncluirInactivas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.PlantillaTarea
    WHERE IdAgencia = @IdAgencia
        AND (@IncluirInactivas = 1 OR Activo = 1)
    ORDER BY Activo DESC, Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlantillaTarea_Registrar
    @IdAgencia INT,
    @IdSkillRequerido INT,
    @Nombre NVARCHAR(150),
    @TituloSugerido NVARCHAR(150),
    @DescripcionBase NVARCHAR(1000) = NULL,
    @HorasEstimadas DECIMAL(10,2),
    @Complejidad NVARCHAR(50) = NULL,
    @PrioridadSugerida NVARCHAR(50) = NULL,
    @SkillRequerido NVARCHAR(100),
    @SeniorityRecomendado NVARCHAR(50) = NULL,
    @ChecklistBaseJson NVARCHAR(MAX) = NULL,
    @ArchivosAdjuntosJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PlantillaTarea
    (
        IdAgencia, IdSkillRequerido, Nombre, TituloSugerido, DescripcionBase, HorasEstimadas,
        Complejidad, PrioridadSugerida, SkillRequerido, SeniorityRecomendado, Estado, Activo,
        FechaCreacion, ChecklistBaseJson, ArchivosAdjuntosJson
    )
    OUTPUT inserted.*
    VALUES
    (
        @IdAgencia, @IdSkillRequerido, @Nombre, @TituloSugerido, @DescripcionBase, @HorasEstimadas,
        @Complejidad, @PrioridadSugerida, @SkillRequerido, @SeniorityRecomendado, N'Activa', 1,
        SYSDATETIME(), @ChecklistBaseJson, @ArchivosAdjuntosJson
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlantillaTarea_Modificar
    @ID INT,
    @IdAgencia INT,
    @IdSkillRequerido INT,
    @Nombre NVARCHAR(150),
    @TituloSugerido NVARCHAR(150),
    @DescripcionBase NVARCHAR(1000) = NULL,
    @HorasEstimadas DECIMAL(10,2),
    @Complejidad NVARCHAR(50) = NULL,
    @PrioridadSugerida NVARCHAR(50) = NULL,
    @SkillRequerido NVARCHAR(100),
    @SeniorityRecomendado NVARCHAR(50) = NULL,
    @ChecklistBaseJson NVARCHAR(MAX) = NULL,
    @ArchivosAdjuntosJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PlantillaTarea
    SET IdSkillRequerido = @IdSkillRequerido,
        Nombre = @Nombre,
        TituloSugerido = @TituloSugerido,
        DescripcionBase = @DescripcionBase,
        HorasEstimadas = @HorasEstimadas,
        Complejidad = @Complejidad,
        PrioridadSugerida = @PrioridadSugerida,
        SkillRequerido = @SkillRequerido,
        SeniorityRecomendado = @SeniorityRecomendado,
        ChecklistBaseJson = @ChecklistBaseJson,
        ArchivosAdjuntosJson = @ArchivosAdjuntosJson
    OUTPUT inserted.*
    WHERE ID = @ID AND IdAgencia = @IdAgencia AND Activo = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51000, 'La plantilla no existe o no está disponible para modificar.', 1;
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PlantillaTarea_DarBaja
    @ID INT,
    @IdAgencia INT,
    @MotivoBaja NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PlantillaTarea
    SET Activo = 0,
        Estado = N'Inactiva',
        FechaBaja = SYSDATETIME(),
        MotivoBaja = @MotivoBaja
    WHERE ID = @ID AND IdAgencia = @IdAgencia AND Activo = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51000, 'La plantilla no existe o ya fue dada de baja.', 1;
    END;
END;
GO
