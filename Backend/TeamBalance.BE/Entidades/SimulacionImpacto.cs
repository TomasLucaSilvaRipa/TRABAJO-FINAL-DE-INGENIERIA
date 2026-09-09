using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class SimulacionImpacto
{
    public SimulacionImpacto()
    {
    }

    public SimulacionImpacto(int iD, int idTarea, int idEmpleadoCandidato, int idUsuarioCreador, decimal? cargaActual, decimal? cargaProyectada, decimal? disponibilidadRestante, decimal? porcentajeOcupacionActual, decimal? porcentajeOcupacionProyectado, bool generaSobrecarga, string? advertenciasJson, string? impactoOperativo, DateTime fechaCreacion, DateTime? fechaUltimaModificacion, DateTime fechaExpiracion, bool activo)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleadoCandidato = idEmpleadoCandidato;
        IdUsuarioCreador = idUsuarioCreador;
        CargaActual = cargaActual;
        CargaProyectada = cargaProyectada;
        DisponibilidadRestante = disponibilidadRestante;
        PorcentajeOcupacionActual = porcentajeOcupacionActual;
        PorcentajeOcupacionProyectado = porcentajeOcupacionProyectado;
        GeneraSobrecarga = generaSobrecarga;
        AdvertenciasJson = advertenciasJson;
        ImpactoOperativo = impactoOperativo;
        FechaCreacion = fechaCreacion;
        FechaUltimaModificacion = fechaUltimaModificacion;
        FechaExpiracion = fechaExpiracion;
        Activo = activo;
    }

    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleadoCandidato { get; set; }

    public int IdUsuarioCreador { get; set; }

    public decimal? CargaActual { get; set; }

    public decimal? CargaProyectada { get; set; }

    public decimal? DisponibilidadRestante { get; set; }

    public decimal? PorcentajeOcupacionActual { get; set; }

    public decimal? PorcentajeOcupacionProyectado { get; set; }

    public bool GeneraSobrecarga { get; set; }

    public string? AdvertenciasJson { get; set; }

    public string? ImpactoOperativo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Activo { get; set; }
}
