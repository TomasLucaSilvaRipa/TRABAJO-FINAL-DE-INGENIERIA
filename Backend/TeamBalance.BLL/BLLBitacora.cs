using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLBitacora
{
    private readonly MPPBitacora _bitacoraMPP;

    public BLLBitacora(MPPBitacora bitacoraMPP)
    {
        _bitacoraMPP = bitacoraMPP;
    }

    public bool Add(Bitacora bitacora)
    {
        if (string.IsNullOrWhiteSpace(bitacora.Accion) || string.IsNullOrWhiteSpace(bitacora.Mensaje))
        {
            return false;
        }

        bitacora.Accion = bitacora.Accion.Trim();
        bitacora.Mensaje = bitacora.Mensaje.Trim();
        bitacora.Entidad = string.IsNullOrWhiteSpace(bitacora.Entidad) ? null : bitacora.Entidad.Trim();
        bitacora.Resultado = string.IsNullOrWhiteSpace(bitacora.Resultado) ? null : bitacora.Resultado.Trim();
        bitacora.Criticidad = string.IsNullOrWhiteSpace(bitacora.Criticidad) ? "Informacion" : bitacora.Criticidad.Trim();
        bitacora.Modulo = string.IsNullOrWhiteSpace(bitacora.Modulo) ? "General" : bitacora.Modulo.Trim();
        bitacora.DireccionIP = string.IsNullOrWhiteSpace(bitacora.DireccionIP) ? null : bitacora.DireccionIP.Trim();
        bitacora.FechaHora = bitacora.FechaHora == default ? DateTime.Now : bitacora.FechaHora;

        return _bitacoraMPP.Add(bitacora);
    }

    public List<Bitacora> LeerBitacora(int? idAgencia = null)
    {
        return _bitacoraMPP.LeerBitacora(idAgencia);
    }

    public List<Bitacora> FiltrarBitacora(int? idAgencia, DateTime? desde = null, DateTime? hasta = null, int? idUsuario = null, string? entidad = null, string? accion = null, string? resultado = null, string? criticidad = null, string? modulo = null)
    {
        return _bitacoraMPP.Filtrar(idAgencia, desde, hasta, idUsuario, entidad, accion, resultado, criticidad, modulo);
    }
}
