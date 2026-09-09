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
            new("@SoloActivos", soloActivos),
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
            new("@ID", id),
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
            new("@ID", id),
            new("@Activo", activo),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_PlanComercial_CambiarEstado", parametros);

        if (tabla.Rows.Count != 1){ throw new KeyNotFoundException("No existe el plan comercial indicado."); }

        return CrearPlan(tabla.Rows[0]);
    }

    private static List<SqlParameter> CrearParametros(PlanComercial plan)
    {
        return new List<SqlParameter>()
        {
            new("@Nombre", plan.Nombre),
            new("@Descripcion", (object?)plan.Descripcion ?? DBNull.Value),
            new("@Periodicidad", plan.Periodicidad),
            new("@PrecioVigente", plan.PrecioVigente),
            new("@Moneda", plan.Moneda),
            new("@DuracionMeses", plan.DuracionMeses),
            new("@AlcanceFuncional", (object?)plan.AlcanceFuncional ?? DBNull.Value),
            new("@CondicionesRenovacion", (object?)plan.CondicionesRenovacion ?? DBNull.Value),
            new("@Activo", plan.Activo),
            new("@FechaVigenciaDesde", plan.FechaVigenciaDesde),
            new("@FechaVigenciaHasta", (object?)plan.FechaVigenciaHasta ?? DBNull.Value),
        };
    }

    private static PlanComercial CrearPlan(DataRow fila)
    {
        return new PlanComercial()
        {
            ID = Convert.ToInt32(fila["ID"]),
            Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty,
            Descripcion = Convert.ToString(fila["Descripcion"]),
            Periodicidad = Convert.ToString(fila["Periodicidad"]) ?? string.Empty,
            PrecioVigente = Convert.ToDecimal(fila["PrecioVigente"]),
            Moneda = Convert.ToString(fila["Moneda"]) ?? string.Empty,
            DuracionMeses = Convert.ToInt32(fila["DuracionMeses"]),
            AlcanceFuncional = Convert.ToString(fila["AlcanceFuncional"]),
            CondicionesRenovacion = Convert.ToString(fila["CondicionesRenovacion"]),
            Activo = Convert.ToBoolean(fila["Activo"]),
            FechaVigenciaDesde = Convert.ToDateTime(fila["FechaVigenciaDesde"]),
            FechaVigenciaHasta = fila["FechaVigenciaHasta"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaVigenciaHasta"]),
        };
    }
}
