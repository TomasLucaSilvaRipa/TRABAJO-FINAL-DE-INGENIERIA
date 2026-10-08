USE [TeamBalance]
GO

IF OBJECT_ID(N'dbo.AreaSkill', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AreaSkill
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AreaSkill PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL CONSTRAINT UQ_AreaSkill_Nombre UNIQUE,
        Activo BIT NOT NULL CONSTRAINT DF_AreaSkill_Activo DEFAULT 1
    );
END
GO

MERGE dbo.AreaSkill AS destino
USING (VALUES
    (N'Programación'), (N'Diseño'), (N'Marketing y contenido'), (N'Audiovisual'),
    (N'Gestión de proyectos'), (N'Datos y analítica'), (N'Operaciones'), (N'Recursos humanos'), (N'General')
) AS origen(Nombre) ON destino.Nombre = origen.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre, Activo) VALUES (origen.Nombre, 1);
GO

IF COL_LENGTH(N'dbo.Skill', N'IdAreaSkill') IS NULL ALTER TABLE dbo.Skill ADD IdAreaSkill INT NULL;
GO

INSERT INTO dbo.AreaSkill(Nombre, Activo)
SELECT DISTINCT LTRIM(RTRIM(S.Categoria)), 1
FROM dbo.Skill S
WHERE NULLIF(LTRIM(RTRIM(S.Categoria)), N'') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.AreaSkill A WHERE A.Nombre = LTRIM(RTRIM(S.Categoria)));
GO

UPDATE S SET IdAreaSkill = A.ID
FROM dbo.Skill S
INNER JOIN dbo.AreaSkill A ON A.Nombre = LTRIM(RTRIM(S.Categoria))
WHERE S.IdAreaSkill IS NULL AND NULLIF(LTRIM(RTRIM(S.Categoria)), N'') IS NOT NULL;
GO

UPDATE S SET IdAreaSkill = A.ID, Categoria = A.Nombre
FROM dbo.Skill S
INNER JOIN dbo.AreaSkill A ON A.Nombre = N'General'
WHERE S.IdAreaSkill IS NULL;
GO

DECLARE @IdProgramacion INT = (SELECT ID FROM dbo.AreaSkill WHERE Nombre = N'Programación');
IF @IdProgramacion IS NOT NULL
BEGIN
    UPDATE S SET IdAreaSkill = @IdProgramacion, Categoria = N'Programación'
    FROM dbo.Skill S
    WHERE LOWER(COALESCE(S.Categoria, N'')) COLLATE Latin1_General_100_CI_AI = N'programacion';

    UPDATE dbo.AreaSkill SET Activo = 0
    WHERE ID <> @IdProgramacion AND LOWER(Nombre) COLLATE Latin1_General_100_CI_AI = N'programacion';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Skill_AreaSkill')
    ALTER TABLE dbo.Skill ADD CONSTRAINT FK_Skill_AreaSkill FOREIGN KEY (IdAreaSkill) REFERENCES dbo.AreaSkill(ID);
GO

CREATE OR ALTER PROCEDURE dbo.usp_AreaSkill_Consultar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Nombre, Activo FROM dbo.AreaSkill ORDER BY Activo DESC, Nombre;
END
GO
