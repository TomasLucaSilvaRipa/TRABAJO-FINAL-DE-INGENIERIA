using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRegistroHora
{
    private readonly MPPRegistroHora _registroHoraMPP;
    private readonly BLLRol _rolBLL;

    public BLLRegistroHora(MPPRegistroHora registroHoraMPP, BLLRol rolBLL)
    {
        _registroHoraMPP = registroHoraMPP;
        _rolBLL = rolBLL;
    }

    public RegistroHora Registrar(RegistroHora registro, Usuario usuario)
    {
        try
        {
            ValidarAcceso(usuario);
            if (registro.IdTarea <= 0 || registro.CantidadHoras <= 0 || registro.CantidadHoras > 24) { throw new ArgumentException("Indicá una tarea y una cantidad de horas válida."); }
            if (registro.Fecha.Date > DateTime.Today) { throw new ArgumentException("No podés registrar horas futuras."); }
            RegistroHora resultado = _registroHoraMPP.Registrar(registro, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RegistroHora> ConsultarPropios(Usuario usuario)
    {
        try
        {
            ValidarAcceso(usuario);
            List<RegistroHora> registros = _registroHoraMPP.ConsultarPropios(usuario);
            return registros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarAcceso(Usuario usuario)
    {
        if (!_rolBLL.TienePermiso(usuario, "RegistrarHoras")) { throw new UnauthorizedAccessException("No tenés permiso para registrar horas."); }
    }
}
