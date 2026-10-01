using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLPlanComercial
{
    private readonly MPPPlanComercial _planMPP;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLPlanComercial(MPPPlanComercial planMPP, BLLBitacora bitacoraBLL)
    {
        _planMPP = planMPP;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<PlanComercial> ConsultarPlanesActivos()
    {
        try
        {
            List<PlanComercial> planes = _planMPP.ConsultarPlanes(true);
            return planes;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<PlanComercial> ConsultarPlanes()
    {
        try
        {
            List<PlanComercial> planes = _planMPP.ConsultarPlanes(false);
            return planes;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public PlanComercial ConsultarPlan(PlanComercial planComercial)
    {
        try
        {
            PlanComercial plan = new PlanComercial();
            return plan;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public PlanComercial ConsultarPlanDisponible(PlanComercial planComercial)
    {
        try
        {
            PlanComercial plan = _planMPP.ConsultarPlan(planComercial);
            if (!plan.Activo) { throw new KeyNotFoundException("El plan comercial indicado no se encuentra disponible."); }
            return plan;
        }
        catch (KeyNotFoundException ex) { throw new KeyNotFoundException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public PlanComercial RegistrarPlan(PlanComercial plan, Usuario usuario)
    {
        try
        {
            PrepararPlan(plan);
            PlanComercial resultado = _planMPP.RegistrarPlan(plan);
            Bitacora bitacora = new Bitacora("RegistrarPlan", "Se registró un nuevo plan comercial.");
            RegistrarBitacora(resultado, usuario, bitacora);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public PlanComercial ModificarPlan(PlanComercial plan, Usuario usuario)
    {
        try
        {
            if (plan.ID <= 0) { throw new ArgumentException("Seleccioná un plan válido."); }
            PrepararPlan(plan);
            PlanComercial resultado = _planMPP.ModificarPlan(plan);
            Bitacora bitacora = new Bitacora("ModificarPlan", "Se modificó un plan comercial.");
            RegistrarBitacora(resultado, usuario, bitacora);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public PlanComercial CambiarEstado(PlanComercial planComercial, Usuario usuario)
    {
        try
        {
            PlanComercial resultado = _planMPP.CambiarEstado(planComercial);
            Bitacora bitacora = new Bitacora(planComercial.Activo ? "ActivarPlan" : "DesactivarPlan", planComercial.Activo ? "Se activó un plan comercial." : "Se desactivó un plan comercial.");
            RegistrarBitacora(resultado, usuario, bitacora);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static void PrepararPlan(PlanComercial plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Nombre) || string.IsNullOrWhiteSpace(plan.Periodicidad) || string.IsNullOrWhiteSpace(plan.Moneda)){ 
            throw new ArgumentException("Completá los datos obligatorios del plan."); 
        }
        if (plan.PrecioVigente <= 0){ throw new ArgumentException("El precio del plan debe ser mayor a cero."); }
        if (plan.DuracionMeses <= 0){ throw new ArgumentException("La duración del plan debe ser mayor a cero."); }

        plan.Nombre = plan.Nombre.Trim();
        plan.Descripcion = plan.Descripcion?.Trim();
        plan.Periodicidad = plan.Periodicidad.Trim();
        plan.Moneda = plan.Moneda.Trim().ToUpperInvariant();
        plan.AlcanceFuncional = plan.AlcanceFuncional?.Trim();
        plan.CondicionesRenovacion = plan.CondicionesRenovacion?.Trim();
        plan.FechaVigenciaDesde = plan.FechaVigenciaDesde == default ? DateTime.Now : plan.FechaVigenciaDesde;
    }

    private void RegistrarBitacora(PlanComercial plan, Usuario usuario, Bitacora bitacora)
    {
        bitacora = new Bitacora(0, usuario.ID, usuario.IdAgencia, "PlanComercial", plan.ID, bitacora.Accion, bitacora.Mensaje, "Exitoso", "Informacion", "Planes", DateTime.Now, null);
        _bitacoraBLL.Add(bitacora);
    }
}
