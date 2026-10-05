using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRegistroHora
{
    private const int DiasMaximosRetroactivos = 30;
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
            List<RegistroHora> resultados = RegistrarImputaciones(new List<RegistroHora>() { registro }, usuario);
            return resultados[0];
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (ArgumentException ex) { throw new ArgumentException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RegistroHora> Previsualizar(List<RegistroHora> registros, Usuario usuario)
    {
        try
        {
            ValidarRegistros(registros, usuario);
            return _registroHoraMPP.Previsualizar(registros, usuario);
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (ArgumentException ex) { throw new ArgumentException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RegistroHora> RegistrarImputaciones(List<RegistroHora> registros, Usuario usuario)
    {
        try
        {
            ValidarRegistros(registros, usuario);
            return _registroHoraMPP.RegistrarImputaciones(registros, usuario);
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (ArgumentException ex) { throw new ArgumentException(ex.Message); }
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

    private void ValidarRegistros(List<RegistroHora> registros, Usuario usuario)
    {
        ValidarAcceso(usuario);
        if (registros is null || registros.Count == 0) { throw new ArgumentException("Agregá al menos una tarea para registrar horas."); }
        DateTime fecha = registros[0].Fecha.Date;
        if (fecha > DateTime.Today) { throw new ArgumentException("No podés registrar horas futuras."); }
        if (fecha < DateTime.Today.AddDays(-DiasMaximosRetroactivos)) { throw new ArgumentException($"Solo podés registrar horas de los últimos {DiasMaximosRetroactivos} días."); }
        if (registros.Any(registro => registro.Fecha.Date != fecha)) { throw new ArgumentException("Todas las horas de una misma carga deben corresponder a la misma fecha."); }
        if (registros.Any(registro => registro.IdTarea <= 0 || registro.CantidadHoras < 0.25m || registro.CantidadHoras > 24 || string.IsNullOrWhiteSpace(registro.Descripcion))) { throw new ArgumentException("Completá una tarea, horas válidas y la descripción del avance para cada registro."); }
        if (registros.Sum(registro => registro.CantidadHoras) > 24) { throw new ArgumentException("El total de horas de la jornada no puede superar 24 horas."); }
    }
}
