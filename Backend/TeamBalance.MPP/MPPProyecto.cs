using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPProyecto
{
    private readonly Conexion _conexion;
    public MPPProyecto(Conexion conexion) { _conexion = conexion; }
    public void CrearEstadosBase(int idAgencia) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) }; _conexion.Escribir("dbo.usp_EstadoTarea_CrearBase", parametros); }
    public List<Proyecto> Consultar(int idAgencia) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) }; return CrearProyectos(_conexion.Leer("dbo.usp_Proyecto_Consultar", parametros)); }
    public Proyecto Guardar(Proyecto proyecto) { List<SqlParameter> parametros = CrearParametrosProyecto(proyecto); DataTable tabla = _conexion.Leer(proyecto.ID == 0 ? "dbo.usp_Proyecto_Registrar" : "dbo.usp_Proyecto_Modificar", parametros); return CrearProyectos(tabla).Single(); }
    public bool CambiarEstado(int idProyecto, int idAgencia, bool activo) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", idProyecto), new SqlParameter("@IdAgencia", idAgencia), new SqlParameter("@Activo", activo) }; return _conexion.Escribir("dbo.usp_Proyecto_CambiarEstado", parametros); }
    public GestionProyectoOpciones ConsultarOpciones(int idAgencia)
    {
        List<SqlParameter> parametrosClientes = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) };
        List<SqlParameter> parametrosResponsables = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) };
        DataTable tablaClientes = _conexion.Leer("dbo.usp_Cliente_ConsultarPorAgencia", parametrosClientes); DataTable tablaResponsables = _conexion.Leer("dbo.usp_Proyecto_ConsultarResponsables", parametrosResponsables);
        List<Cliente> clientes = new List<Cliente>(); 
        foreach (DataRow fila in tablaClientes.Rows) { 
            Cliente cliente = new Cliente(Convert.ToInt32(fila["ID"]), idAgencia, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]), Convert.ToString(fila["Email"]), Convert.ToString(fila["Telefono"]), Convert.ToBoolean(fila["Activo"]), null, new List<Proyecto>()); clientes.Add(cliente); 
        }
        List<Usuario> responsables = CrearUsuarios(tablaResponsables); 
        GestionProyectoOpciones opciones = new GestionProyectoOpciones(clientes, responsables); 
        return opciones;
    }
    public Cliente RegistrarCliente(Cliente cliente, int idAgencia) { 
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Nombre", cliente.Nombre), new SqlParameter("@RazonSocial", (object?)cliente.RazonSocial ?? DBNull.Value), new SqlParameter("@Email", (object?)cliente.Email ?? DBNull.Value), new SqlParameter("@Telefono", (object?)cliente.Telefono ?? DBNull.Value), new SqlParameter("@IdAgencia", idAgencia) }; 
        DataTable tabla = _conexion.Leer("dbo.usp_Cliente_Registrar", parametros); 
        DataRow fila = tabla.Rows[0]; 
        Cliente resultado = new Cliente(Convert.ToInt32(fila["ID"]), idAgencia, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]), Convert.ToString(fila["Email"]), Convert.ToString(fila["Telefono"]), true, null, new List<Proyecto>()); 
        return resultado; 
    }
    public Cliente ModificarCliente(Cliente cliente, int idAgencia) { 
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", cliente.ID), new SqlParameter("@Nombre", cliente.Nombre), new SqlParameter("@RazonSocial", (object?)cliente.RazonSocial ?? DBNull.Value), new SqlParameter("@Email", (object?)cliente.Email ?? DBNull.Value), new SqlParameter("@Telefono", (object?)cliente.Telefono ?? DBNull.Value), new SqlParameter("@IdAgencia", idAgencia) }; DataTable tabla = _conexion.Leer("dbo.usp_Cliente_Modificar", parametros); 
        if (tabla.Rows.Count == 0) { throw new ArgumentException("No se encontró el cliente de la agencia."); } 
        DataRow fila = tabla.Rows[0]; 
        Cliente resultado = new Cliente(Convert.ToInt32(fila["ID"]), idAgencia, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]), Convert.ToString(fila["Email"]), Convert.ToString(fila["Telefono"]), Convert.ToBoolean(fila["Activo"]), null, new List<Proyecto>()); 
        return resultado; 
    }
    public bool CambiarEstadoCliente(int idCliente, int idAgencia, bool activo) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", idCliente), new SqlParameter("@IdAgencia", idAgencia), new SqlParameter("@Activo", activo) }; return _conexion.Escribir("dbo.usp_Cliente_CambiarEstado", parametros); }
    private static List<SqlParameter> CrearParametrosProyecto(Proyecto proyecto) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", proyecto.ID), new SqlParameter("@IdAgencia", proyecto.IdAgencia), new SqlParameter("@IdCliente", proyecto.IdCliente), new SqlParameter("@IdPMResponsable", proyecto.IdPMResponsable), new SqlParameter("@Nombre", proyecto.Nombre), new SqlParameter("@Descripcion", (object?)proyecto.Descripcion ?? DBNull.Value), new SqlParameter("@FechaInicio", (object?)proyecto.FechaInicio ?? DBNull.Value), new SqlParameter("@Deadline", (object?)proyecto.Deadline ?? DBNull.Value), new SqlParameter("@HorasEstimadasTotales", proyecto.HorasEstimadasTotales), new SqlParameter("@Estado", proyecto.Estado) }; return parametros; }
    private static List<Proyecto> CrearProyectos(DataTable tabla) { List<Proyecto> proyectos = new List<Proyecto>(); foreach (DataRow fila in tabla.Rows) { Proyecto proyecto = new Proyecto(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdCliente"]), Convert.ToInt32(fila["IdPMResponsable"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), fila["FechaInicio"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicio"]), fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]), Convert.ToDecimal(fila["HorasEstimadasTotales"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), Convert.ToDateTime(fila["FechaAlta"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"])); proyectos.Add(proyecto); } return proyectos; }
    private static List<Usuario> CrearUsuarios(DataTable tabla) { List<Usuario> usuarios = new List<Usuario>(); foreach (DataRow fila in tabla.Rows) { Usuario usuario = new Usuario(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), new Rol(), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Apellido"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, string.Empty, string.Empty, DateTime.MinValue, true); usuarios.Add(usuario); } return usuarios; }
}
