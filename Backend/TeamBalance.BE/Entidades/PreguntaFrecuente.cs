namespace TeamBalance.BE.Entidades;

public enum CategoriaPreguntaFrecuente
{
    PrimerosPasos = 1,
    ProyectosYTareas = 2,
    EquipoYDisponibilidad = 3,
    CuentaYSuscripcion = 4,
    General = 5
}

public sealed class PreguntaFrecuente
{
    public int ID { get; set; }
    public CategoriaPreguntaFrecuente Categoria { get; set; }
    public string PreguntaEs { get; set; } = string.Empty;
    public string RespuestaEs { get; set; } = string.Empty;
    public string PreguntaEn { get; set; } = string.Empty;
    public string RespuestaEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; }
}
