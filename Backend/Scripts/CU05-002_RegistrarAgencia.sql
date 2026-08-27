/*
  CU05-002 - Registrar Agencia Cliente
  Ejecutar sobre la base TeamBalance después de CU05-001.
*/
GO

CREATE OR ALTER PROCEDURE dbo.usp_Contratacion_ConsultarParaRegistro
    @ReferenciaContratacion NVARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        ID,
        IdAgencia,
        IdUsuario,
        ReferenciaContratacion,
        NombreComercialAgencia,
        RazonSocial,
        CUIT,
        CondicionFiscal,
        EmailFacturacion,
        TelefonoContacto,
        NombreResponsable,
        ApellidoResponsable,
        EmailLaboralResponsable,
        EstadoContratacion,
        Activo
    FROM dbo.ContratacionServicio
    WHERE ReferenciaContratacion = @ReferenciaContratacion
      AND EstadoContratacion = N'Aprobada'
      AND Activo = 1
      AND IdAgencia IS NULL
      AND IdUsuario IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Rol_ConsultarPorNombre
    @Nombre NVARCHAR(80)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        ID,
        Nombre,
        Descripcion,
        EsRolBase,
        Activo,
        FechaBaja
    FROM dbo.Rol
    WHERE Nombre = @Nombre
      AND Activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ExisteEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CONVERT(bit, CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Usuario
        WHERE Email = @Email
    ) THEN 1 ELSE 0 END) AS Existe;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Agencia_Existe
    @CUIT NVARCHAR(20),
    @EmailContacto NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CONVERT(bit, CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Agencia
        WHERE CUIT = @CUIT
           OR EmailContacto = @EmailContacto
    ) THEN 1 ELSE 0 END) AS Existe;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Agencia_RegistrarDesdeContratacion
    @ReferenciaContratacion NVARCHAR(250),
    @NombreComercial NVARCHAR(150),
    @RazonSocial NVARCHAR(200) = NULL,
    @CUIT NVARCHAR(20),
    @CondicionFiscal NVARCHAR(80) = NULL,
    @EmailContacto NVARCHAR(150),
    @TelefonoContacto NVARCHAR(50) = NULL,
    @IdRol INT,
    @Nombre NVARCHAR(100),
    @Apellido NVARCHAR(100),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(500),
    @EstadoUsuario NVARCHAR(50),
    @ActivoUsuario BIT,
    @ActivoDueno BIT,
    @MetodoValidacion NVARCHAR(30),
    @TokenHash NVARCHAR(500),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdContratacion INT;
    DECLARE @IdAgencia INT;
    DECLARE @IdUsuario INT;

    BEGIN TRANSACTION;

    SELECT @IdContratacion = ID
    FROM dbo.ContratacionServicio WITH (UPDLOCK, HOLDLOCK)
    WHERE ReferenciaContratacion = @ReferenciaContratacion
      AND EstadoContratacion = N'Aprobada'
      AND Activo = 1
      AND IdAgencia IS NULL
      AND IdUsuario IS NULL;

    IF @IdContratacion IS NULL
        THROW 51001, 'La contratación no está aprobada o ya fue utilizada para registrar una agencia.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE ID = @IdRol AND Nombre = N'Dueño' AND Activo = 1)
        THROW 51002, 'No existe un rol Dueño activo para crear el usuario inicial.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Agencia WHERE CUIT = @CUIT OR EmailContacto = @EmailContacto)
        THROW 51003, 'Ya existe una agencia con el CUIT o email de contacto indicado.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE Email = @Email)
        THROW 51004, 'Ya existe un usuario con el email laboral indicado.', 1;

    INSERT INTO dbo.Agencia
    (
        NombreComercial,
        RazonSocial,
        CUIT,
        CondicionFiscal,
        EmailContacto,
        TelefonoContacto,
        FechaAlta,
        Estado,
        Activo
    )
    VALUES
    (
        @NombreComercial,
        NULLIF(@RazonSocial, N''),
        @CUIT,
        NULLIF(@CondicionFiscal, N''),
        @EmailContacto,
        NULLIF(@TelefonoContacto, N''),
        SYSDATETIME(),
        N'Activa',
        1
    );

    SET @IdAgencia = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Usuario
    (
        IdAgencia,
        IdRol,
        Nombre,
        Apellido,
        Email,
        PasswordHash,
        Estado,
        FechaAlta,
        Activo
    )
    VALUES
    (
        @IdAgencia,
        @IdRol,
        @Nombre,
        @Apellido,
        @Email,
        @PasswordHash,
        @EstadoUsuario,
        SYSDATETIME(),
        @ActivoUsuario
    );

    SET @IdUsuario = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.[Dueño]
    (
        IdUsuario,
        Activo
    )
    VALUES
    (
        @IdUsuario,
        @ActivoDueno
    );

    INSERT INTO dbo.ValidacionCuenta
    (
        IdUsuario,
        Metodo,
        TokenHash,
        FechaGeneracion,
        FechaExpiracion,
        Utilizado,
        Activo
    )
    VALUES
    (
        @IdUsuario,
        @MetodoValidacion,
        @TokenHash,
        SYSDATETIME(),
        @FechaExpiracion,
        0,
        1
    );

    UPDATE dbo.ContratacionServicio
    SET IdAgencia = @IdAgencia,
        IdUsuario = @IdUsuario
    WHERE ID = @IdContratacion;

    COMMIT TRANSACTION;

    SELECT
        @IdAgencia AS IdAgencia,
        @IdUsuario AS IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarPorEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        ID,
        IdAgencia,
        IdRol,
        Nombre,
        Apellido,
        Email,
        PasswordHash,
        Estado,
        FechaAlta,
        Activo,
        FechaBaja
    FROM dbo.Usuario
    WHERE Email = @Email;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ConsultarPendienteValidacion
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        ID,
        IdAgencia,
        IdRol,
        Nombre,
        Apellido,
        Email,
        PasswordHash,
        Estado,
        FechaAlta,
        Activo,
        FechaBaja
    FROM dbo.Usuario
    WHERE Email = @Email
      AND Estado = N'PendienteValidacion'
      AND Activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ValidacionCuenta_Validar
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdValidacion INT;
    DECLARE @IdUsuario INT;

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @IdValidacion = ID,
        @IdUsuario = IdUsuario
    FROM dbo.ValidacionCuenta WITH (UPDLOCK, HOLDLOCK)
    WHERE TokenHash = @TokenHash
      AND Metodo = N'Email'
      AND Utilizado = 0
      AND Activo = 1
      AND FechaExpiracion >= SYSDATETIME();

    IF @IdValidacion IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT CONVERT(bit, 0) AS Validada;
        RETURN;
    END;

    UPDATE dbo.ValidacionCuenta
    SET Utilizado = 1,
        FechaUtilizacion = SYSDATETIME(),
        Activo = 0
    WHERE ID = @IdValidacion;

    UPDATE dbo.Usuario
    SET Estado = N'Activo'
    WHERE ID = @IdUsuario;

    COMMIT TRANSACTION;

    SELECT CONVERT(bit, 1) AS Validada;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ValidacionCuenta_Reenviar
    @IdUsuario INT,
    @Metodo NVARCHAR(30),
    @TokenHash NVARCHAR(500),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.ValidacionCuenta
    SET Activo = 0
    WHERE IdUsuario = @IdUsuario
      AND Metodo = @Metodo
      AND Utilizado = 0
      AND Activo = 1;

    INSERT INTO dbo.ValidacionCuenta
    (
        IdUsuario,
        Metodo,
        TokenHash,
        FechaGeneracion,
        FechaExpiracion,
        Utilizado,
        Activo
    )
    VALUES
    (
        @IdUsuario,
        @Metodo,
        @TokenHash,
        SYSDATETIME(),
        @FechaExpiracion,
        0,
        1
    );

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SesionUsuario_Registrar
    @IdUsuario INT,
    @TokenHash NVARCHAR(500),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SesionUsuario
    (
        IdUsuario,
        TokenHash,
        FechaInicio,
        FechaUltimaActividad,
        FechaExpiracion,
        Activa
    )
    VALUES
    (
        @IdUsuario,
        @TokenHash,
        SYSDATETIME(),
        SYSDATETIME(),
        @FechaExpiracion,
        1
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SesionUsuario_Validar
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdSesion INT;

    SELECT TOP (1) @IdSesion = ID
    FROM dbo.SesionUsuario
    WHERE TokenHash = @TokenHash
      AND Activa = 1
      AND FechaExpiracion >= SYSDATETIME();

    IF @IdSesion IS NULL
    BEGIN
        SELECT CONVERT(bit, 0) AS Vigente;
        RETURN;
    END;

    UPDATE dbo.SesionUsuario
    SET FechaUltimaActividad = SYSDATETIME()
    WHERE ID = @IdSesion;

    SELECT CONVERT(bit, 1) AS Vigente;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_SesionUsuario_Cerrar
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SesionUsuario
    SET Activa = 0,
        FechaCierre = SYSDATETIME()
    WHERE TokenHash = @TokenHash
      AND Activa = 1;
END;
GO
