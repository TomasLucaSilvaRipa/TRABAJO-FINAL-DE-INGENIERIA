namespace TeamBalance.BE.Entidades;

public class RegistroAgenciaResultado
{
    public RegistroAgenciaResultado()
    {
    }

    public RegistroAgenciaResultado(int idAgencia, int idUsuario)
    {
        IdAgencia = idAgencia;
        IdUsuario = idUsuario;
    }

    public int IdAgencia { get; set; }
    public int IdUsuario { get; set; }
}
