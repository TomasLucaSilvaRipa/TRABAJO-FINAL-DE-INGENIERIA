using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PlantillaTarea
{
    public PlantillaTarea()
    {
    }

    public PlantillaTarea(int iD, int idAgencia, int? idSkillRequerido, string nombre, string? tituloSugerido, string? descripcionBase, decimal? horasEstimadas, string? complejidad, string? prioridadSugerida, string? skillRequerido, string? seniorityRecomendado, string estado, bool activo, DateTime fechaCreacion, DateTime? fechaBaja, string? checklistBaseJson, string? archivosAdjuntosJson)
    {
        ID = iD;
        IdAgencia = idAgencia;
        IdSkillRequerido = idSkillRequerido;
        Nombre = nombre;
        TituloSugerido = tituloSugerido;
        DescripcionBase = descripcionBase;
        HorasEstimadas = horasEstimadas;
        Complejidad = complejidad;
        PrioridadSugerida = prioridadSugerida;
        SkillRequerido = skillRequerido;
        SeniorityRecomendado = seniorityRecomendado;
        Estado = estado;
        Activo = activo;
        FechaCreacion = fechaCreacion;
        FechaBaja = fechaBaja;
        ChecklistBaseJson = checklistBaseJson;
        ArchivosAdjuntosJson = archivosAdjuntosJson;
    }

    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public int? IdSkillRequerido { get; set; }

    public string Nombre { get; set; } = null!;

    public string? TituloSugerido { get; set; }

    public string? DescripcionBase { get; set; }

    public decimal? HorasEstimadas { get; set; }

    public string? Complejidad { get; set; }

    public string? PrioridadSugerida { get; set; }

    public string? SkillRequerido { get; set; }

    public string? SeniorityRecomendado { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? ChecklistBaseJson { get; set; }

    public string? ArchivosAdjuntosJson { get; set; }
}
