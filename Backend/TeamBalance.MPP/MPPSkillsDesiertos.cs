using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPSkillsDesiertos
{
    private readonly Conexion _conexion;
    public MPPSkillsDesiertos(Conexion conexion) { _conexion = conexion; }

    public List<TareaCoberturaSkill> ConsultarTareas(FiltroCoberturaSkill filtro, Usuario usuario)
    {
        List<SqlParameter> parametros = CrearParametros(filtro, usuario);
        DataTable tabla = _conexion.Leer("dbo.usp_SkillsDesiertos_ConsultarTareas", parametros);
        return tabla.Rows.Cast<DataRow>().Select(fila => new TareaCoberturaSkill(Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdSkill"]), Convert.ToString(fila["NombreSkill"]) ?? string.Empty, fila["CategoriaSkill"] == DBNull.Value ? null : Convert.ToString(fila["CategoriaSkill"]), Convert.ToString(fila["TituloTarea"]) ?? string.Empty, Convert.ToDecimal(fila["HorasEstimadas"]), Convert.ToDateTime(fila["Deadline"]), Convert.ToInt32(fila["IdProyecto"]), Convert.ToString(fila["NombreProyecto"]) ?? string.Empty, Convert.ToString(fila["NombreCliente"]) ?? string.Empty)).ToList();
    }

    public List<EmpleadoCoberturaSkill> ConsultarEmpleados(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter> { new SqlParameter("@IdAgencia", usuario.IdAgencia) };
        DataTable tabla = _conexion.Leer("dbo.usp_SkillsDesiertos_ConsultarEmpleados", parametros);
        return tabla.Rows.Cast<DataRow>().Select(fila => new EmpleadoCoberturaSkill(Convert.ToInt32(fila["IdEmpleado"]), Convert.ToInt32(fila["IdSkill"]), Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty, Convert.ToString(fila["Seniority"]) ?? string.Empty, Convert.ToString(fila["NivelSkill"]) ?? string.Empty, Convert.ToDecimal(fila["HorasSemanales"]), Convert.ToDecimal(fila["CargaActual"]))).ToList();
    }

    public bool RegistrarRecomendacion(RecomendacionSkill recomendacion, Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>
        {
            new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@IdSkill", recomendacion.IdSkill), new SqlParameter("@IdUsuarioCreador", usuario.ID),
            new SqlParameter("@TipoAccion", recomendacion.TipoAccion), new SqlParameter("@Observacion", recomendacion.Observacion)
        };
        return _conexion.Escribir("dbo.usp_RecomendacionSkill_Registrar", parametros);
    }

    public List<SugerenciaEmpleadoSkill> ConsultarSugerencias(ConsultaSugerenciasSkill consulta, Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_SkillsDesiertos_SugerirEmpleados", new List<SqlParameter>
        {
            new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@IdSkill", consulta.IdSkill), new SqlParameter("@IdProyecto", consulta.IdProyecto)
        });
        return tabla.Rows.Cast<DataRow>().Select(fila => new SugerenciaEmpleadoSkill(Convert.ToInt32(fila["IdEmpleado"]), Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty, Convert.ToString(fila["Seniority"]) ?? string.Empty, Convert.ToDecimal(fila["HorasSemanales"]), Convert.ToDecimal(fila["CargaActual"]), Convert.ToDecimal(fila["HorasDisponibles"]), Convert.ToBoolean(fila["EsDelProyecto"]), Convert.ToBoolean(fila["TieneSkillObjetivo"]), Convert.ToInt32(fila["CantidadSkillsMismaArea"]), Convert.ToString(fila["SkillsRelacionadas"]) ?? string.Empty)).ToList();
    }

    private static List<SqlParameter> CrearParametros(FiltroCoberturaSkill filtro, Usuario usuario) => new List<SqlParameter>
    {
        new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@FechaDesde", filtro.FechaDesde), new SqlParameter("@FechaHasta", filtro.FechaHasta),
        new SqlParameter("@IdProyecto", (object?)filtro.IdProyecto ?? DBNull.Value), new SqlParameter("@IdCliente", (object?)filtro.IdCliente ?? DBNull.Value)
    };
}
