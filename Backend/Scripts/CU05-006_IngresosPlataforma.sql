/*
   CU05-006 - Consulta financiera interna de TeamBalance.
   Devuelve exclusivamente cobros aprobados y no mezcla monedas.
   El corte territorial disponible actualmente es Agencia.
*/
GO

CREATE OR ALTER PROCEDURE dbo.usp_IngresosPlataforma_ConsultarCobrosConfirmados
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdAgencia, Agencia, Importe, Moneda, Proveedor, FechaCobro
    FROM
    (
        SELECT A.ID AS IdAgencia,
               A.NombreComercial AS Agencia,
               OP.Importe,
               OP.Moneda,
               OP.Proveedor,
               COALESCE(OP.FechaAprobacion, OP.FechaActualizacion, OP.FechaCreacion) AS FechaCobro
        FROM dbo.OperacionPago AS OP
        INNER JOIN dbo.ContratacionServicio AS C ON C.ID = OP.IdContratacionServicio
        INNER JOIN dbo.Agencia AS A ON A.ID = C.IdAgencia
        WHERE LOWER(LTRIM(RTRIM(OP.Estado))) IN (N'approved', N'aprobada')

        UNION ALL

        SELECT A.ID AS IdAgencia,
               A.NombreComercial AS Agencia,
               OS.Importe,
               OS.Moneda,
               OS.Proveedor,
               OS.FechaCreacion AS FechaCobro
        FROM dbo.OperacionSuscripcion AS OS
        INNER JOIN dbo.Suscripcion AS S ON S.ID = OS.IdSuscripcion
        INNER JOIN dbo.Agencia AS A ON A.ID = S.IdAgencia
        WHERE LOWER(LTRIM(RTRIM(OS.Estado))) IN (N'approved', N'aprobada')
          AND OS.Importe IS NOT NULL
          AND OS.Importe > 0
    ) AS Cobros
    WHERE Importe > 0
      AND Moneda IS NOT NULL
      AND FechaCobro IS NOT NULL
    ORDER BY FechaCobro DESC, IdAgencia;
END;
GO

MERGE dbo.Permiso AS destino
USING (VALUES
    (N'ConsultarIngresosPlataforma', N'Consultar ingresos cobrados de TeamBalance, separados por moneda y agencia.', N'/dashboard/ingresos-plataforma')
) AS origen(Codigo, Descripcion, Url)
ON destino.Codigo = origen.Codigo
WHEN MATCHED THEN UPDATE SET Nombre = origen.Codigo, Descripcion = origen.Descripcion, Url = origen.Url, Activo = 1
WHEN NOT MATCHED THEN INSERT(Nombre, Descripcion, Activo, Codigo, Url) VALUES(origen.Codigo, origen.Descripcion, 1, origen.Codigo, origen.Url);
GO
