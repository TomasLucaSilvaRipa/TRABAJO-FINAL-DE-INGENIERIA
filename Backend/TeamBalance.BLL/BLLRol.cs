using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRol
{
    private readonly MPPRol _rolMPP;

    public BLLRol(MPPRol rolMPP)
    {
        _rolMPP = rolMPP;
    }

    public Rol ConsultarRolPorNombre(string nombre)
    {
        return _rolMPP.ConsultarRolPorNombre(nombre);
    }
}
