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

public enum ModuloBitacora
{
    General = 1,
    Contratacion = 2,
    PreguntasFrecuentes = 3,
    HelpDesk = 4,
    Kanban = 5,
    Novedades = 6,
    Planes = 7,
    Planificacion = 8,
    Proyectos = 9,
    Recursos = 10,
    Registro = 11,
    Respaldos = 12,
    Seguridad = 13,
    Suscripcion = 14,
    Usuarios = 15,
    Operadores = 16,
    Encuestas = 17
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

    [JsonIgnore]
    public string? Modulo { get; set; }

    [NotMapped]
    [JsonPropertyName("modulo")]
    public ModuloBitacora? ModuloEnum
    {
        get { return ObtenerModulo(Modulo); }
        set { Modulo = value.HasValue ? TextoModulo(value.Value) : null; }
    }

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

    public static ModuloBitacora? ObtenerModulo(string? modulo)
    {
        if (string.IsNullOrWhiteSpace(modulo)) { return null; }
        if (string.Equals(modulo, "General", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.General; }
        if (string.Equals(modulo, "Contratacion", StringComparison.OrdinalIgnoreCase) || string.Equals(modulo, "Contratación", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Contratacion; }
        if (string.Equals(modulo, "FAQs", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.PreguntasFrecuentes; }
        if (string.Equals(modulo, "HelpDesk", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.HelpDesk; }
        if (string.Equals(modulo, "Kanban", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Kanban; }
        if (string.Equals(modulo, "Novedades", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Novedades; }
        if (string.Equals(modulo, "Planes", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Planes; }
        if (string.Equals(modulo, "Planificacion", StringComparison.OrdinalIgnoreCase) || string.Equals(modulo, "Planificación", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Planificacion; }
        if (string.Equals(modulo, "Proyectos", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Proyectos; }
        if (string.Equals(modulo, "Recursos", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Recursos; }
        if (string.Equals(modulo, "Registro", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Registro; }
        if (string.Equals(modulo, "Respaldos", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Respaldos; }
        if (string.Equals(modulo, "Seguridad", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Seguridad; }
        if (string.Equals(modulo, "Suscripcion", StringComparison.OrdinalIgnoreCase) || string.Equals(modulo, "Suscripción", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Suscripcion; }
        if (string.Equals(modulo, "Usuarios", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Usuarios; }
        if (string.Equals(modulo, "Operadores", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Operadores; }
        if (string.Equals(modulo, "Encuestas", StringComparison.OrdinalIgnoreCase)) { return ModuloBitacora.Encuestas; }
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

    public static string TextoModulo(ModuloBitacora modulo)
    {
        return modulo switch
        {
            ModuloBitacora.General => "General",
            ModuloBitacora.Contratacion => "Contratacion",
            ModuloBitacora.PreguntasFrecuentes => "FAQs",
            ModuloBitacora.HelpDesk => "HelpDesk",
            ModuloBitacora.Kanban => "Kanban",
            ModuloBitacora.Novedades => "Novedades",
            ModuloBitacora.Planes => "Planes",
            ModuloBitacora.Planificacion => "Planificación",
            ModuloBitacora.Proyectos => "Proyectos",
            ModuloBitacora.Recursos => "Recursos",
            ModuloBitacora.Registro => "Registro",
            ModuloBitacora.Respaldos => "Respaldos",
            ModuloBitacora.Seguridad => "Seguridad",
            ModuloBitacora.Suscripcion => "Suscripción",
            ModuloBitacora.Usuarios => "Usuarios",
            ModuloBitacora.Operadores => "Operadores",
            ModuloBitacora.Encuestas => "Encuestas",
            _ => throw new ArgumentOutOfRangeException(nameof(modulo))
        };
    }
}
