using System;
namespace TeamBalance.BE.Entidades;

public class FiltroBitacora
{
    public FiltroBitacora() { }

    public FiltroBitacora(int? idAgencia = null, DateTime? desde = null, DateTime? hasta = null, int? idUsuario = null, string? entidad = null, string? accion = null, ResultadoBitacora? resultado = null, CriticidadBitacora? criticidad = null, ModuloBitacora? modulo = null)
    {
        IdAgencia = idAgencia;
        Desde = desde;
        Hasta = hasta;
        IdUsuario = idUsuario;
        Entidad = entidad;
        Accion = accion;
        Resultado = resultado;
        Criticidad = criticidad;
        Modulo = modulo;
    }

    public int? IdAgencia { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? IdUsuario { get; set; }
    public string? Entidad { get; set; }
    public string? Accion { get; set; }
    public ResultadoBitacora? Resultado { get; set; }
    public CriticidadBitacora? Criticidad { get; set; }
    public ModuloBitacora? Modulo { get; set; }
}
