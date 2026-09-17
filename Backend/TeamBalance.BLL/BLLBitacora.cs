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

    public List<Bitacora> LeerBitacora(FiltroBitacora filtro)
    {
        return _bitacoraMPP.LeerBitacora(filtro);
    }

    public List<Bitacora> FiltrarBitacora(FiltroBitacora filtro)
    {
        try
        {
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");
            }
            return _bitacoraMPP.Filtrar(filtro);
        }
        catch (Exception ex)
        {
            throw new Exception("Error al filtrar la bitácora: " + ex.Message, ex);
        }
    }
}
