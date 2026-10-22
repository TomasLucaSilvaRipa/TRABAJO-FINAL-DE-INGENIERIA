/* Idiomas administrables por Soporte. Las traducciones parciales se almacenan como JSON. */
GO
IF OBJECT_ID(N'dbo.Idioma', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Idioma
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Idioma PRIMARY KEY,
        Codigo NVARCHAR(16) NOT NULL CONSTRAINT UQ_Idioma_Codigo UNIQUE,
        Nombre NVARCHAR(100) NOT NULL,
        TraduccionesJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Idioma_Traducciones DEFAULT N'{}',
        Activo BIT NOT NULL CONSTRAINT DF_Idioma_Activo DEFAULT 1,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_Idioma_FechaAlta DEFAULT SYSUTCDATETIME(),
        FechaBaja DATETIME2 NULL,
        CONSTRAINT CK_Idioma_TraduccionesJson CHECK (ISJSON(TraduccionesJson) = 1)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE Codigo = N'es')
    INSERT INTO dbo.Idioma(Codigo, Nombre, TraduccionesJson, Activo) VALUES(N'es', N'Espa' + NCHAR(241) + N'ol', N'{}', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE Codigo = N'en')
    INSERT INTO dbo.Idioma(Codigo, Nombre, TraduccionesJson, Activo) VALUES(N'en', N'English', N'{}', 1);
UPDATE dbo.Idioma SET Nombre = N'Espa' + NCHAR(241) + N'ol' WHERE Codigo = N'es';
GO
CREATE OR ALTER PROCEDURE dbo.usp_Idioma_ConsultarActivos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Codigo, Nombre, TraduccionesJson, Activo FROM dbo.Idioma WHERE Activo = 1 ORDER BY Nombre;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Idioma_ConsultarGestion
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Codigo, Nombre, TraduccionesJson, Activo FROM dbo.Idioma ORDER BY Activo DESC, Nombre;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Idioma_Registrar
    @Codigo NVARCHAR(16), @Nombre NVARCHAR(100), @Activo BIT, @TraduccionesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Idioma(Codigo, Nombre, Activo, TraduccionesJson) VALUES(@Codigo, @Nombre, @Activo, @TraduccionesJson);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ID;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Idioma_Modificar
    @IdIdioma INT, @Codigo NVARCHAR(16), @Nombre NVARCHAR(100), @Activo BIT, @TraduccionesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Idioma
    SET Codigo = @Codigo, Nombre = @Nombre, Activo = @Activo,
        TraduccionesJson = @TraduccionesJson, FechaBaja = CASE WHEN @Activo = 0 THEN SYSUTCDATETIME() ELSE NULL END
    WHERE ID = @IdIdioma;
    IF @@ROWCOUNT = 0 THROW 50001, 'No existe el idioma indicado.', 1;
END;
GO
