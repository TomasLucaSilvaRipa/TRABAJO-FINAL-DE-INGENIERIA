using System.Net.Mail;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLConsultaPlan
{
    private readonly MPPConsultaPlan _consultaMPP;
    private readonly BLLPlanComercial _planBLL;
    public BLLConsultaPlan(MPPConsultaPlan consultaMPP, BLLPlanComercial planBLL)
    {
        _consultaMPP = consultaMPP;
        _planBLL = planBLL;
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
}
