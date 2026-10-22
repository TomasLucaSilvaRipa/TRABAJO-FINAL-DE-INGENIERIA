using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class RespuestaMensaje
{
    public RespuestaMensaje(string mensaje) => Mensaje = mensaje;
    public string Mensaje { get; set; }
}

public class RespuestaRegistroAgencia
{
    public RespuestaRegistroAgencia(string mensaje, string tokenGestion, DateTime fechaExpiracionValidacion)
    {
        Mensaje = mensaje;
        TokenGestion = tokenGestion;
        FechaExpiracionValidacion = fechaExpiracionValidacion;
    }

    public string Mensaje { get; set; }
    public string TokenGestion { get; set; }
    public DateTime FechaExpiracionValidacion { get; set; }
}

public class RespuestaCodigosRecuperacion
{
    public RespuestaCodigosRecuperacion(List<string> codigosRecuperacion) => CodigosRecuperacion = codigosRecuperacion;
    public List<string> CodigosRecuperacion { get; set; }
}

public class RespuestaVigencia
{
    public RespuestaVigencia(bool vigente) => Vigente = vigente;
    public bool Vigente { get; set; }
}

public class RespuestaSoporteInicial
{
    public RespuestaSoporteInicial(bool disponible) => Disponible = disponible;
    public bool Disponible { get; set; }
}

public class RespuestaFotoPerfil
{
    public RespuestaFotoPerfil(string? fotoPerfil) => FotoPerfil = fotoPerfil;
    public string? FotoPerfil { get; set; }
}

public class RespuestaConsultaSoporte
{
    public RespuestaConsultaSoporte(ConsultaSoporteResumen consulta, List<MensajeSoporteDetalle> mensajes)
    {
        Consulta = consulta;
        Mensajes = mensajes;
    }

    public ConsultaSoporteResumen Consulta { get; set; }
    public List<MensajeSoporteDetalle> Mensajes { get; set; }
}

public class RespuestaConsultaRespaldos
{
    public RespuestaConsultaRespaldos(List<RespaldoBaseDatos> respaldos, List<PruebaRestauracionRespaldo> pruebasRestauracion, List<RegistroContinuidadXml> historialXml, int rpoHoras, int rtoHoras, int retencionDias)
    {
        Respaldos = respaldos;
        PruebasRestauracion = pruebasRestauracion;
        HistorialXml = historialXml;
        RpoHoras = rpoHoras;
        RtoHoras = rtoHoras;
        RetencionDias = retencionDias;
    }

    public List<RespaldoBaseDatos> Respaldos { get; set; }
    public List<PruebaRestauracionRespaldo> PruebasRestauracion { get; set; }
    public List<RegistroContinuidadXml> HistorialXml { get; set; }
    public int RpoHoras { get; set; }
    public int RtoHoras { get; set; }
    public int RetencionDias { get; set; }
}

public class RespuestaSlack
{
    public RespuestaSlack(string tipoRespuesta, string texto)
    {
        TipoRespuesta = tipoRespuesta;
        Texto = texto;
    }

    [JsonPropertyName("response_type")]
    public string TipoRespuesta { get; set; }

    [JsonPropertyName("text")]
    public string Texto { get; set; }
}

public class RespuestaErrorSuscripcion
{
    public RespuestaErrorSuscripcion(string codigo, string mensaje)
    {
        Codigo = codigo;
        Mensaje = mensaje;
    }

    [JsonPropertyName("code")]
    public string Codigo { get; set; }

    [JsonPropertyName("message")]
    public string Mensaje { get; set; }
}

public class SolicitudEnvioTelegram
{
    public SolicitudEnvioTelegram(long chatId, string texto)
    {
        ChatId = chatId;
        Texto = texto;
    }

    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("text")]
    public string Texto { get; set; }
}

public class SolicitudEstadoMercadoPago
{
    public SolicitudEstadoMercadoPago(string estado) => Estado = estado;
    [JsonPropertyName("status")]
    public string Estado { get; set; }
}

public class SolicitudEvaluacionPassword
{
    public SolicitudEvaluacionPassword(string password) => Password = password;
    public string Password { get; set; }
}

public class ItemPreferenciaMercadoPago
{
    public ItemPreferenciaMercadoPago(string titulo, int cantidad, string moneda, decimal importeUnitario)
    {
        Titulo = titulo;
        Cantidad = cantidad;
        Moneda = moneda;
        ImporteUnitario = importeUnitario;
    }

    [JsonPropertyName("title")]
    public string Titulo { get; set; }
    [JsonPropertyName("quantity")]
    public int Cantidad { get; set; }
    [JsonPropertyName("currency_id")]
    public string Moneda { get; set; }
    [JsonPropertyName("unit_price")]
    public decimal ImporteUnitario { get; set; }
}

public class UrlsRetornoMercadoPago
{
    public UrlsRetornoMercadoPago(string exito, string pendiente, string fallo)
    {
        Exito = exito;
        Pendiente = pendiente;
        Fallo = fallo;
    }

    [JsonPropertyName("success")]
    public string Exito { get; set; }
    [JsonPropertyName("pending")]
    public string Pendiente { get; set; }
    [JsonPropertyName("failure")]
    public string Fallo { get; set; }
}

public class AsignacionTareaSincronizacion
{
    public AsignacionTareaSincronizacion(AsignacionTarea asignacion)
    {
        IdEmpleado = asignacion.IdEmpleado;
        HorasAsignadas = asignacion.HorasAsignadas;
        Motivo = string.IsNullOrWhiteSpace(asignacion.Motivo) ? null : asignacion.Motivo.Trim();
    }

    public int IdEmpleado { get; set; }
    public decimal HorasAsignadas { get; set; }
    public string? Motivo { get; set; }
}
