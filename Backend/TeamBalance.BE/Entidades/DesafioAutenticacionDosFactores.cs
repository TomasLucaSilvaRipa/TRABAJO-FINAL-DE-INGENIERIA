using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class DesafioAutenticacionDosFactores
{
    public DesafioAutenticacionDosFactores()
    {
    }

    public DesafioAutenticacionDosFactores(bool valido, int? idUsuario, bool mantenerSesion)
    {
        Valido = valido;
        IdUsuario = idUsuario;
        MantenerSesion = mantenerSesion;
    }

    public bool Valido { get; set; }
    [JsonIgnore]
    public Usuario? Usuario { get; set; }

    public int? IdUsuario
    {
        get => Usuario?.ID;
        set => Usuario = value.HasValue ? (Usuario?.ID == value.Value ? Usuario : new Usuario { ID = value.Value }) : null;
    }
    public bool MantenerSesion { get; set; }
}
