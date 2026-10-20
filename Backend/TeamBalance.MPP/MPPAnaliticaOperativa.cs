using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPAnaliticaOperativa
{
    private readonly Conexion _conexion;

    public MPPAnaliticaOperativa(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<Empleado> ConsultarEmpleadosActivos(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")) };
        DataTable tabla = _conexion.Leer("dbo.usp_Analitica_ConsultarEmpleadosActivos", parametros);
        List<Empleado> empleados = new List<Empleado>();
        foreach (DataRow fila in tabla.Rows)
        {
            Empleado empleado = new Empleado(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), Convert.ToDecimal(fila["CostoHora"]), Convert.ToDecimal(fila["HorasDisponiblesSemanales"]), Convert.ToString(fila["Seniority"]) ?? string.Empty, Convert.ToString(fila["EstadoLaboral"]) ?? string.Empty, fila["FechaIngreso"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaIngreso"]), new List<Skill>());
            empleados.Add(empleado);
        }
        return empleados;
    }

    public int ConsultarIdPM(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")) };
        DataTable tabla = _conexion.Leer("dbo.usp_Analitica_ConsultarIdPM", parametros);
        if (tabla.Rows.Count == 0) { throw new UnauthorizedAccessException("Tu cuenta no está asociada a un perfil PM de la agencia."); }
        return Convert.ToInt32(tabla.Rows[0]["ID"]);
    }

    public List<DestinatarioDesvioHoras> ConsultarDuenosAgencia(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")) };
        DataTable tabla = _conexion.Leer("dbo.usp_Analitica_ConsultarDuenosAgencia", parametros);
        List<DestinatarioDesvioHoras> destinatarios = new List<DestinatarioDesvioHoras>();
        foreach (DataRow fila in tabla.Rows)
        {
            DestinatarioDesvioHoras destinatario = new DestinatarioDesvioHoras();
            destinatario.IdUsuario = Convert.ToInt32(fila["ID"]);
            destinatario.Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty;
            destinatario.Email = Convert.ToString(fila["Email"]) ?? string.Empty;
            destinatarios.Add(destinatario);
        }
        return destinatarios;
    }

    public int RegistrarSolicitudRevision(SolicitudRevisionOperativa solicitud)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", solicitud.IdAgencia), new SqlParameter("@IdEmpleado", solicitud.IdEmpleado), new SqlParameter("@IdProyecto", (object?)solicitud.IdProyecto ?? DBNull.Value), new SqlParameter("@IdTarea", (object?)solicitud.IdTarea ?? DBNull.Value), new SqlParameter("@IdPMResponsable", solicitud.IdPMResponsable), new SqlParameter("@IdUsuarioSolicitante", solicitud.IdUsuarioSolicitante), new SqlParameter("@CausaPrincipal", solicitud.CausaPrincipal), new SqlParameter("@AccionSugerida", solicitud.AccionSugerida)
        };
        DataTable tabla = _conexion.Leer("dbo.usp_SolicitudRevisionOperativa_Registrar", parametros);
        if (tabla.Rows.Count == 0) { throw new InvalidOperationException("No fue posible registrar la solicitud de revisión operativa."); }
        return Convert.ToInt32(tabla.Rows[0]["ID"]);
    }

    public List<SolicitudRevisionOperativa> ConsultarSolicitudesPendientes(Usuario usuario, int idPM)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")), new SqlParameter("@IdPMResponsable", idPM) };
        DataTable tabla = _conexion.Leer("dbo.usp_SolicitudRevisionOperativa_ConsultarPendientesPM", parametros);
        List<SolicitudRevisionOperativa> solicitudes = new List<SolicitudRevisionOperativa>();
        foreach (DataRow fila in tabla.Rows)
        {
            SolicitudRevisionOperativa solicitud = new SolicitudRevisionOperativa();
            solicitud.ID = Convert.ToInt32(fila["ID"]);
            solicitud.IdAgencia = Convert.ToInt32(fila["IdAgencia"]);
            solicitud.IdEmpleado = Convert.ToInt32(fila["IdEmpleado"]);
            solicitud.IdProyecto = fila["IdProyecto"] == DBNull.Value ? null : Convert.ToInt32(fila["IdProyecto"]);
            solicitud.IdTarea = fila["IdTarea"] == DBNull.Value ? null : Convert.ToInt32(fila["IdTarea"]);
            solicitud.IdPMResponsable = Convert.ToInt32(fila["IdPMResponsable"]);
            solicitud.IdUsuarioSolicitante = Convert.ToInt32(fila["IdUsuarioSolicitante"]);
            solicitud.CausaPrincipal = Convert.ToString(fila["CausaPrincipal"]) ?? string.Empty;
            solicitud.AccionSugerida = Convert.ToString(fila["AccionSugerida"]) ?? string.Empty;
            solicitud.Estado = Convert.ToString(fila["Estado"]) ?? string.Empty;
            solicitud.FechaAlta = Convert.ToDateTime(fila["FechaAlta"]);
            solicitud.NombreEmpleado = Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty;
            solicitud.NombreProyecto = Convert.ToString(fila["NombreProyecto"]) ?? string.Empty;
            solicitud.NombrePM = Convert.ToString(fila["NombrePM"]) ?? string.Empty;
            solicitudes.Add(solicitud);
        }
        return solicitudes;
    }

    public DestinatarioDesvioHoras? ConsultarDestinatarioPM(int idPM)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdPMResponsable", idPM) };
        DataTable tabla = _conexion.Leer("dbo.usp_Analitica_ConsultarDestinatarioPM", parametros);
        if (tabla.Rows.Count == 0) { return null; }
        DestinatarioDesvioHoras destinatario = new DestinatarioDesvioHoras();
        destinatario.IdUsuario = Convert.ToInt32(tabla.Rows[0]["ID"]);
        destinatario.Nombre = Convert.ToString(tabla.Rows[0]["Nombre"]) ?? string.Empty;
        destinatario.Email = Convert.ToString(tabla.Rows[0]["Email"]) ?? string.Empty;
        return destinatario;
    }
}
