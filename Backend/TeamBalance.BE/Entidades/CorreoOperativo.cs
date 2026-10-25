namespace TeamBalance.BE.Entidades;

public class PreferenciaCorreoOperativo
{
    public int IdUsuario { get; set; }
    public bool ResumenDiarioActivo { get; set; }
    public TimeSpan HoraResumenDiario { get; set; } = new(9, 0, 0);
    public bool AlertasDeadlineActivo { get; set; } = true;
    public bool ResumenSemanalActivo { get; set; } = true;
}

public class DestinatarioCorreoOperativo : PreferenciaCorreoOperativo
{
    public int? IdAgencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TipoUsuario { get; set; } = string.Empty;
}

public class TareaResumenCorreo
{
    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Prioridad { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public string? TokenEnProgreso { get; set; }
    public string? TokenFinalizada { get; set; }
}

public class AccionCorreoTarea
{
    public int IdUsuario { get; set; }
    public int IdTarea { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string TituloTarea { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public bool Disponible { get; set; }
}

public class ResumenSemanalCorreo
{
    public int ProyectosActivos { get; set; }
    public int TareasActivas { get; set; }
    public int TareasVencidas { get; set; }
    public int EmpleadosSobrecargados { get; set; }
    public decimal OcupacionPromedio { get; set; }
}
