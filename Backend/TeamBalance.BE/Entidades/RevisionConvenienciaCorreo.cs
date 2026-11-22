using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class RevisionConvenienciaCorreo
{
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    [JsonIgnore, ReferenciaInterna]
    public Cliente Cliente { get; set; } = new() { Nombre = string.Empty };

    [JsonIgnore, ReferenciaInterna]
    public List<Proyecto> ProyectosRelacionados { get; set; } = new();

    public string NombreCliente
    {
        get => Cliente.Nombre ?? string.Empty;
        set => Cliente.Nombre = value;
    }

    public List<string> Proyectos
    {
        get => ProyectosRelacionados.Select(proyecto => proyecto.Nombre).ToList();
        set => ProyectosRelacionados = value.Select(nombre => new Proyecto { Nombre = nombre }).ToList();
    }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
