using System.Xml.Linq;

namespace TeamBalance.Services;

public sealed class RegistroContinuidadXml
{
    public string IdOperacion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicioUtc { get; set; }
    public DateTime? FechaFinUtc { get; set; }
    public int? IdRespaldo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public long? TamanoBytes { get; set; }
    public bool? Verificado { get; set; }
    public int? IdUsuarioSolicitante { get; set; }
    public int? ArchivosConfiguracion { get; set; }
    public string? Mensaje { get; set; }
}


public sealed class ContinuityHistoryXmlService
{
    private static readonly SemaphoreSlim Bloqueo = new SemaphoreSlim(1, 1);
    private readonly BackupSettings _configuracion;

    public ContinuityHistoryXmlService(BackupSettings configuracion) { _configuracion = configuracion; }

    public string RegistrarInicio(RegistroContinuidadXml registro)
    {
        if (string.IsNullOrWhiteSpace(registro.Tipo)) throw new ArgumentException("El tipo de evento de continuidad es obligatorio.");
        registro.IdOperacion = string.IsNullOrWhiteSpace(registro.IdOperacion) ? Guid.NewGuid().ToString("N") : registro.IdOperacion;
        registro.Estado = "En proceso";
        registro.FechaInicioUtc = registro.FechaInicioUtc == default ? DateTime.UtcNow : registro.FechaInicioUtc.ToUniversalTime();
        Modificar(documento =>
        {
            documento.Root!.Element("Eventos")!.Add(CrearElemento(registro));
            return 0;
        });
        return registro.IdOperacion;
    }

    public void Finalizar(string idOperacion, string estado, string? mensaje = null, long? tamanoBytes = null, bool? verificado = null, int? archivosConfiguracion = null)
    {
        Modificar(documento =>
        {
            XElement evento = documento.Root!.Element("Eventos")!.Elements("Evento")
                .SingleOrDefault(item => string.Equals((string?)item.Attribute("id"), idOperacion, StringComparison.Ordinal))
                ?? throw new KeyNotFoundException("No existe el evento XML de continuidad que se desea finalizar.");
            evento.SetAttributeValue("estado", estado);
            evento.SetAttributeValue("fechaFinUtc", DateTime.UtcNow.ToString("O"));
            EstablecerElemento(evento, "Mensaje", mensaje);
            EstablecerElemento(evento, "TamanoBytes", tamanoBytes?.ToString());
            EstablecerElemento(evento, "Verificado", verificado?.ToString().ToLowerInvariant());
            EstablecerElemento(evento, "ArchivosConfiguracion", archivosConfiguracion?.ToString());
            return 0;
        });
    }

    public List<RegistroContinuidadXml> Consultar(int cantidadMaxima = 100)
    {
        Bloqueo.Wait();
        try
        {
            if (!File.Exists(RutaArchivo())) return new List<RegistroContinuidadXml>();
            XDocument documento = XDocument.Load(RutaArchivo(), LoadOptions.None);
            List<RegistroContinuidadXml> registros = documento.Root?.Element("Eventos")?.Elements("Evento")
                .Select(Convertir)
                .OrderByDescending(item => item.FechaInicioUtc)
                .Take(Math.Clamp(cantidadMaxima, 1, 500))
                .ToList() ?? new List<RegistroContinuidadXml>();
            return registros;
        }
        finally { Bloqueo.Release(); }
    }

    private void Modificar(Func<XDocument, int> accion)
    {
        Bloqueo.Wait();
        try
        {
            string ruta = RutaArchivo();
            Directory.CreateDirectory(Path.GetDirectoryName(ruta) ?? throw new InvalidOperationException("La ruta del historial XML no es válida."));
            XDocument documento = File.Exists(ruta) ? XDocument.Load(ruta, LoadOptions.PreserveWhitespace) : CrearDocumentoInicial();
            if (documento.Root?.Element("Eventos") is null) throw new InvalidOperationException("El historial XML de continuidad no tiene una estructura válida.");
            accion(documento);
            string temporal = ruta + ".tmp";
            documento.Save(temporal);
            File.Move(temporal, ruta, true);
        }
        finally { Bloqueo.Release(); }
    }

