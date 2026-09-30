namespace TeamBalance.BE.Entidades;

public class FiltroRiesgoRetraso
{
    public FiltroRiesgoRetraso()
    {
    }

    public FiltroRiesgoRetraso(int? idProyecto, int? idCliente, int? idPM, string? nivelRiesgo, int? diasHastaDeadlineMaximo)
    {
        IdProyecto = idProyecto;
        IdCliente = idCliente;
        IdPM = idPM;
        NivelRiesgo = nivelRiesgo;
        DiasHastaDeadlineMaximo = diasHastaDeadlineMaximo;
    }

    public int? IdProyecto { get; set; }

    public int? IdCliente { get; set; }

    public int? IdPM { get; set; }

    public string? NivelRiesgo { get; set; }

    public int? DiasHastaDeadlineMaximo { get; set; }
}
