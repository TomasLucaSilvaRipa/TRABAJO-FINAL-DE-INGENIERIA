using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class PreferenciaCorreoOperativo
{
    [JsonIgnore, ReferenciaInterna]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value };
    }
    public bool ResumenDiarioActivo { get; set; }
    public TimeSpan HoraResumenDiario { get; set; } = new(9, 0, 0);
    public bool AlertasDeadlineActivo { get; set; } = true;
    public bool ResumenSemanalActivo { get; set; } = true;
}
