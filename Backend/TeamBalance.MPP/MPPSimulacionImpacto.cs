using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPSimulacionImpacto
{
    private readonly Conexion _conexion;

    public MPPSimulacionImpacto(Conexion conexion)
    {
        _conexion = conexion;
    }

    public SimulacionImpacto ConsultarDatosOperativos(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            DataTable tabla = _conexion.Leer("dbo.usp_SimulacionImpacto_Crear", CrearParametrosBase(simulacion, usuario));

            if (tabla.Rows.Count == 0)
            {
                throw new ArgumentException("La tarea no está disponible para simular o el recurso candidato no pertenece a tu agencia.");
            }

            return CrearDatosOperativos(tabla.Rows[0], simulacion, usuario);
        }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public SimulacionImpacto Registrar(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>()
            {
                new SqlParameter("@IdTarea", simulacion.IdTarea),
                new SqlParameter("@IdEmpleadoCandidato", simulacion.IdEmpleadoCandidato),
                new SqlParameter("@IdUsuario", usuario.ID),
                new SqlParameter("@CargaActual", simulacion.CargaActual ?? 0),
                new SqlParameter("@CargaProyectada", simulacion.CargaProyectada ?? 0),
                new SqlParameter("@DisponibilidadRestante", simulacion.DisponibilidadRestante ?? 0),
                new SqlParameter("@PorcentajeOcupacionActual", simulacion.PorcentajeOcupacionActual ?? 0),
                new SqlParameter("@PorcentajeOcupacionProyectado", simulacion.PorcentajeOcupacionProyectado ?? 0),
                new SqlParameter("@GeneraSobrecarga", simulacion.GeneraSobrecarga),
                new SqlParameter("@AdvertenciasJson", simulacion.AdvertenciasJson ?? "[]"),
                new SqlParameter("@ImpactoOperativo", simulacion.ImpactoOperativo ?? "Viable")
            };
            DataTable tabla = _conexion.Leer("dbo.usp_SimulacionImpacto_Registrar", parametros);
            SimulacionImpacto resultado = CrearPersistida(tabla.Rows[0]);
            CopiarDatosDeContexto(resultado, simulacion);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public bool Descartar(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>()
            {
                new SqlParameter("@ID", simulacion.ID),
                new SqlParameter("@IdUsuario", usuario.ID)
            };
            return _conexion.Escribir("dbo.usp_SimulacionImpacto_Descartar", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    private static List<SqlParameter> CrearParametrosBase(SimulacionImpacto simulacion, Usuario usuario)
    {
        return new List<SqlParameter>()
        {
            new SqlParameter("@IdTarea", simulacion.IdTarea),
            new SqlParameter("@IdEmpleadoCandidato", simulacion.IdEmpleadoCandidato),
            new SqlParameter("@IdUsuario", usuario.ID)
        };
    }

    private static SimulacionImpacto CrearDatosOperativos(DataRow fila, SimulacionImpacto simulacion, Usuario usuario)
    {
        return new SimulacionImpacto
        {
            IdTarea = simulacion.IdTarea,
            IdEmpleadoCandidato = simulacion.IdEmpleadoCandidato,
            IdUsuarioCreador = usuario.ID,
            CargaActual = Convert.ToDecimal(fila["CargaActual"]),
            HorasTarea = Convert.ToDecimal(fila["HorasTarea"]),
            CapacidadSemanal = Convert.ToDecimal(fila["CapacidadSemanal"]),
            DiasAusenciaProximaSemana = Convert.ToInt32(fila["DiasAusenciaProximaSemana"]),
            DeadlineTarea = fila["DeadlineTarea"] == DBNull.Value ? null : Convert.ToDateTime(fila["DeadlineTarea"]),
            PrioridadTarea = Convert.ToString(fila["PrioridadTarea"]),
            NombreTarea = Convert.ToString(fila["NombreTarea"]),
            NombreProyecto = Convert.ToString(fila["NombreProyecto"])
        };
    }

    private static SimulacionImpacto CrearPersistida(DataRow fila)
    {
        return new SimulacionImpacto(
            Convert.ToInt32(fila["ID"]),
            Convert.ToInt32(fila["IdTarea"]),
            Convert.ToInt32(fila["IdEmpleadoCandidato"]),
            Convert.ToInt32(fila["IdUsuarioCreador"]),
            Convert.ToDecimal(fila["CargaActual"]),
            Convert.ToDecimal(fila["CargaProyectada"]),
            Convert.ToDecimal(fila["DisponibilidadRestante"]),
            Convert.ToDecimal(fila["PorcentajeOcupacionActual"]),
            Convert.ToDecimal(fila["PorcentajeOcupacionProyectado"]),
            Convert.ToBoolean(fila["GeneraSobrecarga"]),
            Convert.ToString(fila["AdvertenciasJson"]),
            Convert.ToString(fila["ImpactoOperativo"]),
            Convert.ToDateTime(fila["FechaCreacion"]),
            fila["FechaUltimaModificacion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaUltimaModificacion"]),
            Convert.ToDateTime(fila["FechaExpiracion"]),
            Convert.ToBoolean(fila["Activo"]));
    }

    private static void CopiarDatosDeContexto(SimulacionImpacto destino, SimulacionImpacto origen)
    {
        destino.HorasTarea = origen.HorasTarea;
        destino.CapacidadSemanal = origen.CapacidadSemanal;
        destino.DiasAusenciaProximaSemana = origen.DiasAusenciaProximaSemana;
        destino.DeadlineTarea = origen.DeadlineTarea;
        destino.PrioridadTarea = origen.PrioridadTarea;
        destino.NombreTarea = origen.NombreTarea;
        destino.NombreProyecto = origen.NombreProyecto;
    }
}
