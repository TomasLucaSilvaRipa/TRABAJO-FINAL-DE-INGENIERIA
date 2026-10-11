/*
  CU05-006 - Registro de agencia reintentable
  Una contratación aprobada ya asociada se consulta para que la aplicación
  pueda finalizar tareas pendientes de un intento interrumpido.
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
      AND Activo = 1;
END;
GO
