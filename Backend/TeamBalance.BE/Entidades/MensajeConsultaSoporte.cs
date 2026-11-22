using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class MensajeConsultaSoporte
{
    public MensajeConsultaSoporte()
    {
    }

    public MensajeConsultaSoporte(int iD, int idConsultaSoporte, int idUsuario, string mensaje, string? adjuntosJson, DateTime fecha, bool activo)
    {
        ID = iD;
        IdConsultaSoporte = idConsultaSoporte;
        IdUsuario = idUsuario;
        Mensaje = mensaje;
        AdjuntosJson = adjuntosJson;
        Fecha = fecha;
        Activo = activo;
    }

    public int ID { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public ConsultaSoporte Consulta { get; set; } = new();

    public int IdConsultaSoporte
    {
        get => Consulta.ID;
        set => Consulta = Consulta.ID == value ? Consulta : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value };
    }

    public string Mensaje { get; set; } = null!;

    public string? AdjuntosJson { get; set; }

    public DateTime Fecha { get; set; }

    public bool Activo { get; set; }
}
