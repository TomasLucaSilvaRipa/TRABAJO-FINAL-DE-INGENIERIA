using System.Net.Mail;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLConsultaPlan
{
    private static readonly HashSet<string> Estados = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Pendiente", "En revisión", "Respondida", "Resuelta" };
    private readonly MPPConsultaPlan _consultaMPP;
    private readonly BLLPlanComercial _planBLL;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    public BLLConsultaPlan(MPPConsultaPlan consultaMPP, BLLPlanComercial planBLL, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _consultaMPP = consultaMPP;
        _planBLL = planBLL;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<ConsultaPlan> Consultar(PlanComercial planComercial)
    {
        try
        {
            _planBLL.ConsultarPlanDisponible(planComercial);
            List<ConsultaPlan> consultas = _consultaMPP.Consultar(planComercial);
            foreach (ConsultaPlan consulta in consultas)
            {
                consulta.Email = string.Empty;
            }
            return consultas;
        }
        catch (KeyNotFoundException ex) { throw new KeyNotFoundException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public ConsultaPlan Registrar(ConsultaPlan consulta)
    {
        try
        {
            PlanComercial planComercial = new PlanComercial(consulta.IdPlanComercial);
            planComercial = _planBLL.ConsultarPlanDisponible(planComercial);
            if (string.IsNullOrWhiteSpace(consulta.Nombre) || string.IsNullOrWhiteSpace(consulta.Email) || string.IsNullOrWhiteSpace(consulta.Consulta)) { throw new ArgumentException("Completá nombre, email y consulta."); }
            if (!MailAddress.TryCreate(consulta.Email.Trim(), out _)) { throw new ArgumentException("Ingresá un email válido."); }
            if (consulta.Nombre.Trim().Length < 2 || consulta.Consulta.Trim().Length < 5 || consulta.Consulta.Trim().Length > 1000) { throw new ArgumentException("La consulta debe tener entre 5 y 1000 caracteres."); }
            consulta.ID = 0;
            consulta.IdPlanComercial = planComercial.ID;
            consulta.Nombre = consulta.Nombre.Trim();
            consulta.Email = consulta.Email.Trim().ToLowerInvariant();
            consulta.Consulta = consulta.Consulta.Trim();
            consulta.Activo = true;
            ConsultaPlan resultado = _consultaMPP.Registrar(consulta);
            return resultado;
        }
        catch (KeyNotFoundException ex) { throw new KeyNotFoundException(ex.Message); }
        catch (ArgumentException ex) { throw new ArgumentException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<ConsultaPlan> ConsultarBandeja(Usuario usuario, ConsultaPlan filtro)
    {
        ExigirBandejaSoporte(usuario);
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && !Estados.Contains(filtro.Estado)) { throw new ArgumentException("El filtro de estado no es válido."); }
        return _consultaMPP.ConsultarBandeja(filtro);
    }

    public ConsultaPlan Responder(Usuario usuario, ConsultaPlan consulta)
    {
        ExigirBandejaSoporte(usuario);
        if (consulta.ID <= 0) { throw new ArgumentException("La consulta pública indicada no es válida."); }
        if (string.IsNullOrWhiteSpace(consulta.Respuesta) || consulta.Respuesta.Trim().Length > 4000) { throw new ArgumentException("Ingresá una respuesta de hasta 4000 caracteres."); }
        consulta.Respuesta = consulta.Respuesta.Trim();
        ConsultaPlan resultado = _consultaMPP.Responder(consulta, usuario);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "ConsultaPlan", resultado.ID, "ResponderConsultaPlan", "Soporte respondió una consulta pública sobre un plan.", "Exitoso", "Informacion", "Planes"));
        return resultado;
    }

    private static bool EsSoporte(Usuario usuario) { return usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase)); }
    private void ExigirBandejaSoporte(Usuario usuario) { if (!EsSoporte(usuario) || !_rolBLL.TienePermiso(usuario, "GestionarBandejaSoporte")) { throw new UnauthorizedAccessException("Esta bandeja sólo está disponible para operadores de Soporte autorizados."); } }
}
