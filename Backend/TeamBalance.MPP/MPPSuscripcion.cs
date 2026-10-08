using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPSuscripcion
{
    private readonly Conexion _conexion;

    public MPPSuscripcion(Conexion conexion) => _conexion = conexion;

    public Suscripcion? ConsultarActual(int idAgencia)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarActualPorAgencia", new List<SqlParameter> { new("@IdAgencia", idAgencia) });
        return tabla.Rows.Count == 0 ? null : CrearSuscripcion(tabla.Rows[0]);
    }

    public List<OperacionSuscripcionHistorial> ConsultarHistorial(int idAgencia)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarHistorialPorAgencia", new List<SqlParameter> { new("@IdAgencia", idAgencia) });
        return tabla.Rows.Cast<DataRow>().Select(fila => new OperacionSuscripcionHistorial
        {
            ID = Convert.ToInt32(fila["ID"]),
            TipoOperacion = Convert.ToString(fila["TipoOperacion"]) ?? string.Empty,
            Modalidad = fila["Modalidad"] == DBNull.Value ? null : Convert.ToString(fila["Modalidad"]),
            Importe = fila["Importe"] == DBNull.Value ? null : Convert.ToDecimal(fila["Importe"]),
            Moneda = fila["Moneda"] == DBNull.Value ? null : Convert.ToString(fila["Moneda"]),
            Proveedor = fila["Proveedor"] == DBNull.Value ? null : Convert.ToString(fila["Proveedor"]),
            Referencia = fila["Referencia"] == DBNull.Value ? null : Convert.ToString(fila["Referencia"]),
            Estado = Convert.ToString(fila["Estado"]) ?? string.Empty,
            Detalle = fila["Detalle"] == DBNull.Value ? null : Convert.ToString(fila["Detalle"]),
            Fecha = Convert.ToDateTime(fila["Fecha"]),
        }).ToList();
    }

    public OperacionSuscripcionPendiente CrearSolicitudCambioPlan(int idSuscripcion, int idPlanNuevo, string referenciaInterna, string proveedor)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_CrearSolicitudCambioPlan", new List<SqlParameter>
        {
            new("@IdSuscripcion", idSuscripcion), new("@IdPlanNuevo", idPlanNuevo), new("@ReferenciaInterna", referenciaInterna), new("@Proveedor", proveedor),
        });
        if (tabla.Rows.Count != 1) throw new InvalidOperationException("No fue posible generar la solicitud de actualización.");
        DataRow fila = tabla.Rows[0];
        return new OperacionSuscripcionPendiente
        {
            IdOperacion = Convert.ToInt32(fila["IdOperacion"]), IdSuscripcion = Convert.ToInt32(fila["IdSuscripcion"]), IdPlanNuevo = Convert.ToInt32(fila["IdPlanNuevo"]),
            ReferenciaInterna = Convert.ToString(fila["ReferenciaInterna"]) ?? string.Empty, Importe = Convert.ToDecimal(fila["Importe"]),
            Moneda = Convert.ToString(fila["Moneda"]) ?? string.Empty, Proveedor = Convert.ToString(fila["Proveedor"]) ?? string.Empty,
        };
    }

    public OperacionSuscripcionPendiente ConsultarOperacionPendiente(string referenciaInterna)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarOperacionPendiente", new List<SqlParameter> { new("@ReferenciaInterna", referenciaInterna) });
        if (tabla.Rows.Count != 1) throw new KeyNotFoundException("No existe una actualización de suscripción pendiente con la referencia indicada.");
        DataRow fila = tabla.Rows[0];
        return new OperacionSuscripcionPendiente
        {
            IdOperacion = Convert.ToInt32(fila["IdOperacion"]), IdSuscripcion = Convert.ToInt32(fila["IdSuscripcion"]), IdPlanNuevo = Convert.ToInt32(fila["IdPlanNuevo"]),
            ReferenciaInterna = Convert.ToString(fila["ReferenciaInterna"]) ?? string.Empty, Importe = Convert.ToDecimal(fila["Importe"]),
            Moneda = Convert.ToString(fila["Moneda"]) ?? string.Empty, Proveedor = Convert.ToString(fila["Proveedor"]) ?? string.Empty,
        };
    }

    public Suscripcion AplicarResultadoCambioPlan(string referenciaInterna, string referenciaProveedor, string estadoProveedor, string detalle)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_AplicarResultadoCambioPlan", new List<SqlParameter>
        {
            new("@ReferenciaInterna", referenciaInterna), new("@ReferenciaProveedor", referenciaProveedor), new("@EstadoProveedor", estadoProveedor), new("@Detalle", detalle),
        });
        if (tabla.Rows.Count != 1) throw new InvalidOperationException("No fue posible actualizar el resultado de la suscripción.");
        return CrearSuscripcion(tabla.Rows[0]);
    }

    public Suscripcion ActualizarRenovacion(int idSuscripcion, bool activa, string? motivo)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ActualizarRenovacion", new List<SqlParameter>
        {
            new("@IdSuscripcion", idSuscripcion), new("@Activa", activa), new("@Motivo", (object?)motivo ?? DBNull.Value),
        });
        if (tabla.Rows.Count != 1) throw new KeyNotFoundException("No existe una suscripción activa para actualizar.");
        return CrearSuscripcion(tabla.Rows[0]);
    }

    public bool PuedeUsarAgencia(int idAgencia)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_PuedeUsarAgencia", new List<SqlParameter> { new("@IdAgencia", idAgencia) });
        return tabla.Rows.Count == 1 && Convert.ToBoolean(tabla.Rows[0]["Vigente"]);
    }

    public List<SincronizacionVencimientosResultado> SincronizarVencimientos()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_SincronizarVencimientos", new List<SqlParameter>());
        return tabla.Rows.Cast<DataRow>().Select(fila => new SincronizacionVencimientosResultado
        {
            IdSuscripcion = Convert.ToInt32(fila["IdSuscripcion"]), IdAgencia = Convert.ToInt32(fila["IdAgencia"]), Estado = Convert.ToString(fila["Estado"]) ?? string.Empty,
        }).ToList();
    }

    public void RegistrarRenovacionProveedor(int idSuscripcion, string referenciaProveedor, string estadoProveedor)
    {
        _conexion.Leer("dbo.usp_Suscripcion_RegistrarRenovacionProveedor", new List<SqlParameter>
        {
            new("@IdSuscripcion", idSuscripcion), new("@ReferenciaProveedor", referenciaProveedor), new("@EstadoProveedor", estadoProveedor),
        });
    }

    public List<RenovacionProveedorPendiente> ConsultarRenovacionesProveedor()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarRenovacionesProveedor", new List<SqlParameter>());
        return tabla.Rows.Cast<DataRow>().Select(f => new RenovacionProveedorPendiente
        {
            IdSuscripcion = Convert.ToInt32(f["IdSuscripcion"]), IdAgencia = Convert.ToInt32(f["IdAgencia"]), IdPlanComercial = Convert.ToInt32(f["IdPlanComercial"]),
            ReferenciaRenovacionProveedor = Convert.ToString(f["ReferenciaRenovacionProveedor"]) ?? string.Empty,
        }).ToList();
    }

    public void AplicarCobroRecurrente(int idSuscripcion, string referenciaPago, decimal importe, string moneda, string detalle)
    {
        _conexion.Leer("dbo.usp_Suscripcion_AplicarCobroRecurrente", new List<SqlParameter>
        {
            new("@IdSuscripcion", idSuscripcion), new("@ReferenciaPago", referenciaPago), new("@Importe", importe), new("@Moneda", moneda), new("@Detalle", detalle),
        });
    }

    public void ActualizarEstadoRenovacionProveedor(int idSuscripcion, string estadoProveedor)
    {
        _conexion.Leer("dbo.usp_Suscripcion_ActualizarEstadoRenovacionProveedor", new List<SqlParameter>
        {
            new("@IdSuscripcion", idSuscripcion), new("@EstadoProveedor", estadoProveedor),
        });
    }

    private static Suscripcion CrearSuscripcion(DataRow fila)
    {
        Suscripcion suscripcion = new(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdPlanComercial"]),
            fila["ReferenciaExterna"] == DBNull.Value ? null : Convert.ToString(fila["ReferenciaExterna"]), Convert.ToString(fila["Estado"]) ?? string.Empty,
            Convert.ToDateTime(fila["FechaAlta"]), Convert.ToDateTime(fila["FechaVencimiento"]), fila["FechaProximaRenovacion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaProximaRenovacion"]),
            Convert.ToBoolean(fila["RenovacionAutomatica"]), Convert.ToDecimal(fila["ImporteVigente"]), Convert.ToBoolean(fila["Activo"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]));
        suscripcion.NombrePlan = fila.Table.Columns.Contains("NombrePlan") ? Convert.ToString(fila["NombrePlan"]) : null;
        suscripcion.PeriodicidadPlan = fila.Table.Columns.Contains("PeriodicidadPlan") ? Convert.ToString(fila["PeriodicidadPlan"]) : null;
        suscripcion.Moneda = fila.Table.Columns.Contains("Moneda") ? Convert.ToString(fila["Moneda"]) : null;
        suscripcion.ReferenciaRenovacionProveedor = fila.Table.Columns.Contains("ReferenciaRenovacionProveedor") && fila["ReferenciaRenovacionProveedor"] != DBNull.Value ? Convert.ToString(fila["ReferenciaRenovacionProveedor"]) : null;
        suscripcion.EstadoRenovacionProveedor = fila.Table.Columns.Contains("EstadoRenovacionProveedor") && fila["EstadoRenovacionProveedor"] != DBNull.Value ? Convert.ToString(fila["EstadoRenovacionProveedor"]) : null;
        suscripcion.FechaSincronizacionRenovacion = fila.Table.Columns.Contains("FechaSincronizacionRenovacion") && fila["FechaSincronizacionRenovacion"] != DBNull.Value ? Convert.ToDateTime(fila["FechaSincronizacionRenovacion"]) : null;
        return suscripcion;
    }
}
