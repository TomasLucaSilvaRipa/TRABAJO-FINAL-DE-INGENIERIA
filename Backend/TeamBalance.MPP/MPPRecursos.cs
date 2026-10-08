using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRecursos
{
    private readonly Conexion _conexion;
    public MPPRecursos(Conexion conexion) { _conexion = conexion; }
    public List<Skill> ConsultarSkills()
    {
        try
        {
            DataTable tabla = _conexion.Leer("dbo.usp_Skill_Consultar");
            List<Skill> skills = CrearSkills(tabla);
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AreaSkill> ConsultarAreasSkills()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AreaSkill_Consultar");
        return tabla.Rows.Cast<DataRow>().Select(fila => new AreaSkill
        {
            ID = Convert.ToInt32(fila["ID"]), Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty, Activo = Convert.ToBoolean(fila["Activo"])
        }).ToList();
    }

    public Skill RegistrarSkill(Skill skill)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Nombre", skill.Nombre), new SqlParameter("@IdAreaSkill", (object?)skill.IdAreaSkill ?? DBNull.Value) };
            DataTable tabla = _conexion.Leer("dbo.usp_Skill_Registrar", parametros);
            List<Skill> skills = CrearSkills(tabla);
            Skill resultado = skills.Single();
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoSkill(Skill skill)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", skill.ID), new SqlParameter("@Activo", skill.Activo) };
            bool resultado = _conexion.Escribir("dbo.usp_Skill_CambiarEstado", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public Empleado ConsultarFichaEmpleado(Usuario usuario, Usuario solicitante)
    {
        try
        {
            int idAgencia = solicitante.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            int idEmpleado = ConsultarIdEmpleado(usuario.ID, idAgencia);
            Empleado empleado = new Empleado();
            empleado.ID = idEmpleado;
            List<SqlParameter> parametrosSkills = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) };
            DataTable tablaSkills = _conexion.Leer("dbo.usp_EmpleadoSkill_Consultar", parametrosSkills);
            empleado.EmpleadoSkills = CrearEmpleadoSkills(tablaSkills);
            List<SqlParameter> parametrosDisponibilidad = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) };
            DataTable tablaDisponibilidad = _conexion.Leer("dbo.usp_DisponibilidadBase_Consultar", parametrosDisponibilidad);
            if (tablaDisponibilidad.Rows.Count > 0)
            {
                DataRow fila = tablaDisponibilidad.Rows[0];
                TimeSpan horaInicio = (TimeSpan)fila["HoraInicio"];
                TimeSpan horaFin = (TimeSpan)fila["HoraFin"];
                DisponibilidadBase disponibilidad = new DisponibilidadBase(Convert.ToInt32(fila["ID"]), idEmpleado, TimeOnly.FromTimeSpan(horaInicio), TimeOnly.FromTimeSpan(horaFin), Convert.ToDecimal(fila["HorasSemanales"]), Convert.ToString(fila["Observacion"]), Convert.ToBoolean(fila["Activo"]));
                empleado.DisponibilidadBase = disponibilidad;
            }
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public int ConsultarIdEmpleadoPorUsuarioAgencia(int idUsuario, Usuario solicitante)
    {
        try
        {
            int idAgencia = solicitante.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            return ConsultarIdEmpleado(idUsuario, idAgencia);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public void GuardarFichaEmpleado(Empleado empleado, Usuario solicitante)
    {
        try
        {
            int idAgencia = solicitante.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            int idEmpleado = ConsultarIdEmpleado(empleado.ID, idAgencia);
            List<SqlParameter> parametrosLimpiar = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) };
            _conexion.Escribir("dbo.usp_EmpleadoSkill_Limpiar", parametrosLimpiar);
            foreach (EmpleadoSkill empleadoSkill in empleado.EmpleadoSkills)
            {
                List<SqlParameter> parametrosSkill = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@IdSkill", empleadoSkill.IdSkill), new SqlParameter("@Nivel", (object?)empleadoSkill.Nivel ?? DBNull.Value) };
                _conexion.Escribir("dbo.usp_EmpleadoSkill_Registrar", parametrosSkill);
            }
            if (empleado.DisponibilidadBase is null) { throw new ArgumentException("Completá la disponibilidad base del empleado."); }
            DisponibilidadBase disponibilidad = empleado.DisponibilidadBase;
            List<SqlParameter> parametrosDisponibilidad = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@HoraInicio", disponibilidad.HoraInicio.ToTimeSpan()), new SqlParameter("@HoraFin", disponibilidad.HoraFin.ToTimeSpan()), new SqlParameter("@HorasSemanales", disponibilidad.HorasSemanales), new SqlParameter("@Observacion", (object?)disponibilidad.Observacion ?? DBNull.Value) };
            _conexion.Escribir("dbo.usp_DisponibilidadBase_RegistrarActualizar", parametrosDisponibilidad);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Empleado ConsultarMiDisponibilidad(Usuario usuario)
    {
        try
        {
            Empleado empleado = ConsultarFichaEmpleado(usuario, usuario);
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void GuardarMiDisponibilidad(DisponibilidadBase disponibilidad, Usuario usuario)
    {
        try
        {
            int idAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            int idEmpleado = ConsultarIdEmpleado(usuario.ID, idAgencia);
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@HoraInicio", disponibilidad.HoraInicio.ToTimeSpan()), new SqlParameter("@HoraFin", disponibilidad.HoraFin.ToTimeSpan()), new SqlParameter("@HorasSemanales", disponibilidad.HorasSemanales), new SqlParameter("@Observacion", (object?)disponibilidad.Observacion ?? DBNull.Value) };
            _conexion.Escribir("dbo.usp_DisponibilidadBase_RegistrarActualizar", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AusenciaEmpleado> ConsultarMisAusencias(Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_ConsultarPropias", parametros);
            List<AusenciaEmpleado> ausencias = CrearAusencias(tabla);
            return ausencias;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public AusenciaEmpleado RegistrarAusencia(AusenciaEmpleado ausencia, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@TipoPeriodo", ausencia.TipoPeriodo), new SqlParameter("@FechaInicio", ausencia.FechaInicioSolicitada), new SqlParameter("@FechaFin", ausencia.FechaFinSolicitada), new SqlParameter("@HorasNoDisponibles", (object?)ausencia.HorasNoDisponiblesSolicitadas ?? DBNull.Value), new SqlParameter("@Motivo", (object?)ausencia.Motivo ?? DBNull.Value), new SqlParameter("@ComprobanteUrl", (object?)ausencia.ComprobanteUrl ?? DBNull.Value) };
            DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_Registrar", parametros);
            AusenciaEmpleado resultado = CrearAusencias(tabla).Single();
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private int ConsultarIdEmpleado(int idUsuario, int idAgencia)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", idUsuario), new SqlParameter("@IdAgencia", idAgencia) };
            DataTable tabla = _conexion.Leer("dbo.usp_Empleado_ConsultarIdPorUsuarioAgencia", parametros);
            if (tabla.Rows.Count == 0) { throw new ArgumentException("El usuario seleccionado no es un empleado de tu agencia."); }
            int idEmpleado = Convert.ToInt32(tabla.Rows[0]["ID"]);
            return idEmpleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Skill> CrearSkills(DataTable tabla)
    {
        try
        {
            List<Skill> skills = new List<Skill>();
            foreach (DataRow fila in tabla.Rows)
            {
                Skill skill = new Skill(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Categoria"]), Convert.ToBoolean(fila["Activo"]))
                {
                    IdAreaSkill = fila.Table.Columns.Contains("IdAreaSkill") && fila["IdAreaSkill"] != DBNull.Value ? Convert.ToInt32(fila["IdAreaSkill"]) : null,
                    NombreArea = fila.Table.Columns.Contains("NombreArea") && fila["NombreArea"] != DBNull.Value ? Convert.ToString(fila["NombreArea"]) : null,
                };
                skills.Add(skill);
            }
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<EmpleadoSkill> CrearEmpleadoSkills(DataTable tabla)
    {
        try
        {
            List<EmpleadoSkill> habilidades = new List<EmpleadoSkill>();
            foreach (DataRow fila in tabla.Rows)
            {
                EmpleadoSkill habilidad = new EmpleadoSkill(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToInt32(fila["IdSkill"]), Convert.ToString(fila["Nivel"]), Convert.ToBoolean(fila["Activo"]));
                habilidades.Add(habilidad);
            }
            return habilidades;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<AusenciaEmpleado> CrearAusencias(DataTable tabla)
    {
        try
        {
            List<AusenciaEmpleado> ausencias = new List<AusenciaEmpleado>();
            foreach (DataRow fila in tabla.Rows)
            {
                AusenciaEmpleado ausencia = new AusenciaEmpleado(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToString(fila["TipoPeriodo"]) ?? string.Empty, Convert.ToDateTime(fila["FechaInicioSolicitada"]), Convert.ToDateTime(fila["FechaFinSolicitada"]), fila["FechaInicioAprobada"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicioAprobada"]), fila["FechaFinAprobada"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaFinAprobada"]), fila["HorasNoDisponiblesSolicitadas"] == DBNull.Value ? null : Convert.ToDecimal(fila["HorasNoDisponiblesSolicitadas"]), fila["HorasNoDisponiblesAprobadas"] == DBNull.Value ? null : Convert.ToDecimal(fila["HorasNoDisponiblesAprobadas"]), Convert.ToString(fila["Motivo"]), fila["IdUsuarioResolucion"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuarioResolucion"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToDateTime(fila["FechaSolicitud"]), fila["FechaResolucion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaResolucion"]), Convert.ToString(fila["MotivoResolucion"]), Convert.ToBoolean(fila["Activo"]));
                ausencia.ComprobanteUrl = tabla.Columns.Contains("ComprobanteUrl") && fila["ComprobanteUrl"] != DBNull.Value ? Convert.ToString(fila["ComprobanteUrl"]) : null;
                ausencia.NombreEmpleado = tabla.Columns.Contains("NombreEmpleado") && fila["NombreEmpleado"] != DBNull.Value ? Convert.ToString(fila["NombreEmpleado"]) : null;
                ausencias.Add(ausencia);
            }
            return ausencias;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<AusenciaEmpleado> ConsultarSolicitudesPendientes(Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_ConsultarPendientes", new List<SqlParameter> { new("@IdAgencia", solicitante.IdAgencia) });
        return CrearAusencias(tabla);
    }

    public List<EmpleadoDisponibilidadOpcion> ConsultarEmpleadosDisponibilidad(Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Disponibilidad_ConsultarEmpleados", new List<SqlParameter> { new("@IdAgencia", solicitante.IdAgencia) });
        return tabla.Rows.Cast<DataRow>().Select(fila => new EmpleadoDisponibilidadOpcion { IdUsuario = Convert.ToInt32(fila["IdUsuario"]), NombreCompleto = Convert.ToString(fila["NombreCompleto"]) ?? string.Empty }).ToList();
    }

    public AusenciaEmpleado ConsultarAusencia(int idAusencia, Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_ConsultarPorId", new List<SqlParameter> { new("@IdAusencia", idAusencia), new("@IdAgencia", solicitante.IdAgencia) });
        if (tabla.Rows.Count == 0) { throw new KeyNotFoundException("La solicitud no existe o no pertenece a tu agencia."); }
        return CrearAusencias(tabla).Single();
    }

    public List<AusenciaEmpleado> ConsultarAusenciasEmpleado(int idEmpleado, Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_ConsultarPorEmpleado", new List<SqlParameter> { new("@IdEmpleado", idEmpleado), new("@IdAgencia", solicitante.IdAgencia) });
        return CrearAusencias(tabla);
    }

    public List<TareaDisponibilidadAfectada> ConsultarTareasAfectadas(int idEmpleado, DateTime desde, DateTime hasta, Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Disponibilidad_ConsultarTareasAfectadas", new List<SqlParameter> { new("@IdEmpleado", idEmpleado), new("@FechaDesde", desde), new("@FechaHasta", hasta), new("@IdAgencia", solicitante.IdAgencia) });
        return tabla.Rows.Cast<DataRow>().Select(fila => new TareaDisponibilidadAfectada { IdTarea = Convert.ToInt32(fila["IdTarea"]), Titulo = Convert.ToString(fila["Titulo"]) ?? string.Empty, NombreProyecto = Convert.ToString(fila["NombreProyecto"]) ?? string.Empty, Deadline = fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]) }).ToList();
    }

    public AusenciaEmpleado ResolverAusencia(ResolucionAusenciaEmpleado resolucion, string estado, Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_Resolver", new List<SqlParameter> { new("@IdAusencia", resolucion.IdAusencia), new("@IdUsuarioResolucion", solicitante.ID), new("@Estado", estado), new("@FechaInicioAprobada", (object?)resolucion.FechaInicioAprobada ?? DBNull.Value), new("@FechaFinAprobada", (object?)resolucion.FechaFinAprobada ?? DBNull.Value), new("@HorasNoDisponiblesAprobadas", (object?)resolucion.HorasNoDisponiblesAprobadas ?? DBNull.Value), new("@MotivoResolucion", (object?)resolucion.MotivoResolucion ?? DBNull.Value) });
        return CrearAusencias(tabla).Single();
    }

    public AusenciaEmpleado RegistrarAusenciaDirecta(AusenciaEmpleado ausencia, Usuario solicitante)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_RegistrarDirecta", new List<SqlParameter> { new("@IdEmpleado", ausencia.IdEmpleado), new("@IdAgencia", solicitante.IdAgencia), new("@IdUsuarioResolucion", solicitante.ID), new("@TipoPeriodo", ausencia.TipoPeriodo), new("@FechaInicio", ausencia.FechaInicioSolicitada), new("@FechaFin", ausencia.FechaFinSolicitada), new("@HorasNoDisponibles", (object?)ausencia.HorasNoDisponiblesSolicitadas ?? DBNull.Value), new("@Motivo", (object?)ausencia.Motivo ?? DBNull.Value), new("@ComprobanteUrl", (object?)ausencia.ComprobanteUrl ?? DBNull.Value) });
        return CrearAusencias(tabla).Single();
    }

    public bool EsGestorDisponibilidad(Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Disponibilidad_EsGestor", new List<SqlParameter> { new("@IdUsuario", usuario.ID), new("@IdAgencia", usuario.IdAgencia) });
        return tabla.Rows.Count > 0 && Convert.ToBoolean(tabla.Rows[0]["EsGestor"]);
    }
}
