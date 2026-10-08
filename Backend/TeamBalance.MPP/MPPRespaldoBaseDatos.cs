using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPRespaldoBaseDatos
{
    private readonly Conexion _conexion;

    public MPPRespaldoBaseDatos(Conexion conexion) { _conexion = conexion; }

    public async Task EjecutarBackupAsync(string nombreBaseDatos, string rutaArchivo)
    {
        string baseDatos = IdentificadorSeguro(nombreBaseDatos);
        const string consulta = "BACKUP DATABASE {0} TO DISK = @RutaArchivo WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;";
        await _conexion.EjecutarAdministracionAsync(string.Format(consulta, baseDatos), new List<SqlParameter> { new("@RutaArchivo", rutaArchivo) }, nombreBaseDatos, 1800);
    }

    public Task VerificarBackupAsync(string rutaArchivo) => _conexion.EjecutarAdministracionAsync("RESTORE VERIFYONLY FROM DISK = @RutaArchivo WITH CHECKSUM;", new List<SqlParameter> { new("@RutaArchivo", rutaArchivo) }, "master", 1800);

    public async Task<DataTable> ConsultarArchivosLogicosAsync(string rutaArchivo)
    {
        return await _conexion.LeerAdministracionAsync("RESTORE FILELISTONLY FROM DISK = @RutaArchivo;", new List<SqlParameter> { new("@RutaArchivo", rutaArchivo) }, "master", 1800);
    }

    public Task RestaurarEnAisladoAsync(string nombreBaseDatosDestino, string rutaArchivo, IEnumerable<(string Logico, string Fisico)> archivos)
    {
        string destino = IdentificadorSeguro(nombreBaseDatosDestino);
        string movimientos = string.Join(", ", archivos.Select(archivo => $"MOVE N'{LiteralSeguro(archivo.Logico)}' TO N'{LiteralSeguro(archivo.Fisico)}'"));
        string consulta = $"RESTORE DATABASE {destino} FROM DISK = @RutaArchivo WITH {movimientos}, CHECKSUM, RECOVERY, STATS = 10; DBCC CHECKDB ({destino}) WITH NO_INFOMSGS;";
        return _conexion.EjecutarAdministracionAsync(consulta, new List<SqlParameter> { new("@RutaArchivo", rutaArchivo) }, "master", 3600);
    }

    public Task<DataTable> ConsultarArchivosBaseActualAsync(string nombreBaseDatos)
    {
        string nombreSeguro = IdentificadorSeguro(nombreBaseDatos);
        const string consulta = "SELECT name AS LogicalName, type AS FileType, physical_name AS PhysicalName FROM sys.master_files WHERE database_id = DB_ID(@NombreBaseDatos) ORDER BY file_id;";
        return _conexion.LeerAdministracionAsync(consulta, new List<SqlParameter> { new("@NombreBaseDatos", nombreBaseDatos) }, "master", 300);
    }

    public Task RestaurarProduccionAsync(string nombreBaseDatos, string rutaArchivo, IEnumerable<(string Logico, string Fisico)> archivos)
    {
        string destino = IdentificadorSeguro(nombreBaseDatos);
        string literal = LiteralSeguro(nombreBaseDatos);
        string movimientos = string.Join(", ", archivos.Select(archivo => $"MOVE N'{LiteralSeguro(archivo.Logico)}' TO N'{LiteralSeguro(archivo.Fisico)}'"));
        string consulta = $"BEGIN TRY " +
            $"ALTER DATABASE {destino} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"RESTORE DATABASE {destino} FROM DISK = @RutaArchivo WITH REPLACE, {movimientos}, CHECKSUM, RECOVERY, STATS = 10; " +
            $"DBCC CHECKDB ({destino}) WITH NO_INFOMSGS; " +
            $"ALTER DATABASE {destino} SET MULTI_USER; " +
            "END TRY BEGIN CATCH " +
            $"IF DB_ID(N'{literal}') IS NOT NULL ALTER DATABASE {destino} SET MULTI_USER; " +
            "THROW; END CATCH;";
        return _conexion.EjecutarAdministracionAsync(consulta, new List<SqlParameter> { new("@RutaArchivo", rutaArchivo) }, "master", 3600);
    }

    public Task EliminarBaseAisladaAsync(string nombreBaseDatos)
    {
        string destino = IdentificadorSeguro(nombreBaseDatos);
        string literal = LiteralSeguro(nombreBaseDatos);
        string consulta = $"IF DB_ID(N'{literal}') IS NOT NULL BEGIN ALTER DATABASE {destino} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {destino}; END;";
        return _conexion.EjecutarAdministracionAsync(consulta, null, "master", 300);
    }

    public int Registrar(RespaldoBaseDatos respaldo)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_RespaldoBaseDatos_Registrar", new List<SqlParameter>
        {
            new("@NombreArchivo", respaldo.NombreArchivo), new("@RutaArchivo", respaldo.RutaArchivo), new("@FechaInicio", respaldo.FechaInicio),
            new("@IdUsuarioSolicitante", (object?)respaldo.IdUsuarioSolicitante ?? DBNull.Value), new("@FechaExpiracion", respaldo.FechaExpiracion)
        });
        return tabla.Rows.Count == 1 ? Convert.ToInt32(tabla.Rows[0]["ID"]) : throw new InvalidOperationException("No se pudo registrar el respaldo.");
    }

    public void Finalizar(RespaldoBaseDatos respaldo) => _conexion.Escribir("dbo.usp_RespaldoBaseDatos_Finalizar", new List<SqlParameter>
    {
        new("@IdRespaldo", respaldo.ID), new("@FechaFin", (object?)respaldo.FechaFin ?? DBNull.Value), new("@Estado", respaldo.Estado), new("@Verificado", respaldo.Verificado),
        new("@TamanoBytes", (object?)respaldo.TamanoBytes ?? DBNull.Value), new("@Mensaje", (object?)respaldo.Mensaje ?? DBNull.Value)
    });

    public List<RespaldoBaseDatos> Consultar() => _conexion.Leer("dbo.usp_RespaldoBaseDatos_Consultar").Rows.Cast<DataRow>().Select(CrearRespaldo).ToList();

    public int RegistrarPrueba(PruebaRestauracionRespaldo prueba)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_PruebaRestauracion_Registrar", new List<SqlParameter>
        {
            new("@IdRespaldo", prueba.IdRespaldo), new("@BaseDatosDestino", prueba.BaseDatosDestino), new("@FechaInicio", prueba.FechaInicio), new("@IdUsuarioSolicitante", (object?)prueba.IdUsuarioSolicitante ?? DBNull.Value)
        });
        return tabla.Rows.Count == 1 ? Convert.ToInt32(tabla.Rows[0]["ID"]) : throw new InvalidOperationException("No se pudo registrar la prueba de restauración.");
    }

    public void FinalizarPrueba(PruebaRestauracionRespaldo prueba) => _conexion.Escribir("dbo.usp_PruebaRestauracion_Finalizar", new List<SqlParameter>
    {
        new("@IdPrueba", prueba.ID), new("@FechaFin", (object?)prueba.FechaFin ?? DBNull.Value), new("@Estado", prueba.Estado), new("@Mensaje", (object?)prueba.Mensaje ?? DBNull.Value)
    });

    public List<PruebaRestauracionRespaldo> ConsultarPruebas() => _conexion.Leer("dbo.usp_PruebaRestauracion_Consultar").Rows.Cast<DataRow>().Select(fila => new PruebaRestauracionRespaldo
    {
        ID = Convert.ToInt32(fila["ID"]), IdRespaldo = Convert.ToInt32(fila["IdRespaldo"]), BaseDatosDestino = Convert.ToString(fila["BaseDatosDestino"]) ?? string.Empty,
        FechaInicio = Convert.ToDateTime(fila["FechaInicio"]), FechaFin = fila["FechaFin"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaFin"]),
        Estado = Convert.ToString(fila["Estado"]) ?? string.Empty, Mensaje = fila["Mensaje"] == DBNull.Value ? null : Convert.ToString(fila["Mensaje"]),
        IdUsuarioSolicitante = fila["IdUsuarioSolicitante"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuarioSolicitante"])
    }).ToList();

    public void EliminarRegistro(int idRespaldo) => _conexion.Escribir("dbo.usp_RespaldoBaseDatos_Eliminar", new List<SqlParameter> { new("@IdRespaldo", idRespaldo) });

    private static RespaldoBaseDatos CrearRespaldo(DataRow fila) => new()
    {
        ID = Convert.ToInt32(fila["ID"]), NombreArchivo = Convert.ToString(fila["NombreArchivo"]) ?? string.Empty, RutaArchivo = Convert.ToString(fila["RutaArchivo"]) ?? string.Empty,
        FechaInicio = Convert.ToDateTime(fila["FechaInicio"]), FechaFin = fila["FechaFin"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaFin"]),
        Estado = Convert.ToString(fila["Estado"]) ?? string.Empty, Verificado = Convert.ToBoolean(fila["Verificado"]), TamanoBytes = fila["TamanoBytes"] == DBNull.Value ? null : Convert.ToInt64(fila["TamanoBytes"]),
        Mensaje = fila["Mensaje"] == DBNull.Value ? null : Convert.ToString(fila["Mensaje"]), IdUsuarioSolicitante = fila["IdUsuarioSolicitante"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuarioSolicitante"]), FechaExpiracion = Convert.ToDateTime(fila["FechaExpiracion"])
    };

    private static string IdentificadorSeguro(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Any(caracter => !char.IsLetterOrDigit(caracter) && caracter != '_')) throw new ArgumentException("El nombre de la base de datos no es válido.");
        return $"[{valor}]";
    }
    private static string LiteralSeguro(string valor) => valor.Replace("'", "''", StringComparison.Ordinal);
}
