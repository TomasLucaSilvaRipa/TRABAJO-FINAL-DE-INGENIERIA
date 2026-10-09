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

IF COL_LENGTH(N'dbo.ConsultaPlan', N'Estado') IS NULL ALTER TABLE dbo.ConsultaPlan ADD Estado NVARCHAR(50) NOT NULL CONSTRAINT DF_ConsultaPlan_Estado DEFAULT N'Pendiente';
IF COL_LENGTH(N'dbo.ConsultaPlan', N'Respuesta') IS NULL ALTER TABLE dbo.ConsultaPlan ADD Respuesta NVARCHAR(4000) NULL;
IF COL_LENGTH(N'dbo.ConsultaPlan', N'FechaRespuesta') IS NULL ALTER TABLE dbo.ConsultaPlan ADD FechaRespuesta DATETIME2 NULL;
IF COL_LENGTH(N'dbo.ConsultaPlan', N'IdUsuarioSoporte') IS NULL ALTER TABLE dbo.ConsultaPlan ADD IdUsuarioSoporte INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConsultaPlan_UsuarioSoporte') ALTER TABLE dbo.ConsultaPlan ADD CONSTRAINT FK_ConsultaPlan_UsuarioSoporte FOREIGN KEY (IdUsuarioSoporte) REFERENCES dbo.Usuario(ID);
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConsultaPlan_ConsultarPublicas @IdPlanComercial INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT C.ID, C.IdPlanComercial, C.Nombre, C.Email, C.Consulta, C.FechaAlta, C.Activo, C.Estado, C.Respuesta, C.FechaRespuesta, C.IdUsuarioSoporte, P.Nombre NombrePlan, CONCAT(U.Nombre, N' ', U.Apellido) NombreRespondedor
    FROM dbo.ConsultaPlan C
    INNER JOIN dbo.PlanComercial P ON P.ID = C.IdPlanComercial
    LEFT JOIN dbo.Usuario U ON U.ID = C.IdUsuarioSoporte
    WHERE C.IdPlanComercial = @IdPlanComercial AND C.Activo = 1
    ORDER BY C.FechaAlta DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConsultaPlan_Registrar @IdPlanComercial INT, @Nombre NVARCHAR(100), @Email NVARCHAR(150), @Consulta NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ConsultaPlan(IdPlanComercial, Nombre, Email, Consulta, Estado, Activo) VALUES(@IdPlanComercial, @Nombre, @Email, @Consulta, N'Pendiente', 1);
    DECLARE @ID INT = CONVERT(INT, SCOPE_IDENTITY());
    SELECT C.ID, C.IdPlanComercial, C.Nombre, C.Email, C.Consulta, C.FechaAlta, C.Activo, C.Estado, C.Respuesta, C.FechaRespuesta, C.IdUsuarioSoporte, P.Nombre NombrePlan, CONCAT(U.Nombre, N' ', U.Apellido) NombreRespondedor FROM dbo.ConsultaPlan C INNER JOIN dbo.PlanComercial P ON P.ID = C.IdPlanComercial LEFT JOIN dbo.Usuario U ON U.ID = C.IdUsuarioSoporte WHERE C.ID = @ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConsultaPlan_ConsultarBandeja @Estado NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT C.ID, C.IdPlanComercial, C.Nombre, C.Email, C.Consulta, C.FechaAlta, C.Activo, C.Estado, C.Respuesta, C.FechaRespuesta, C.IdUsuarioSoporte, P.Nombre NombrePlan, CONCAT(U.Nombre, N' ', U.Apellido) NombreRespondedor
    FROM dbo.ConsultaPlan C
    INNER JOIN dbo.PlanComercial P ON P.ID = C.IdPlanComercial
    LEFT JOIN dbo.Usuario U ON U.ID = C.IdUsuarioSoporte
    WHERE C.Activo = 1 AND (@Estado IS NULL OR C.Estado = @Estado)
    ORDER BY CASE C.Estado WHEN N'Pendiente' THEN 1 WHEN N'En revisión' THEN 2 WHEN N'Respondida' THEN 3 ELSE 4 END, ISNULL(C.FechaRespuesta, C.FechaAlta) DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConsultaPlan_Responder @IdConsultaPlan INT, @IdUsuarioSoporte INT, @Respuesta NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ConsultaPlan SET Respuesta = @Respuesta, Estado = N'Respondida', FechaRespuesta = SYSDATETIME(), IdUsuarioSoporte = @IdUsuarioSoporte WHERE ID = @IdConsultaPlan AND Activo = 1;
    SELECT C.ID, C.IdPlanComercial, C.Nombre, C.Email, C.Consulta, C.FechaAlta, C.Activo, C.Estado, C.Respuesta, C.FechaRespuesta, C.IdUsuarioSoporte, P.Nombre NombrePlan, CONCAT(U.Nombre, N' ', U.Apellido) NombreRespondedor FROM dbo.ConsultaPlan C INNER JOIN dbo.PlanComercial P ON P.ID = C.IdPlanComercial LEFT JOIN dbo.Usuario U ON U.ID = C.IdUsuarioSoporte WHERE C.ID = @IdConsultaPlan AND C.Activo = 1;
END;
GO
