/* Estados intermedios configurables por agencia. Los cuatro estados base se
   conservan y los cálculos siguen reconociendo Finalizada como cierre real. */
CREATE OR ALTER PROCEDURE dbo.usp_EstadoTarea_RegistrarPersonalizado
    @IdAgencia INT,
    @Nombre NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Nombre = LTRIM(RTRIM(@Nombre));
    IF NULLIF(@Nombre, N'') IS NULL THROW 51100, 'Indicá un nombre para el estado.', 1;
    IF LEN(@Nombre) > 80 THROW 51101, 'El nombre del estado no puede superar los 80 caracteres.', 1;
    IF (SELECT COUNT(1) FROM dbo.EstadoTarea WHERE IdAgencia=@IdAgencia AND EsBase=0 AND Activo=1) >= 3
        THROW 51102, 'La agencia ya tiene tres estados personalizados activos.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EstadoTarea WHERE IdAgencia=@IdAgencia AND LOWER(Nombre)=LOWER(@Nombre) AND Activo=1)
        THROW 51103, 'Ya existe un estado activo con ese nombre.', 1;

    DECLARE @Orden INT = ISNULL((SELECT MAX(Orden) FROM dbo.EstadoTarea WHERE IdAgencia=@IdAgencia AND EsFinal=0), 0) + 1;
    DECLARE @OrdenFinal INT = (SELECT TOP (1) Orden FROM dbo.EstadoTarea WHERE IdAgencia=@IdAgencia AND EsFinal=1 AND Activo=1 ORDER BY Orden);
    IF @OrdenFinal IS NOT NULL AND @Orden >= @OrdenFinal
    BEGIN
        UPDATE dbo.EstadoTarea SET Orden = Orden + 1 WHERE IdAgencia=@IdAgencia AND Orden >= @OrdenFinal;
        SET @Orden = @OrdenFinal;
    END

    INSERT INTO dbo.EstadoTarea(IdAgencia,Nombre,Orden,EsBase,EsFinal,Activo)
    VALUES(@IdAgencia,@Nombre,@Orden,0,0,1);
    SELECT * FROM dbo.EstadoTarea WHERE ID=SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_EstadoTarea_ModificarPersonalizado
    @ID INT,
    @IdAgencia INT,
    @Nombre NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Nombre = LTRIM(RTRIM(@Nombre));
    IF NULLIF(@Nombre, N'') IS NULL THROW 51100, 'Indicá un nombre para el estado.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EstadoTarea WHERE ID=@ID AND IdAgencia=@IdAgencia AND EsBase=1)
        THROW 51104, 'Los estados base no se pueden modificar.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EstadoTarea WHERE IdAgencia=@IdAgencia AND ID<>@ID AND LOWER(Nombre)=LOWER(@Nombre) AND Activo=1)
        THROW 51103, 'Ya existe un estado activo con ese nombre.', 1;

    DECLARE @NombreAnterior NVARCHAR(80);
    SELECT @NombreAnterior=Nombre FROM dbo.EstadoTarea WHERE ID=@ID AND IdAgencia=@IdAgencia AND EsBase=0 AND Activo=1;
    IF @NombreAnterior IS NULL THROW 51105, 'El estado personalizado no existe.', 1;
    UPDATE dbo.EstadoTarea SET Nombre=@Nombre WHERE ID=@ID;
    UPDATE T SET Estado=@Nombre FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE P.IdAgencia=@IdAgencia AND T.IdEstadoTarea=@ID;
    SELECT * FROM dbo.EstadoTarea WHERE ID=@ID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_EstadoTarea_DesactivarPersonalizado
    @ID INT,
    @IdAgencia INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.EstadoTarea WHERE ID=@ID AND IdAgencia=@IdAgencia AND EsBase=1)
        THROW 51104, 'Los estados base no se pueden desactivar.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE T.IdEstadoTarea=@ID AND P.IdAgencia=@IdAgencia AND T.Activo=1)
        THROW 51106, 'No se puede desactivar el estado porque tiene tareas activas asociadas.', 1;
    UPDATE dbo.EstadoTarea SET Activo=0 WHERE ID=@ID AND IdAgencia=@IdAgencia AND EsBase=0;
    IF @@ROWCOUNT=0 THROW 51105, 'El estado personalizado no existe.', 1;
END
GO

