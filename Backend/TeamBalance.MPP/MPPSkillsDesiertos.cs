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
        return tabla.Rows.Cast<DataRow>().Select(fila => new TareaCoberturaSkill
        {
            IdTarea = Convert.ToInt32(fila["IdTarea"]), IdSkill = Convert.ToInt32(fila["IdSkill"]), NombreSkill = Convert.ToString(fila["NombreSkill"]) ?? string.Empty,
            CategoriaSkill = fila["CategoriaSkill"] == DBNull.Value ? null : Convert.ToString(fila["CategoriaSkill"]), TituloTarea = Convert.ToString(fila["TituloTarea"]) ?? string.Empty,
            HorasEstimadas = Convert.ToDecimal(fila["HorasEstimadas"]), Deadline = Convert.ToDateTime(fila["Deadline"]), IdProyecto = Convert.ToInt32(fila["IdProyecto"]),
            NombreProyecto = Convert.ToString(fila["NombreProyecto"]) ?? string.Empty, NombreCliente = Convert.ToString(fila["NombreCliente"]) ?? string.Empty
        }).ToList();
    }

    public List<EmpleadoCoberturaSkill> ConsultarEmpleados(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter> { new SqlParameter("@IdAgencia", usuario.IdAgencia) };
        DataTable tabla = _conexion.Leer("dbo.usp_SkillsDesiertos_ConsultarEmpleados", parametros);
        return tabla.Rows.Cast<DataRow>().Select(fila => new EmpleadoCoberturaSkill
        {
            IdEmpleado = Convert.ToInt32(fila["IdEmpleado"]), IdSkill = Convert.ToInt32(fila["IdSkill"]), NombreEmpleado = Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty,
            Seniority = Convert.ToString(fila["Seniority"]) ?? string.Empty, NivelSkill = Convert.ToString(fila["NivelSkill"]) ?? string.Empty,
            HorasSemanales = Convert.ToDecimal(fila["HorasSemanales"]), CargaActual = Convert.ToDecimal(fila["CargaActual"])
        }).ToList();
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
        return tabla.Rows.Cast<DataRow>().Select(fila => new SugerenciaEmpleadoSkill
        {
            IdEmpleado = Convert.ToInt32(fila["IdEmpleado"]), NombreEmpleado = Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty,
            Seniority = Convert.ToString(fila["Seniority"]) ?? string.Empty, HorasSemanales = Convert.ToDecimal(fila["HorasSemanales"]),
            CargaActual = Convert.ToDecimal(fila["CargaActual"]), HorasDisponibles = Convert.ToDecimal(fila["HorasDisponibles"]),
            EsDelProyecto = Convert.ToBoolean(fila["EsDelProyecto"]), TieneSkillObjetivo = Convert.ToBoolean(fila["TieneSkillObjetivo"]),
            CantidadSkillsMismaArea = Convert.ToInt32(fila["CantidadSkillsMismaArea"]), SkillsRelacionadas = Convert.ToString(fila["SkillsRelacionadas"]) ?? string.Empty,
        }).ToList();
    }

    private static List<SqlParameter> CrearParametros(FiltroCoberturaSkill filtro, Usuario usuario) => new List<SqlParameter>
    {
        new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@FechaDesde", filtro.FechaDesde), new SqlParameter("@FechaHasta", filtro.FechaHasta),
        new SqlParameter("@IdProyecto", (object?)filtro.IdProyecto ?? DBNull.Value), new SqlParameter("@IdCliente", (object?)filtro.IdCliente ?? DBNull.Value)
    };
}
