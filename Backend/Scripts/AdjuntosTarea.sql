IF OBJECT_ID(N'dbo.ArchivoAdjuntoTarea',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArchivoAdjuntoTarea
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArchivoAdjuntoTarea PRIMARY KEY,
        IdTarea INT NOT NULL,
        IdUsuarioAlta INT NOT NULL,
        NombreOriginal NVARCHAR(260) NOT NULL,
        ClaveAlmacenamiento NVARCHAR(500) NOT NULL,
        TipoContenido NVARCHAR(150) NOT NULL,
        TamanoBytes BIGINT NOT NULL,
        FechaAlta DATETIME2 NOT NULL CONSTRAINT DF_ArchivoAdjuntoTarea_FechaAlta DEFAULT SYSDATETIME(),
        Archivado BIT NOT NULL CONSTRAINT DF_ArchivoAdjuntoTarea_Archivado DEFAULT 0,
        FechaArchivado DATETIME2 NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ArchivoAdjuntoTarea_Activo DEFAULT 1,
        FechaBaja DATETIME2 NULL,
        CONSTRAINT FK_ArchivoAdjuntoTarea_Tarea FOREIGN KEY(IdTarea) REFERENCES dbo.Tarea(ID),
        CONSTRAINT FK_ArchivoAdjuntoTarea_Usuario FOREIGN KEY(IdUsuarioAlta) REFERENCES dbo.Usuario(ID),
        CONSTRAINT CK_ArchivoAdjuntoTarea_Tamano CHECK(TamanoBytes > 0 AND TamanoBytes <= 26214400)
    );
    CREATE UNIQUE INDEX UX_ArchivoAdjuntoTarea_Clave ON dbo.ArchivoAdjuntoTarea(ClaveAlmacenamiento);
    CREATE INDEX IX_ArchivoAdjuntoTarea_Tarea ON dbo.ArchivoAdjuntoTarea(IdTarea,Activo,FechaAlta DESC);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdjuntoTarea_Registrar
    @IdTarea INT,@IdAgencia INT,@IdUsuario INT,@NombreOriginal NVARCHAR(260),
    @ClaveAlmacenamiento NVARCHAR(500),@TipoContenido NVARCHAR(150),@TamanoBytes BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF @TamanoBytes<=0 OR @TamanoBytes>26214400 THROW 51200, 'Cada archivo debe pesar como máximo 25 MB.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE T.ID=@IdTarea AND P.IdAgencia=@IdAgencia AND T.Activo=1)
        THROW 51201, 'La tarea no existe o no pertenece a la agencia.', 1;

    DECLARE @Plan NVARCHAR(80)=COALESCE((SELECT TOP(1) PC.Nombre FROM dbo.Suscripcion S INNER JOIN dbo.PlanComercial PC ON PC.ID=S.IdPlanComercial WHERE S.IdAgencia=@IdAgencia AND S.Activo=1 ORDER BY S.FechaAlta DESC),N'Entry');
    DECLARE @MaxPorTarea INT=CASE @Plan WHEN N'Corporate' THEN 20 WHEN N'Business' THEN 10 ELSE 5 END;
    DECLARE @Cuota BIGINT=CASE @Plan WHEN N'Corporate' THEN 21474836480 WHEN N'Business' THEN 5368709120 ELSE 1073741824 END;
    IF (SELECT COUNT(1) FROM dbo.ArchivoAdjuntoTarea WHERE IdTarea=@IdTarea AND Activo=1)>=@MaxPorTarea
        THROW 51202, 'La tarea alcanzó el máximo de adjuntos permitido por el plan.', 1;
    IF ISNULL((SELECT SUM(A.TamanoBytes) FROM dbo.ArchivoAdjuntoTarea A INNER JOIN dbo.Tarea T ON T.ID=A.IdTarea INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE P.IdAgencia=@IdAgencia AND A.Activo=1),0)+@TamanoBytes>@Cuota
        THROW 51203, 'La agencia alcanzó la cuota de almacenamiento de su plan.', 1;

    INSERT INTO dbo.ArchivoAdjuntoTarea(IdTarea,IdUsuarioAlta,NombreOriginal,ClaveAlmacenamiento,TipoContenido,TamanoBytes)
    VALUES(@IdTarea,@IdUsuario,@NombreOriginal,@ClaveAlmacenamiento,@TipoContenido,@TamanoBytes);
    SELECT * FROM dbo.ArchivoAdjuntoTarea WHERE ID=SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdjuntoTarea_Consultar @IdTarea INT,@IdAgencia INT AS
BEGIN
    SELECT A.* FROM dbo.ArchivoAdjuntoTarea A INNER JOIN dbo.Tarea T ON T.ID=A.IdTarea INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto
    WHERE A.IdTarea=@IdTarea AND P.IdAgencia=@IdAgencia AND A.Activo=1 ORDER BY A.FechaAlta,A.ID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdjuntoTarea_ConsultarDetalle @ID INT,@IdTarea INT,@IdAgencia INT AS
BEGIN
    SELECT A.* FROM dbo.ArchivoAdjuntoTarea A INNER JOIN dbo.Tarea T ON T.ID=A.IdTarea INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto
    WHERE A.ID=@ID AND A.IdTarea=@IdTarea AND P.IdAgencia=@IdAgencia AND A.Activo=1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdjuntoTarea_Desactivar @ID INT,@IdTarea INT,@IdAgencia INT AS
BEGIN
    UPDATE A SET Activo=0,FechaBaja=SYSDATETIME() FROM dbo.ArchivoAdjuntoTarea A INNER JOIN dbo.Tarea T ON T.ID=A.IdTarea INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto
    WHERE A.ID=@ID AND A.IdTarea=@IdTarea AND P.IdAgencia=@IdAgencia AND A.Activo=1;
    IF @@ROWCOUNT=0 THROW 51204, 'El adjunto no existe o ya fue eliminado.', 1;
END
GO
