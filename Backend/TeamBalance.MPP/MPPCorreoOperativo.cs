using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPCorreoOperativo
{
    private readonly Conexion _conexion;
    public MPPCorreoOperativo(Conexion conexion) { _conexion = conexion; }

    public PreferenciaCorreoOperativo ConsultarPreferencia(int idUsuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_PreferenciaCorreoOperativo_Consultar", new() { new("@IdUsuario", idUsuario) });
        DataRow fila = tabla.Rows[0];
        return new PreferenciaCorreoOperativo
        {
            IdUsuario = idUsuario,
            ResumenDiarioActivo = Convert.ToBoolean(fila["ResumenDiarioActivo"]),
            HoraResumenDiario = (TimeSpan)fila["HoraResumenDiario"],
            AlertasDeadlineActivo = Convert.ToBoolean(fila["AlertasDeadlineActivo"]),
            ResumenSemanalActivo = Convert.ToBoolean(fila["ResumenSemanalActivo"])
        };
    }

    public void GuardarPreferencia(PreferenciaCorreoOperativo preferencia)
    {
        _conexion.Escribir("dbo.usp_PreferenciaCorreoOperativo_Guardar", new()
        {
            new("@IdUsuario", preferencia.IdUsuario), new("@ResumenDiarioActivo", preferencia.ResumenDiarioActivo),
            new("@HoraResumenDiario", preferencia.HoraResumenDiario), new("@AlertasDeadlineActivo", preferencia.AlertasDeadlineActivo),
            new("@ResumenSemanalActivo", preferencia.ResumenSemanalActivo)
        });
    }

    public List<DestinatarioCorreoOperativo> ConsultarDestinatarios()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_CorreoOperativo_ConsultarDestinatarios");
        return tabla.Rows.Cast<DataRow>().Select(fila => new DestinatarioCorreoOperativo
        {
            IdUsuario = Convert.ToInt32(fila["IdUsuario"]), IdAgencia = fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]),
            Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty, Email = Convert.ToString(fila["Email"]) ?? string.Empty,
            TipoUsuario = Convert.ToString(fila["TipoUsuario"]) ?? string.Empty, ResumenDiarioActivo = Convert.ToBoolean(fila["ResumenDiarioActivo"]),
            HoraResumenDiario = (TimeSpan)fila["HoraResumenDiario"], AlertasDeadlineActivo = Convert.ToBoolean(fila["AlertasDeadlineActivo"]),
            ResumenSemanalActivo = Convert.ToBoolean(fila["ResumenSemanalActivo"])
        }).ToList();
    }

    public List<TareaResumenCorreo> ConsultarTareas(int idUsuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_CorreoOperativo_ConsultarTareasEmpleado", new() { new("@IdUsuario", idUsuario) });
        return tabla.Rows.Cast<DataRow>().Select(fila => new TareaResumenCorreo
        {
            IdTarea = Convert.ToInt32(fila["IdTarea"]), Titulo = Convert.ToString(fila["Titulo"]) ?? string.Empty,
            Proyecto = Convert.ToString(fila["Proyecto"]) ?? string.Empty, Estado = Convert.ToString(fila["Estado"]) ?? "Pendiente",
            Prioridad = Convert.ToString(fila["Prioridad"]), Deadline = fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]),
            PorcentajeAvance = Convert.ToDecimal(fila["PorcentajeAvance"])
        }).ToList();
    }

    public ResumenSemanalCorreo ConsultarResumenSemanal(int idAgencia)
    {
        DataRow fila = _conexion.Leer("dbo.usp_CorreoOperativo_ResumenSemanal", new() { new("@IdAgencia", idAgencia) }).Rows[0];
        return new ResumenSemanalCorreo
        {
            ProyectosActivos = Convert.ToInt32(fila["ProyectosActivos"]), TareasActivas = Convert.ToInt32(fila["TareasActivas"]),
            TareasVencidas = Convert.ToInt32(fila["TareasVencidas"]), EmpleadosSobrecargados = Convert.ToInt32(fila["EmpleadosSobrecargados"]),
            OcupacionPromedio = Convert.ToDecimal(fila["OcupacionPromedio"])
        };
    }

    public bool ReservarEnvio(string clave, int idUsuario, int? idAgencia, string tipo)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_CorreoOperativo_ReservarEnvio", new()
        {
            new("@ClaveEnvio", clave), new("@IdUsuario", idUsuario), new("@IdAgencia", (object?)idAgencia ?? DBNull.Value), new("@Tipo", tipo)
        });
        return tabla.Rows.Count == 1 && Convert.ToBoolean(tabla.Rows[0]["Reservado"]);
    }

    public void CompletarEnvio(string clave, bool enviado, string? detalle = null) => _conexion.Escribir("dbo.usp_CorreoOperativo_CompletarEnvio", new()
    {
        new("@ClaveEnvio", clave), new("@Enviado", enviado), new("@DetalleError", (object?)detalle ?? DBNull.Value)
    });

    public void CrearToken(string tokenHash, int idUsuario, int idTarea, string accion, DateTime expiracion) => _conexion.Escribir("dbo.usp_AccionCorreoTarea_Crear", new()
    {
        new("@TokenHash", tokenHash), new("@IdUsuario", idUsuario), new("@IdTarea", idTarea), new("@Accion", accion), new("@FechaExpiracion", expiracion)
    });

    public AccionCorreoTarea? ConsultarAccion(string tokenHash)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AccionCorreoTarea_Consultar", new() { new("@TokenHash", tokenHash) });
        if (tabla.Rows.Count == 0) return null;
        DataRow fila = tabla.Rows[0];
        return new AccionCorreoTarea
        {
            IdUsuario = Convert.ToInt32(fila["IdUsuario"]), IdTarea = Convert.ToInt32(fila["IdTarea"]), Accion = Convert.ToString(fila["Accion"]) ?? string.Empty,
            TituloTarea = Convert.ToString(fila["TituloTarea"]) ?? string.Empty, Proyecto = Convert.ToString(fila["Proyecto"]) ?? string.Empty,
            FechaExpiracion = Convert.ToDateTime(fila["FechaExpiracion"]), Disponible = Convert.ToBoolean(fila["Disponible"])
        };
    }

    public bool MarcarAccionUsada(string tokenHash)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AccionCorreoTarea_MarcarUsada", new() { new("@TokenHash", tokenHash) });
        return tabla.Rows.Count == 1 && Convert.ToBoolean(tabla.Rows[0]["Actualizada"]);
    }
}
