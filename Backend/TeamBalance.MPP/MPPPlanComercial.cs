using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPPlanComercial
{
    private readonly Conexion _conexion;

    public MPPPlanComercial(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<PlanComercial> ConsultarPlanes(bool soloActivos)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@SoloActivos", soloActivos),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_PlanComercial_Consultar", parametros);
        List<PlanComercial> planes = new List<PlanComercial>();

        foreach (DataRow fila in tabla.Rows)
        {
            planes.Add(CrearPlan(fila));
        }

        return planes;
    }

    public PlanComercial ConsultarPlan(int id)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ID", id),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_PlanComercial_ConsultarPorId", parametros);

        if (tabla.Rows.Count != 1){ throw new KeyNotFoundException("No existe el plan comercial indicado."); }

        return CrearPlan(tabla.Rows[0]);
    }

    public PlanComercial RegistrarPlan(PlanComercial plan)
    {
        List<SqlParameter> parametros = CrearParametros(plan);
        DataTable tabla = _conexion.Leer("dbo.usp_PlanComercial_Registrar", parametros);

        if (tabla.Rows.Count != 1){ throw new InvalidOperationException("No fue posible registrar el plan comercial."); }

        return CrearPlan(tabla.Rows[0]);
    }

    public PlanComercial ModificarPlan(PlanComercial plan)
    {
        List<SqlParameter> parametros = CrearParametros(plan);
        parametros.Insert(0, new SqlParameter("@ID", plan.ID));
        DataTable tabla = _conexion.Leer("dbo.usp_PlanComercial_Modificar", parametros);

        if (tabla.Rows.Count != 1){ throw new InvalidOperationException("No fue posible modificar el plan comercial."); }

        return CrearPlan(tabla.Rows[0]);
    }

    public PlanComercial CambiarEstado(int id, bool activo)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ID", id),
            new SqlParameter("@Activo", activo),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_PlanComercial_CambiarEstado", parametros);

        if (tabla.Rows.Count != 1){ throw new KeyNotFoundException("No existe el plan comercial indicado."); }

        return CrearPlan(tabla.Rows[0]);
    }

    private static List<SqlParameter> CrearParametros(PlanComercial plan)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Nombre", plan.Nombre),
            new SqlParameter("@Descripcion", (object?)plan.Descripcion ?? DBNull.Value),
            new SqlParameter("@Periodicidad", plan.Periodicidad),
            new SqlParameter("@PrecioVigente", plan.PrecioVigente),
            new SqlParameter("@Moneda", plan.Moneda),
            new SqlParameter("@DuracionMeses", plan.DuracionMeses),
            new SqlParameter("@AlcanceFuncional", (object?)plan.AlcanceFuncional ?? DBNull.Value),
            new SqlParameter("@CondicionesRenovacion", (object?)plan.CondicionesRenovacion ?? DBNull.Value),
            new SqlParameter("@Activo", plan.Activo),
            new SqlParameter("@FechaVigenciaDesde", plan.FechaVigenciaDesde),
            new SqlParameter("@FechaVigenciaHasta", (object?)plan.FechaVigenciaHasta ?? DBNull.Value),
        };
        return parametros;
    }

    private static PlanComercial CrearPlan(DataRow fila)
    {
        DateTime? fechaVigenciaHasta = fila["FechaVigenciaHasta"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaVigenciaHasta"]);
        PlanComercial plan = new PlanComercial(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), Convert.ToString(fila["Periodicidad"]) ?? string.Empty, Convert.ToDecimal(fila["PrecioVigente"]), Convert.ToString(fila["Moneda"]) ?? string.Empty, Convert.ToInt32(fila["DuracionMeses"]), Convert.ToString(fila["AlcanceFuncional"]), Convert.ToString(fila["CondicionesRenovacion"]), Convert.ToBoolean(fila["Activo"]), Convert.ToDateTime(fila["FechaVigenciaDesde"]), fechaVigenciaHasta);
        return plan;
    }
}
