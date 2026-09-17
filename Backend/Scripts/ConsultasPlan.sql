IF OBJECT_ID(N'dbo.ConsultaPlan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultaPlan
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsultaPlan PRIMARY KEY,
        IdPlanComercial INT NOT NULL,
        Nombre NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        Consulta NVARCHAR(1000) NOT NULL,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_ConsultaPlan_FechaAlta DEFAULT SYSDATETIME(),
        Activo BIT NOT NULL CONSTRAINT DF_ConsultaPlan_Activo DEFAULT 1,
        CONSTRAINT FK_ConsultaPlan_PlanComercial FOREIGN KEY (IdPlanComercial) REFERENCES dbo.PlanComercial(ID)
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConsultaPlan_ConsultarPublicas @IdPlanComercial INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, IdPlanComercial, Nombre, Email, Consulta, FechaAlta, Activo
    FROM dbo.ConsultaPlan
    WHERE IdPlanComercial = @IdPlanComercial AND Activo = 1
    ORDER BY FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConsultaPlan_Registrar @IdPlanComercial INT, @Nombre NVARCHAR(100), @Email NVARCHAR(150), @Consulta NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ConsultaPlan(IdPlanComercial, Nombre, Email, Consulta, Activo) VALUES(@IdPlanComercial, @Nombre, @Email, @Consulta, 1);
    DECLARE @ID INT = CONVERT(INT, SCOPE_IDENTITY());
    SELECT ID, IdPlanComercial, Nombre, Email, Consulta, FechaAlta, Activo FROM dbo.ConsultaPlan WHERE ID = @ID;
END;
GO
