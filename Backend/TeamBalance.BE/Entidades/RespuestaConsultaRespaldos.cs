using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaConsultaRespaldos
{
    public RespuestaConsultaRespaldos(List<RespaldoBaseDatos> respaldos, List<PruebaRestauracionRespaldo> pruebasRestauracion, List<RegistroContinuidadXml> historialXml, int rpoHoras, int rtoHoras, int retencionDias)
    {
        Respaldos = respaldos;
        PruebasRestauracion = pruebasRestauracion;
        HistorialXml = historialXml;
        RpoHoras = rpoHoras;
        RtoHoras = rtoHoras;
        RetencionDias = retencionDias;
    }

    public List<RespaldoBaseDatos> Respaldos { get; set; }
    public List<PruebaRestauracionRespaldo> PruebasRestauracion { get; set; }
    public List<RegistroContinuidadXml> HistorialXml { get; set; }
    public int RpoHoras { get; set; }
    public int RtoHoras { get; set; }
    public int RetencionDias { get; set; }
}
