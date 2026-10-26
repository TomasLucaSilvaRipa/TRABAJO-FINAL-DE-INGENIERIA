namespace TeamBalance.BE.Entidades;
public class EstadoAutenticacionDosFactores
{
    public EstadoAutenticacionDosFactores()
    {
    }

    public EstadoAutenticacionDosFactores(bool totpHabilitado, DateTime? fechaActivacionTotp)
    {
        TotpHabilitado = totpHabilitado;
        FechaActivacionTotp = fechaActivacionTotp;
    }

    public bool TotpHabilitado { get; set; }
    public DateTime? FechaActivacionTotp { get; set; }
}