    private static XDocument CrearDocumentoInicial()
    {
        XDocument documento = new XDocument();
        XElement raiz = new XElement("HistorialContinuidad");
        raiz.SetAttributeValue("version", "1");
        raiz.Add(new XElement("Eventos"));
        documento.Declaration = new XDeclaration("1.0", "utf-8", "yes");
        documento.Add(raiz);
        return documento;
    }

    private static XElement CrearElemento(RegistroContinuidadXml registro)
    {
        XElement evento = new XElement("Evento");
        evento.SetAttributeValue("id", registro.IdOperacion);
        evento.SetAttributeValue("tipo", registro.Tipo);
        evento.SetAttributeValue("estado", registro.Estado);
        evento.SetAttributeValue("fechaInicioUtc", registro.FechaInicioUtc.ToString("O"));
        evento.Add(new XElement("Respaldo", new XAttribute("idSql", registro.IdRespaldo?.ToString() ?? string.Empty), registro.NombreArchivo));
        evento.Add(new XElement("UsuarioSolicitanteId", registro.IdUsuarioSolicitante?.ToString() ?? string.Empty));
        evento.Add(new XElement("TamanoBytes", registro.TamanoBytes?.ToString() ?? string.Empty));
        evento.Add(new XElement("Verificado", registro.Verificado?.ToString().ToLowerInvariant() ?? string.Empty));
        evento.Add(new XElement("ArchivosConfiguracion", registro.ArchivosConfiguracion?.ToString() ?? string.Empty));
        evento.Add(new XElement("Mensaje", registro.Mensaje ?? string.Empty));
        return evento;
    }

    private static RegistroContinuidadXml Convertir(XElement elemento)
    {
        RegistroContinuidadXml registro = new RegistroContinuidadXml();
        registro.IdOperacion = (string?)elemento.Attribute("id") ?? string.Empty;
        registro.Tipo = (string?)elemento.Attribute("tipo") ?? string.Empty;
        registro.Estado = (string?)elemento.Attribute("estado") ?? string.Empty;
        registro.FechaInicioUtc = LeerFecha((string?)elemento.Attribute("fechaInicioUtc"));
        registro.FechaFinUtc = LeerFechaOpcional((string?)elemento.Attribute("fechaFinUtc"));
        registro.IdRespaldo = LeerEntero((string?)elemento.Element("Respaldo")?.Attribute("idSql"));
        registro.NombreArchivo = (string?)elemento.Element("Respaldo") ?? string.Empty;
        registro.IdUsuarioSolicitante = LeerEntero((string?)elemento.Element("UsuarioSolicitanteId"));
        registro.TamanoBytes = LeerLong((string?)elemento.Element("TamanoBytes"));
        registro.Verificado = LeerBooleano((string?)elemento.Element("Verificado"));
        registro.ArchivosConfiguracion = LeerEntero((string?)elemento.Element("ArchivosConfiguracion"));
        registro.Mensaje = (string?)elemento.Element("Mensaje");
        return registro;
    }

    private string RutaArchivo() => Path.GetFullPath(_configuracion.XmlHistoryFile);
    private static void EstablecerElemento(XElement elemento, string nombre, string? valor)
    {
        XElement destino = elemento.Element(nombre) ?? new XElement(nombre);
        if (destino.Parent is null) elemento.Add(destino);
        destino.SetValue(valor ?? string.Empty);
    }
    private static DateTime LeerFecha(string? valor) => DateTime.TryParse(valor, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime fecha) ? fecha : DateTime.MinValue;
    private static DateTime? LeerFechaOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : LeerFecha(valor);
    private static int? LeerEntero(string? valor) => int.TryParse(valor, out int numero) ? numero : null;
    private static long? LeerLong(string? valor) => long.TryParse(valor, out long numero) ? numero : null;
    private static bool? LeerBooleano(string? valor) => bool.TryParse(valor, out bool resultado) ? resultado : null;
}
