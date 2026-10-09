using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public enum ResultadoBitacora
{
    Pendiente = 1,
    Exitoso = 2,
    Parcial = 3,
    Denegado = 4,
    Error = 5
}

public enum CriticidadBitacora
{
    Informacion = 1,
    Advertencia = 2,
    Critico = 3
}

public partial class Bitacora
{
    public Bitacora() { }

    public Bitacora(string accion, string mensaje)
    {
        Accion = accion;
        Mensaje = mensaje;
        FechaHora = DateTime.Now;
    }
    public Bitacora(int? idUsuario, int? idAgencia, string? entidad, int? idEntidad, string accion, string mensaje, string? resultado, string? criticidad, string? modulo)
    {
        IdUsuario = idUsuario;
        IdAgencia = idAgencia;
        Entidad = entidad;
        IdEntidad = idEntidad;
        Accion = accion;
        Mensaje = mensaje;
        Resultado = resultado;
        Criticidad = criticidad;
        Modulo = modulo;
        FechaHora = DateTime.Now;
    }

    public Bitacora(int id, int? idUsuario, int? idAgencia, string? entidad, int? idEntidad, string accion, string mensaje, string? resultado, string? criticidad, string? modulo, DateTime fechaHora, string? direccionIP)
    {
        ID = id;
        IdUsuario = idUsuario;
        IdAgencia = idAgencia;
        Entidad = entidad;
        IdEntidad = idEntidad;
        Accion = accion;
        Mensaje = mensaje;
        Resultado = resultado;
        Criticidad = criticidad;
        Modulo = modulo;
        FechaHora = fechaHora;
        DireccionIP = direccionIP;
    }
    public int ID { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdAgencia { get; set; }

    public string? Entidad { get; set; }

    public int? IdEntidad { get; set; }

    public string Accion { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    [JsonIgnore]
    public string? Resultado { get; set; }

    [JsonIgnore]
    public string? Criticidad { get; set; }

    [NotMapped]
    [JsonPropertyName("resultado")]
    public ResultadoBitacora? ResultadoEnum
    {
        get { return ObtenerResultado(Resultado); }
        set { Resultado = value.HasValue ? TextoResultado(value.Value) : null; }
    }

    [NotMapped]
    [JsonPropertyName("criticidad")]
    public CriticidadBitacora CriticidadEnum
    {
        get { return ObtenerCriticidad(Criticidad) ?? CriticidadBitacora.Informacion; }
        set { Criticidad = TextoCriticidad(value); }
    }

    public string? Modulo { get; set; }

    public DateTime FechaHora { get; set; }

    public string? DireccionIP { get; set; }

    public static ResultadoBitacora? ObtenerResultado(string? resultado)
    {
        if (string.IsNullOrWhiteSpace(resultado)) { return null; }
        if (string.Equals(resultado, "Pendiente", StringComparison.OrdinalIgnoreCase)) { return ResultadoBitacora.Pendiente; }
        if (string.Equals(resultado, "Exitoso", StringComparison.OrdinalIgnoreCase)) { return ResultadoBitacora.Exitoso; }
        if (string.Equals(resultado, "Parcial", StringComparison.OrdinalIgnoreCase)) { return ResultadoBitacora.Parcial; }
        if (string.Equals(resultado, "Denegado", StringComparison.OrdinalIgnoreCase)) { return ResultadoBitacora.Denegado; }
        if (string.Equals(resultado, "Error", StringComparison.OrdinalIgnoreCase) || string.Equals(resultado, "Fallido", StringComparison.OrdinalIgnoreCase)) { return ResultadoBitacora.Error; }
        return null;
    }

    public static CriticidadBitacora? ObtenerCriticidad(string? criticidad)
    {
        if (string.IsNullOrWhiteSpace(criticidad)) { return null; }
        if (string.Equals(criticidad, "Informacion", StringComparison.OrdinalIgnoreCase) || string.Equals(criticidad, "Información", StringComparison.OrdinalIgnoreCase)) { return CriticidadBitacora.Informacion; }
        if (string.Equals(criticidad, "Advertencia", StringComparison.OrdinalIgnoreCase)) { return CriticidadBitacora.Advertencia; }
        if (string.Equals(criticidad, "Critico", StringComparison.OrdinalIgnoreCase) || string.Equals(criticidad, "Crítico", StringComparison.OrdinalIgnoreCase)) { return CriticidadBitacora.Critico; }
        return null;
    }

    public static string TextoResultado(ResultadoBitacora resultado)
    {
        return resultado switch
        {
            ResultadoBitacora.Pendiente => "Pendiente",
            ResultadoBitacora.Exitoso => "Exitoso",
            ResultadoBitacora.Parcial => "Parcial",
            ResultadoBitacora.Denegado => "Denegado",
            ResultadoBitacora.Error => "Error",
            _ => throw new ArgumentOutOfRangeException(nameof(resultado))
        };
    }

    public static string TextoCriticidad(CriticidadBitacora criticidad)
    {
        return criticidad switch
        {
            CriticidadBitacora.Informacion => "Informacion",
            CriticidadBitacora.Advertencia => "Advertencia",
            CriticidadBitacora.Critico => "Critico",
            _ => throw new ArgumentOutOfRangeException(nameof(criticidad))
        };
    }
}
