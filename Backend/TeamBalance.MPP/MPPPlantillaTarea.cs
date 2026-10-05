using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPPlantillaTarea
{
    private readonly Conexion _conexion;

    public MPPPlantillaTarea(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<PlantillaTarea> Consultar(Usuario usuario, bool incluirInactivas)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>()
            {
                new SqlParameter("@IdAgencia", usuario.IdAgencia),
                new SqlParameter("@IncluirInactivas", incluirInactivas)
            };
            return CrearPlantillas(_conexion.Leer("dbo.usp_PlantillaTarea_Consultar", parametros));
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public PlantillaTarea Guardar(PlantillaTarea plantilla, Usuario usuario)
    {
        try
        {
            DataTable tabla = _conexion.Leer(plantilla.ID == 0 ? "dbo.usp_PlantillaTarea_Registrar" : "dbo.usp_PlantillaTarea_Modificar", CrearParametrosGuardar(plantilla, usuario));
            return CrearPlantillas(tabla).Single();
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public bool DarBaja(PlantillaTarea plantilla, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>()
            {
                new SqlParameter("@ID", plantilla.ID),
                new SqlParameter("@IdAgencia", usuario.IdAgencia),
                new SqlParameter("@MotivoBaja", plantilla.MotivoBaja ?? string.Empty)
            };
            return _conexion.Escribir("dbo.usp_PlantillaTarea_DarBaja", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    private static List<SqlParameter> CrearParametrosGuardar(PlantillaTarea plantilla, Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", usuario.IdAgencia),
            new SqlParameter("@IdSkillRequerido", plantilla.IdSkillRequerido ?? 0),
            new SqlParameter("@Nombre", plantilla.Nombre),
            new SqlParameter("@TituloSugerido", plantilla.TituloSugerido ?? string.Empty),
            new SqlParameter("@DescripcionBase", (object?)plantilla.DescripcionBase ?? DBNull.Value),
            new SqlParameter("@HorasEstimadas", plantilla.HorasEstimadas ?? 0),
            new SqlParameter("@Complejidad", (object?)plantilla.Complejidad ?? DBNull.Value),
            new SqlParameter("@PrioridadSugerida", (object?)plantilla.PrioridadSugerida ?? DBNull.Value),
            new SqlParameter("@SkillRequerido", plantilla.SkillRequerido ?? string.Empty),
            new SqlParameter("@SeniorityRecomendado", (object?)plantilla.SeniorityRecomendado ?? DBNull.Value),
            new SqlParameter("@ChecklistBaseJson", (object?)plantilla.ChecklistBaseJson ?? DBNull.Value),
            new SqlParameter("@ArchivosAdjuntosJson", (object?)plantilla.ArchivosAdjuntosJson ?? DBNull.Value)
        };

        if (plantilla.ID > 0)
        {
            parametros.Insert(0, new SqlParameter("@ID", plantilla.ID));
        }

        return parametros;
    }

    private static List<PlantillaTarea> CrearPlantillas(DataTable tabla)
    {
        List<PlantillaTarea> plantillas = new List<PlantillaTarea>();

        foreach (DataRow fila in tabla.Rows)
        {
            PlantillaTarea plantilla = new PlantillaTarea(
                Convert.ToInt32(fila["ID"]),
                Convert.ToInt32(fila["IdAgencia"]),
                fila["IdSkillRequerido"] == DBNull.Value ? null : Convert.ToInt32(fila["IdSkillRequerido"]),
                Convert.ToString(fila["Nombre"]) ?? string.Empty,
                Convert.ToString(fila["TituloSugerido"]),
                Convert.ToString(fila["DescripcionBase"]),
                fila["HorasEstimadas"] == DBNull.Value ? null : Convert.ToDecimal(fila["HorasEstimadas"]),
                Convert.ToString(fila["Complejidad"]),
                Convert.ToString(fila["PrioridadSugerida"]),
                Convert.ToString(fila["SkillRequerido"]),
                Convert.ToString(fila["SeniorityRecomendado"]),
                Convert.ToString(fila["Estado"]) ?? string.Empty,
                Convert.ToBoolean(fila["Activo"]),
                Convert.ToDateTime(fila["FechaCreacion"]),
                fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]),
                Convert.ToString(fila["ChecklistBaseJson"]),
                Convert.ToString(fila["ArchivosAdjuntosJson"]));
            plantilla.MotivoBaja = fila.Table.Columns.Contains("MotivoBaja") ? Convert.ToString(fila["MotivoBaja"]) : null;
            plantillas.Add(plantilla);
        }

        return plantillas;
    }
}
