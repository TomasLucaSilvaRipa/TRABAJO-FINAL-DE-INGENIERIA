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
        Usuario = idUsuario is int id ? new Usuario(id) : null;
        MantenerSesion = mantenerSesion;
    }

    public bool Valido { get; set; }
    [JsonIgnore]
    public Usuario? Usuario { get; set; }

    public bool MantenerSesion { get; set; }
}
