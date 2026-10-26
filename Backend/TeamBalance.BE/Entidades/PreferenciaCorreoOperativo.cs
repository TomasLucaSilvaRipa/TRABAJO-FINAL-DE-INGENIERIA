namespace TeamBalance.BE.Entidades;
public class PreferenciaCorreoOperativo
{
    public int IdUsuario { get; set; }
    public bool ResumenDiarioActivo { get; set; }
    public TimeSpan HoraResumenDiario { get; set; } = new(9, 0, 0);
    public bool AlertasDeadlineActivo { get; set; } = true;
    public bool ResumenSemanalActivo { get; set; } = true;
}
