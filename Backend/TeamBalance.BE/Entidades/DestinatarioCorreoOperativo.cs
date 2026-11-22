using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class DestinatarioCorreoOperativo
{
    [JsonIgnore]
    public Usuario Usuario { get; set; } = new() { Nombre = string.Empty, Email = string.Empty, Rol = new Rol { TipoUsuario = string.Empty } };

    public string Nombre { get => Usuario.Nombre; set => Usuario.Nombre = value; }
    public string Email { get => Usuario.Email; set => Usuario.Email = value; }

    [JsonIgnore]
    public Agencia? Agencia { get; set; }

    [JsonIgnore]
    public PreferenciaCorreoOperativo Preferencia { get; set; } = new();

    public int IdUsuario { get => Usuario.ID; set => Usuario.ID = value; }
    public int? IdAgencia
    {
        get => Agencia?.ID;
        set => Agencia = value.HasValue ? new Agencia { ID = value.Value } : null;
    }

    public string TipoUsuario { get => Usuario.Rol.TipoUsuario; set => Usuario.Rol.TipoUsuario = value; }
    public bool ResumenDiarioActivo { get => Preferencia.ResumenDiarioActivo; set => Preferencia.ResumenDiarioActivo = value; }
    public TimeSpan HoraResumenDiario { get => Preferencia.HoraResumenDiario; set => Preferencia.HoraResumenDiario = value; }
    public bool AlertasDeadlineActivo { get => Preferencia.AlertasDeadlineActivo; set => Preferencia.AlertasDeadlineActivo = value; }
    public bool ResumenSemanalActivo { get => Preferencia.ResumenSemanalActivo; set => Preferencia.ResumenSemanalActivo = value; }
}
