IF OBJECT_ID(N'dbo.OpinionServicio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpinionServicio
    (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdUsuario INT NOT NULL,
        IdAgencia INT NULL,
        Calificacion INT NOT NULL CHECK (Calificacion BETWEEN 1 AND 5),
        Titulo NVARCHAR(100) NOT NULL,
        Comentario NVARCHAR(1000) NOT NULL,
        FechaAlta DATETIME NOT NULL CONSTRAINT DF_OpinionServicio_FechaAlta DEFAULT GETDATE(),
        CONSTRAINT UQ_OpinionServicio_Usuario UNIQUE (IdUsuario),
        CONSTRAINT FK_OpinionServicio_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(ID),
        CONSTRAINT FK_OpinionServicio_Agencia FOREIGN KEY (IdAgencia) REFERENCES dbo.Agencia(ID)
    );
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_OpinionServicio_Guardar @IdUsuario INT, @IdAgencia INT = NULL, @Calificacion INT, @Titulo NVARCHAR(100), @Comentario NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.OpinionServicio WHERE IdUsuario = @IdUsuario)
        UPDATE dbo.OpinionServicio SET Calificacion = @Calificacion, Titulo = @Titulo, Comentario = @Comentario, FechaAlta = GETDATE() WHERE IdUsuario = @IdUsuario;
    ELSE
        INSERT INTO dbo.OpinionServicio (IdUsuario, IdAgencia, Calificacion, Titulo, Comentario) VALUES (@IdUsuario, @IdAgencia, @Calificacion, @Titulo, @Comentario);
    EXEC dbo.usp_OpinionServicio_ConsultarPorUsuario @IdUsuario;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_OpinionServicio_ConsultarPorUsuario @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT O.ID, O.IdUsuario, O.IdAgencia, O.Calificacion, O.Titulo, O.Comentario, O.FechaAlta, CONCAT(U.Nombre, ' ', U.Apellido) AS NombreUsuario, ISNULL(R.Nombre, 'Usuario TeamBalance') AS NombreRol
    FROM dbo.OpinionServicio O INNER JOIN dbo.Usuario U ON U.ID = O.IdUsuario LEFT JOIN dbo.Rol R ON R.ID = U.IdRol WHERE O.IdUsuario = @IdUsuario;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_OpinionServicio_ConsultarPublicas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT O.ID, O.IdUsuario, O.IdAgencia, O.Calificacion, O.Titulo, O.Comentario, O.FechaAlta, CONCAT(U.Nombre, ' ', U.Apellido) AS NombreUsuario, ISNULL(R.Nombre, 'Usuario TeamBalance') AS NombreRol
    FROM dbo.OpinionServicio O INNER JOIN dbo.Usuario U ON U.ID = O.IdUsuario LEFT JOIN dbo.Rol R ON R.ID = U.IdRol ORDER BY O.FechaAlta DESC;
END
GO
