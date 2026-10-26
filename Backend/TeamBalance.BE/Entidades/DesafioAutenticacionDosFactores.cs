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
    public int? IdUsuario { get; set; }
    public bool MantenerSesion { get; set; }
}
