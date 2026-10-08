using System.Data;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public sealed class BLLRespaldoBaseDatos
{
    private readonly MPPRespaldoBaseDatos _respaldoMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly BackupSettings _configuracion;
    private readonly ContinuityHistoryXmlService _historialXml;

    public BLLRespaldoBaseDatos(MPPRespaldoBaseDatos respaldoMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL, BackupSettings configuracion, ContinuityHistoryXmlService historialXml)
    {
        _respaldoMPP = respaldoMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _configuracion = configuracion;
        _historialXml = historialXml;
    }

    public (List<RespaldoBaseDatos> Respaldos, List<PruebaRestauracionRespaldo> Pruebas, List<RegistroContinuidadXml> HistorialXml) Consultar(Usuario solicitante)
    {
        ExigirGestionRespaldos(solicitante);
        return (_respaldoMPP.Consultar(), _respaldoMPP.ConsultarPruebas(), _historialXml.Consultar());
    }

    public Task<RespaldoBaseDatos> CrearManual(Usuario solicitante)
    {
        ExigirGestionRespaldos(solicitante);
        return CrearRespaldoAsync(solicitante.ID, "Manual");
    }

    public async Task<PruebaRestauracionRespaldo> RestaurarEnEntornoAislado(Usuario solicitante, int idRespaldo, RestaurarRespaldoRequest solicitud)
    {
        ExigirGestionRespaldos(solicitante);
        if (!string.Equals(solicitud.Confirmacion?.Trim(), $"RESTORE {idRespaldo}", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Para confirmar la restauración escribí RESTORE {idRespaldo}.");
        }

        RespaldoBaseDatos respaldo = _respaldoMPP.Consultar().FirstOrDefault(item => item.ID == idRespaldo && item.Estado == "Completado" && item.Verificado)
            ?? throw new KeyNotFoundException("No existe un respaldo verificado disponible para restaurar.");
        ValidarRutaRespaldo(respaldo.RutaArchivo);
        if (!File.Exists(respaldo.RutaArchivo)) { throw new FileNotFoundException("No se encontró el archivo de respaldo en la ubicación configurada."); }

        string nombreDestino = $"TeamBalanceRecovery_{idRespaldo}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        PruebaRestauracionRespaldo prueba = new PruebaRestauracionRespaldo()
        {
            IdRespaldo = respaldo.ID,
            BaseDatosDestino = nombreDestino,
            FechaInicio = DateTime.Now,
            IdUsuarioSolicitante = solicitante.ID
        };
        prueba.ID = _respaldoMPP.RegistrarPrueba(prueba);
        string? idEventoXml = null;

        try
        {
            idEventoXml = _historialXml.RegistrarInicio(CrearEventoXml("RestoreAislado", respaldo, solicitante.ID));
            Directory.CreateDirectory(RutaRecovery());
            await _respaldoMPP.VerificarBackupAsync(respaldo.RutaArchivo);
            DataTable archivos = await _respaldoMPP.ConsultarArchivosLogicosAsync(respaldo.RutaArchivo);
            List<(string Logico, string Fisico)> destinos = CrearDestinosArchivos(nombreDestino, archivos);
            await _respaldoMPP.RestaurarEnAisladoAsync(nombreDestino, respaldo.RutaArchivo, destinos);
            await _respaldoMPP.EliminarBaseAisladaAsync(nombreDestino);
            prueba.FechaFin = DateTime.Now;
            prueba.Estado = "Completada";
            prueba.Mensaje = "Backup verificado y restaurado correctamente en un entorno aislado temporal.";
            _historialXml.Finalizar(idEventoXml, "Completado", prueba.Mensaje, respaldo.TamanoBytes, true);
            _respaldoMPP.FinalizarPrueba(prueba);
            RegistrarBitacora(solicitante.ID, "ProbarRestauracionRespaldo", $"El respaldo {respaldo.NombreArchivo} se restauró en la base aislada {nombreDestino}.", "Exitoso", "Informacion");
            return prueba;
        }
        catch (Exception ex)
        {
            try { await _respaldoMPP.EliminarBaseAisladaAsync(nombreDestino); } catch { /* Se conserva el error principal en la bitácora. */ }
            if (idEventoXml is not null) { try { _historialXml.Finalizar(idEventoXml, "Fallido", LimitarMensaje(ex.Message), respaldo.TamanoBytes, false); } catch { /* No se reemplaza el error de la operación por uno de auditoría. */ } }
            prueba.FechaFin = DateTime.Now;
            prueba.Estado = "Fallida";
            prueba.Mensaje = LimitarMensaje(ex.Message);
            _respaldoMPP.FinalizarPrueba(prueba);
            RegistrarBitacora(solicitante.ID, "ProbarRestauracionRespaldo", $"Falló la restauración aislada del respaldo {respaldo.NombreArchivo}: {LimitarMensaje(ex.Message)}", "Fallido", "Critico");
            throw;
        }
    }

    public async Task RestaurarProduccion(Usuario solicitante, int idRespaldo, RestaurarRespaldoRequest solicitud)
    {
        ExigirGestionRespaldos(solicitante);
        if (!string.Equals(solicitud.Confirmacion?.Trim(), $"RECUPERAR PRODUCCION {idRespaldo}", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Para confirmar la recuperación escribí RECUPERAR PRODUCCION {idRespaldo}.");
        }

        RespaldoBaseDatos respaldo = _respaldoMPP.Consultar().FirstOrDefault(item => item.ID == idRespaldo && item.Estado == "Completado" && item.Verificado)
            ?? throw new KeyNotFoundException("No existe un respaldo verificado disponible para recuperar producción.");
        ValidarRutaRespaldo(respaldo.RutaArchivo);
        if (!File.Exists(respaldo.RutaArchivo)) throw new FileNotFoundException("No se encontró el archivo de respaldo en la ubicación configurada.");
        string idEventoXml = _historialXml.RegistrarInicio(CrearEventoXml("RestoreProduccion", respaldo, solicitante.ID));

        try
        {
            await _respaldoMPP.VerificarBackupAsync(respaldo.RutaArchivo);
            DataTable archivosRespaldo = await _respaldoMPP.ConsultarArchivosLogicosAsync(respaldo.RutaArchivo);
            DataTable archivosActuales = await _respaldoMPP.ConsultarArchivosBaseActualAsync(_configuracion.DatabaseName);
            List<(string Logico, string Fisico)> destinos = CrearDestinosProduccion(archivosRespaldo, archivosActuales);
            await _respaldoMPP.RestaurarProduccionAsync(_configuracion.DatabaseName, respaldo.RutaArchivo, destinos);
            _historialXml.Finalizar(idEventoXml, "Completado", "La base productiva se recuperó desde el respaldo seleccionado.", respaldo.TamanoBytes, true);
            RegistrarBitacora(solicitante.ID, "RestaurarProduccionDesdeRespaldo", $"La base productiva se recuperó desde el respaldo verificado {respaldo.NombreArchivo}.", "Exitoso", "Critico");
        }
        catch (Exception ex)
        {
            try { _historialXml.Finalizar(idEventoXml, "Fallido", LimitarMensaje(ex.Message), respaldo.TamanoBytes, false); } catch { /* El registro inicial externo queda como evidencia de una operación interrumpida. */ }
            try { RegistrarBitacora(solicitante.ID, "RestaurarProduccionDesdeRespaldo", $"Falló la recuperación productiva desde {respaldo.NombreArchivo}: {LimitarMensaje(ex.Message)}", "Fallido", "Critico"); } catch { /* La base puede estar en proceso de recuperación. */ }
            throw;
        }
    }

    public async Task EjecutarRespaldoDiarioSiCorresponde()
    {
        if (!_configuracion.Habilitado) return;
        DateTime ahora = HoraLocal();
        int hora = Math.Clamp(_configuracion.DailyHourLocal, 0, 23);
        if (ahora.Hour < hora) return;
        bool yaEjecutado = _respaldoMPP.Consultar().Any(item => item.Estado == "Completado" && item.Verificado && HoraLocal(item.FechaInicio).Date == ahora.Date);
        if (!yaEjecutado) await CrearRespaldoAsync(null, "Automático");
    }

    private async Task<RespaldoBaseDatos> CrearRespaldoAsync(int? idUsuarioSolicitante, string origen)
    {
        ValidarConfiguracion();
        Directory.CreateDirectory(RutaRespaldos());
        DateTime inicio = DateTime.Now;
        string nombre = $"TeamBalance_full_{inicio:yyyyMMdd_HHmmss}.bak";
        string ruta = Path.Combine(RutaRespaldos(), nombre);
        RespaldoBaseDatos respaldo = new RespaldoBaseDatos()
        {
            NombreArchivo = nombre,
            RutaArchivo = ruta,
            FechaInicio = inicio,
            IdUsuarioSolicitante = idUsuarioSolicitante,
            FechaExpiracion = inicio.AddDays(Math.Clamp(_configuracion.RetentionDays, 1, 365))
        };
        respaldo.ID = _respaldoMPP.Registrar(respaldo);
        string? idEventoXml = null;
        try
        {
            idEventoXml = _historialXml.RegistrarInicio(CrearEventoXml("Backup", respaldo, idUsuarioSolicitante));
            await _respaldoMPP.EjecutarBackupAsync(_configuracion.DatabaseName, ruta);
            await _respaldoMPP.VerificarBackupAsync(ruta);
            int archivosConfiguracion = RespaldarConfiguracion(nombre);
            respaldo.FechaFin = DateTime.Now;
            respaldo.Estado = "Completado";
            respaldo.Verificado = true;
            respaldo.TamanoBytes = File.Exists(ruta) ? new FileInfo(ruta).Length : null;
            respaldo.Mensaje = $"Respaldo completo {origen.ToLowerInvariant()} verificado con RESTORE VERIFYONLY. Configuración asociada: {archivosConfiguracion} archivo(s).";
            _historialXml.Finalizar(idEventoXml, "Completado", respaldo.Mensaje, respaldo.TamanoBytes, true, archivosConfiguracion);
            _respaldoMPP.Finalizar(respaldo);
            RegistrarBitacora(idUsuarioSolicitante, "CrearRespaldoBaseDatos", $"Se generó un respaldo completo {origen.ToLowerInvariant()} y se verificó su integridad.", "Exitoso", "Informacion");
            PurgarVencidos();
            return respaldo;
        }
        catch (Exception ex)
        {
            if (idEventoXml is not null) { try { _historialXml.Finalizar(idEventoXml, "Fallido", LimitarMensaje(ex.Message), null, false); } catch { /* El registro inicial externo conserva la operación interrumpida. */ } }
            respaldo.FechaFin = DateTime.Now;
            respaldo.Estado = "Fallido";
            respaldo.Verificado = false;
            respaldo.Mensaje = LimitarMensaje(ex.Message);
            _respaldoMPP.Finalizar(respaldo);
            RegistrarBitacora(idUsuarioSolicitante, "CrearRespaldoBaseDatos", $"Falló la creación de un respaldo {origen.ToLowerInvariant()}: {LimitarMensaje(ex.Message)}", "Fallido", "Critico");
            throw;
        }
    }

    private void PurgarVencidos()
    {
        foreach (RespaldoBaseDatos respaldo in _respaldoMPP.Consultar().Where(item => item.FechaExpiracion < DateTime.Now))
        {
            try
            {
                ValidarRutaRespaldo(respaldo.RutaArchivo);
                if (File.Exists(respaldo.RutaArchivo)) File.Delete(respaldo.RutaArchivo);
                string configuracion = RutaConfiguracion(respaldo.NombreArchivo);
                if (Directory.Exists(configuracion)) Directory.Delete(configuracion, true);
                _respaldoMPP.EliminarRegistro(respaldo.ID);
            }
            catch
            {
                // Se conserva el registro para volver a intentar la purga sin ocultar un archivo existente.
            }
        }
    }

    private List<(string Logico, string Fisico)> CrearDestinosArchivos(string nombreBaseDatosDestino, DataTable archivos)
    {
        if (!archivos.Columns.Contains("LogicalName") || !archivos.Columns.Contains("Type") || archivos.Rows.Count == 0) throw new InvalidOperationException("El backup no contiene archivos restaurables.");
        int datos = 0;
        int log = 0;
        return archivos.Rows.Cast<DataRow>().Select(fila =>
        {
            string logico = Convert.ToString(fila["LogicalName"]) ?? throw new InvalidOperationException("El backup contiene un archivo sin nombre lógico.");
            bool esLog = string.Equals(Convert.ToString(fila["Type"]), "L", StringComparison.OrdinalIgnoreCase);
            int indice = esLog ? ++log : ++datos;
            string extension = esLog ? ".ldf" : indice == 1 ? ".mdf" : ".ndf";
            string fisico = Path.Combine(RutaRecovery(), $"{nombreBaseDatosDestino}_{(esLog ? "log" : "data")}{indice}{extension}");
            return (logico, fisico);
        }).ToList();
    }

    private static RegistroContinuidadXml CrearEventoXml(string tipo, RespaldoBaseDatos respaldo, int? idUsuario)
    {
        RegistroContinuidadXml evento = new RegistroContinuidadXml();
        evento.Tipo = tipo;
        evento.IdRespaldo = respaldo.ID;
        evento.NombreArchivo = respaldo.NombreArchivo;
        evento.TamanoBytes = respaldo.TamanoBytes;
        evento.Verificado = respaldo.Verificado;
        evento.IdUsuarioSolicitante = idUsuario;
        return evento;
    }

    private static List<(string Logico, string Fisico)> CrearDestinosProduccion(DataTable archivosRespaldo, DataTable archivosActuales)
    {
        if (!archivosRespaldo.Columns.Contains("LogicalName") || !archivosRespaldo.Columns.Contains("Type") || !archivosActuales.Columns.Contains("LogicalName") || !archivosActuales.Columns.Contains("FileType") || !archivosActuales.Columns.Contains("PhysicalName"))
            throw new InvalidOperationException("No fue posible identificar los archivos de la base productiva.");

        List<DataRow> actuales = archivosActuales.Rows.Cast<DataRow>().ToList();
        List<DataRow> respaldo = archivosRespaldo.Rows.Cast<DataRow>().ToList();
        if (actuales.Count == 0 || respaldo.Count == 0) throw new InvalidOperationException("No se encontraron archivos para restaurar la base productiva.");

        Dictionary<string, DataRow> porNombre = actuales.ToDictionary(fila => Convert.ToString(fila["LogicalName"]) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        Dictionary<int, Queue<DataRow>> porTipo = actuales.GroupBy(fila => Convert.ToInt32(fila["FileType"]))
            .ToDictionary(grupo => grupo.Key, grupo => new Queue<DataRow>(grupo));

        return respaldo.Select(fila =>
        {
            string logico = Convert.ToString(fila["LogicalName"]) ?? throw new InvalidOperationException("El backup contiene un archivo sin nombre lógico.");
            int tipo = string.Equals(Convert.ToString(fila["Type"]), "L", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            if (!porNombre.TryGetValue(logico, out DataRow? actual))
            {
                if (!porTipo.TryGetValue(tipo, out Queue<DataRow>? coincidencias) || coincidencias.Count == 0)
                    throw new InvalidOperationException("La estructura física del backup no coincide con la base productiva.");
                actual = coincidencias.Dequeue();
            }
            return (logico, Convert.ToString(actual["PhysicalName"]) ?? throw new InvalidOperationException("No se encontró la ruta física de un archivo productivo."));
        }).ToList();
    }

    private int RespaldarConfiguracion(string nombreRespaldo)
    {
        string directorioDestino = RutaConfiguracion(nombreRespaldo);
        Directory.CreateDirectory(directorioDestino);
        List<string> archivosCopiados = new List<string>();
        foreach (string archivoConfiguracion in _configuracion.ConfigurationFiles.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            string origen = Path.IsPathRooted(archivoConfiguracion)
                ? Path.GetFullPath(archivoConfiguracion)
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, archivoConfiguracion));
            if (!File.Exists(origen)) continue;
            string destino = Path.Combine(directorioDestino, Path.GetFileName(origen));
            File.Copy(origen, destino, true);
            archivosCopiados.Add(Path.GetFileName(origen));
        }

        string[] lineasManifest = new string[]
        {
            $"Respaldo de configuración asociado a {nombreRespaldo}",
            $"Generado: {DateTime.Now:O}",
            $"Archivos: {string.Join(", ", archivosCopiados)}"
        };
        File.WriteAllLines(Path.Combine(directorioDestino, "manifest.txt"), lineasManifest);
        return archivosCopiados.Count;
    }

    private void ExigirGestionRespaldos(Usuario usuario)
    {
        if (!usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase)) || !_rolBLL.TienePermiso(usuario, "GestionarRespaldos"))
        {
            throw new UnauthorizedAccessException("Sólo Soporte TeamBalance autorizado puede administrar respaldos.");
        }
    }

    private void ValidarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_configuracion.DatabaseName) || string.IsNullOrWhiteSpace(_configuracion.Directory) || string.IsNullOrWhiteSpace(_configuracion.RecoveryDirectory)) throw new InvalidOperationException("La configuración de respaldos está incompleta.");
        if (string.Equals(RutaRespaldos(), RutaRecovery(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La carpeta de respaldos debe ser distinta de la carpeta de restauraciones aisladas.");
    }

    private string RutaRespaldos() => Path.GetFullPath(_configuracion.Directory);
    private string RutaRecovery() => Path.GetFullPath(_configuracion.RecoveryDirectory);
    private string RutaConfiguracion(string nombreRespaldo) => Path.Combine(RutaRespaldos(), "configuration", Path.GetFileNameWithoutExtension(nombreRespaldo));
    private void ValidarRutaRespaldo(string ruta) { if (!ruta.StartsWith(RutaRespaldos() + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La ruta del respaldo no pertenece al almacenamiento configurado."); }
    private DateTime HoraLocal(DateTime fecha) { try { return TimeZoneInfo.ConvertTime(fecha, TimeZoneInfo.FindSystemTimeZoneById(_configuracion.TimeZoneId)); } catch { return fecha; } }
    private DateTime HoraLocal() => HoraLocal(DateTime.Now);
    private void RegistrarBitacora(int? idUsuario, string accion, string mensaje, string resultado, string criticidad) => _bitacoraBLL.Add(new Bitacora(idUsuario, null, "RespaldoBaseDatos", null, accion, mensaje, resultado, criticidad, "Respaldos"));
    private static string LimitarMensaje(string mensaje) => mensaje.Length > 1800 ? mensaje[..1800] : mensaje;
}
