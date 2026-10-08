/*
   AES-GCM agrega nonce, tag de autenticación y texto codificado en Base64.
   La observación continúa limitada por la interfaz, pero requiere más espacio al persistirse cifrada.
*/
IF COL_LENGTH('dbo.DisponibilidadBase', 'Observacion') IS NOT NULL
BEGIN
    ALTER TABLE dbo.DisponibilidadBase ALTER COLUMN Observacion NVARCHAR(MAX) NULL;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_DisponibilidadBase_RegistrarActualizar
    @IdEmpleado INT,
    @HoraInicio TIME,
    @HoraFin TIME,
    @HorasSemanales DECIMAL(10,2),
    @Observacion NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS(SELECT 1 FROM dbo.DisponibilidadBase WHERE IdEmpleado=@IdEmpleado)
        UPDATE dbo.DisponibilidadBase
        SET HoraInicio=@HoraInicio, HoraFin=@HoraFin, HorasSemanales=@HorasSemanales,
            Observacion=@Observacion, Activo=1
        WHERE IdEmpleado=@IdEmpleado;
    ELSE
        INSERT INTO dbo.DisponibilidadBase(IdEmpleado, HoraInicio, HoraFin, HorasSemanales, Observacion, Activo)
        VALUES(@IdEmpleado, @HoraInicio, @HoraFin, @HorasSemanales, @Observacion, 1);
END
GO