/* Evita que IdEstadoTarea y el texto histórico Estado diverjan. */
CREATE OR ALTER TRIGGER dbo.trg_Tarea_SincronizarEstado
ON dbo.Tarea
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;
    UPDATE T SET Estado=ET.Nombre,
        FechaFinReal=CASE WHEN ET.EsFinal=1 THEN COALESCE(T.FechaFinReal,SYSDATETIME()) ELSE NULL END,
        Bloqueada=CASE WHEN ET.EsBase=1 AND ET.Nombre=N'Bloqueada' THEN 1 ELSE 0 END,
        MotivoBloqueo=CASE WHEN ET.EsBase=1 AND ET.Nombre=N'Bloqueada' THEN T.MotivoBloqueo ELSE NULL END
    FROM dbo.Tarea T
    INNER JOIN inserted I ON I.ID=T.ID
    INNER JOIN dbo.EstadoTarea ET ON ET.ID=T.IdEstadoTarea;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ActualizarAvanceEmpleado
    @ID INT,
    @IdUsuario INT,
    @IdEstadoTarea INT = NULL,
    @Estado NVARCHAR(80) = NULL,
    @ChecklistJson NVARCHAR(MAX)=NULL,
    @PorcentajeAvance DECIMAL(5,2),
    @MotivoBloqueo NVARCHAR(1000)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdAgencia INT, @IdEstadoResuelto INT, @NombreEstado NVARCHAR(80), @EsBloqueada BIT=0;
    SELECT @IdAgencia=P.IdAgencia FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto WHERE T.ID=@ID;
    SELECT TOP (1) @IdEstadoResuelto=ET.ID,@NombreEstado=ET.Nombre,
        @EsBloqueada=CASE WHEN ET.EsBase=1 AND ET.Nombre=N'Bloqueada' THEN 1 ELSE 0 END
    FROM dbo.EstadoTarea ET
    WHERE ET.IdAgencia=@IdAgencia AND ET.Activo=1
      AND ((@IdEstadoTarea IS NOT NULL AND ET.ID=@IdEstadoTarea) OR (@IdEstadoTarea IS NULL AND ET.Nombre=@Estado));
    IF @IdEstadoResuelto IS NULL THROW 51107, 'El estado seleccionado no pertenece a la agencia o está inactivo.', 1;
    IF @EsBloqueada=1 AND NULLIF(LTRIM(RTRIM(@MotivoBloqueo)),N'') IS NULL THROW 50021, 'Indicá el motivo del bloqueo.', 1;

    UPDATE T SET IdEstadoTarea=@IdEstadoResuelto, Estado=@NombreEstado, ChecklistJson=@ChecklistJson,
        PorcentajeAvance=@PorcentajeAvance, Bloqueada=@EsBloqueada,
        MotivoBloqueo=CASE WHEN @EsBloqueada=1 THEN LTRIM(RTRIM(@MotivoBloqueo)) ELSE NULL END
    FROM dbo.Tarea T
    WHERE T.ID=@ID AND T.Activo=1 AND (
        EXISTS (SELECT 1 FROM dbo.AsignacionTarea AT INNER JOIN dbo.Empleado E ON E.ID=AT.IdEmpleado WHERE AT.IdTarea=T.ID AND AT.Activa=1 AND E.IdUsuario=@IdUsuario)
        OR (NOT EXISTS (SELECT 1 FROM dbo.AsignacionTarea AT WHERE AT.IdTarea=T.ID AND AT.Activa=1) AND EXISTS (SELECT 1 FROM dbo.Empleado E WHERE E.ID=T.IdEmpleadoAsignado AND E.IdUsuario=@IdUsuario))
    );
    IF @@ROWCOUNT=0 THROW 50022, 'La tarea no está asignada a tu usuario.', 1;
    SELECT T.* FROM dbo.Tarea T WHERE T.ID=@ID;
END
GO

/* Conserva los enlaces rápidos de correo y cualquier cliente anterior que
   todavía envíe el nombre en lugar del ID. */
CREATE OR ALTER PROCEDURE dbo.usp_Tarea_ActualizarEstadoEmpleado @ID INT,@IdUsuario INT,@Estado NVARCHAR(80) AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdEstadoTarea INT;
    SELECT TOP(1) @IdEstadoTarea=ET.ID
    FROM dbo.Tarea T INNER JOIN dbo.Proyecto P ON P.ID=T.IdProyecto INNER JOIN dbo.EstadoTarea ET ON ET.IdAgencia=P.IdAgencia AND ET.Nombre=@Estado AND ET.Activo=1
    WHERE T.ID=@ID;
    IF @IdEstadoTarea IS NULL THROW 51107, 'El estado seleccionado no pertenece a la agencia o está inactivo.', 1;
    UPDATE T SET IdEstadoTarea=@IdEstadoTarea,Estado=@Estado
    FROM dbo.Tarea T WHERE T.ID=@ID AND T.Activo=1 AND (
      EXISTS(SELECT 1 FROM dbo.AsignacionTarea AT INNER JOIN dbo.Empleado E ON E.ID=AT.IdEmpleado WHERE AT.IdTarea=T.ID AND AT.Activa=1 AND E.IdUsuario=@IdUsuario)
      OR (NOT EXISTS(SELECT 1 FROM dbo.AsignacionTarea AT WHERE AT.IdTarea=T.ID AND AT.Activa=1) AND EXISTS(SELECT 1 FROM dbo.Empleado E WHERE E.ID=T.IdEmpleadoAsignado AND E.IdUsuario=@IdUsuario)));
    IF @@ROWCOUNT=0 THROW 50022, 'La tarea no está asignada a tu usuario.', 1;
END
GO
