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
        return _planMPP.ConsultarPlanes(true);
    }

    public List<PlanComercial> ConsultarPlanes()
    {
        return _planMPP.ConsultarPlanes(false);
    }

    public PlanComercial ConsultarPlan(int id)
    {
        return _planMPP.ConsultarPlan(id);
    }

    public PlanComercial ConsultarPlanDisponible(int id)
    {
        PlanComercial plan = _planMPP.ConsultarPlan(id);
        if (!plan.Activo){ throw new KeyNotFoundException("El plan comercial indicado no se encuentra disponible."); }
        return plan;
    }

    public PlanComercial RegistrarPlan(PlanComercial plan, Usuario usuario)
    {
        PrepararPlan(plan);
        PlanComercial resultado = _planMPP.RegistrarPlan(plan);
        RegistrarBitacora(resultado, usuario, "RegistrarPlan", "Se registró un nuevo plan comercial.");
        return resultado;
    }

    public PlanComercial ModificarPlan(PlanComercial plan, Usuario usuario)
    {
        if (plan.ID <= 0){ throw new ArgumentException("Seleccioná un plan válido."); }

        PrepararPlan(plan);
        PlanComercial resultado = _planMPP.ModificarPlan(plan);
        RegistrarBitacora(resultado, usuario, "ModificarPlan", "Se modificó un plan comercial.");
        return resultado;
    }

    public PlanComercial CambiarEstado(int id, bool activo, Usuario usuario)
    {
        PlanComercial resultado = _planMPP.CambiarEstado(id, activo);
        RegistrarBitacora(resultado, usuario, activo ? "ActivarPlan" : "DesactivarPlan", activo ? "Se activó un plan comercial." : "Se desactivó un plan comercial.");
        return resultado;
    }

    private static void PrepararPlan(PlanComercial plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Nombre) || string.IsNullOrWhiteSpace(plan.Periodicidad) || string.IsNullOrWhiteSpace(plan.Moneda)){ throw new ArgumentException("Completá los datos obligatorios del plan."); }
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

    private void RegistrarBitacora(PlanComercial plan, Usuario usuario, string accion, string mensaje)
    {
        Bitacora bitacora = new Bitacora(0, usuario.ID, usuario.IdAgencia, "PlanComercial", plan.ID, accion, mensaje, "Exitoso", "Informacion", "Planes", DateTime.Now, null);
        _bitacoraBLL.Add(bitacora);
    }
}
