using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPProyecto
{
    private readonly Conexion _conexion;

    public MPPProyecto(Conexion conexion)
    {
        _conexion = conexion;
    }

    public void CrearEstadosBase(Agencia agencia)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", agencia.ID) };
            _conexion.Escribir("dbo.usp_EstadoTarea_CrearBase", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Proyecto> Consultar(Usuario usuario)
    {
        try
        {
            FiltroProyecto filtro = new FiltroProyecto();
            return Consultar(usuario, filtro);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Proyecto> Consultar(Usuario usuario, FiltroProyecto filtro)
    {
        try
        {
            List<SqlParameter> parametros = CrearParametrosFiltro(usuario, filtro);
            DataTable tabla = _conexion.Leer("dbo.usp_Proyecto_Consultar", parametros);
            List<Proyecto> proyectos = CrearProyectos(tabla);
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Proyecto ConsultarDetalle(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametrosProyecto = new List<SqlParameter>() { new SqlParameter("@ID", proyecto.ID), new SqlParameter("@IdAgencia", proyecto.IdAgencia) };
            DataTable tablaProyecto = _conexion.Leer("dbo.usp_Proyecto_ConsultarDetalle", parametrosProyecto);
            List<Proyecto> proyectos = CrearProyectos(tablaProyecto);
            if (proyectos.Count == 0) { throw new KeyNotFoundException("No se encontró el proyecto solicitado."); }
            Proyecto resultado = proyectos[0];
            List<SqlParameter> parametrosTareas = new List<SqlParameter>() { new SqlParameter("@IdProyecto", resultado.ID), new SqlParameter("@IdAgencia", resultado.IdAgencia) };
            DataTable tablaTareas = _conexion.Leer("dbo.usp_Tarea_ConsultarPorProyecto", parametrosTareas);
            resultado.Tareas = CrearTareas(tablaTareas);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Proyecto> ConsultarPorResponsable(Usuario usuario, FiltroProyecto filtro)
    {
        try
        {
            List<SqlParameter> parametros = CrearParametrosFiltro(usuario, filtro);
            parametros.Add(new SqlParameter("@IdUsuario", usuario.ID));
            DataTable tabla = _conexion.Leer("dbo.usp_Proyecto_ConsultarPorPM", parametros);
            List<Proyecto> proyectos = CrearProyectos(tabla);
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Proyecto Guardar(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = CrearParametrosProyecto(proyecto);
            DataTable tabla = _conexion.Leer(proyecto.ID == 0 ? "dbo.usp_Proyecto_Registrar" : "dbo.usp_Proyecto_Modificar", parametros);
            List<Proyecto> proyectos = CrearProyectos(tabla);
            Proyecto resultado = proyectos.Single();
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstado(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", proyecto.ID), new SqlParameter("@IdAgencia", proyecto.IdAgencia), new SqlParameter("@Activo", proyecto.Activo) };
            bool resultado = _conexion.Escribir("dbo.usp_Proyecto_CambiarEstado", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool Cerrar(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", proyecto.ID), new SqlParameter("@IdAgencia", proyecto.IdAgencia) };
            bool resultado = _conexion.Escribir("dbo.usp_Proyecto_Cerrar", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool EsResponsable(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", proyecto.ID), new SqlParameter("@IdAgencia", proyecto.IdAgencia), new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_Proyecto_EsResponsable", parametros);
            bool resultado = tabla.Rows.Count > 0;
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public GestionProyectoOpciones ConsultarOpciones(Usuario usuario)
    {
        try
        {
            int idAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            List<SqlParameter> parametrosClientes = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) };
            List<SqlParameter> parametrosResponsables = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) };
            DataTable tablaClientes = _conexion.Leer("dbo.usp_Cliente_ConsultarPorAgencia", parametrosClientes);
            DataTable tablaResponsables = _conexion.Leer("dbo.usp_Proyecto_ConsultarResponsables", parametrosResponsables);
            List<Cliente> clientes = new List<Cliente>();
            foreach (DataRow fila in tablaClientes.Rows)
            {
                Cliente cliente = new Cliente(Convert.ToInt32(fila["ID"]), idAgencia, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]), Convert.ToString(fila["Email"]), Convert.ToString(fila["Telefono"]), Convert.ToBoolean(fila["Activo"]), null, new List<Proyecto>());
                clientes.Add(cliente);
            }
            List<Usuario> responsables = CrearUsuarios(tablaResponsables);
            GestionProyectoOpciones opciones = new GestionProyectoOpciones(clientes, responsables);
            return opciones;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Cliente RegistrarCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            int idAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Nombre", cliente.Nombre), new SqlParameter("@RazonSocial", (object?)cliente.RazonSocial ?? DBNull.Value), new SqlParameter("@Email", (object?)cliente.Email ?? DBNull.Value), new SqlParameter("@Telefono", (object?)cliente.Telefono ?? DBNull.Value), new SqlParameter("@IdAgencia", idAgencia) };
            DataTable tabla = _conexion.Leer("dbo.usp_Cliente_Registrar", parametros);
            DataRow fila = tabla.Rows[0];
            Cliente resultado = new Cliente(Convert.ToInt32(fila["ID"]), idAgencia, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]), Convert.ToString(fila["Email"]), Convert.ToString(fila["Telefono"]), true, null, new List<Proyecto>());
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Cliente ModificarCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            int idAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", cliente.ID), new SqlParameter("@Nombre", cliente.Nombre), new SqlParameter("@RazonSocial", (object?)cliente.RazonSocial ?? DBNull.Value), new SqlParameter("@Email", (object?)cliente.Email ?? DBNull.Value), new SqlParameter("@Telefono", (object?)cliente.Telefono ?? DBNull.Value), new SqlParameter("@IdAgencia", idAgencia) };
            DataTable tabla = _conexion.Leer("dbo.usp_Cliente_Modificar", parametros);
            if (tabla.Rows.Count == 0) { throw new ArgumentException("No se encontró el cliente de la agencia."); }
            DataRow fila = tabla.Rows[0];
            Cliente resultado = new Cliente(Convert.ToInt32(fila["ID"]), idAgencia, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]), Convert.ToString(fila["Email"]), Convert.ToString(fila["Telefono"]), Convert.ToBoolean(fila["Activo"]), null, new List<Proyecto>());
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoCliente(Cliente cliente)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", cliente.ID), new SqlParameter("@IdAgencia", cliente.IdAgencia), new SqlParameter("@Activo", cliente.Activo) };
            bool resultado = _conexion.Escribir("dbo.usp_Cliente_CambiarEstado", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private static List<SqlParameter> CrearParametrosProyecto(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", proyecto.ID), new SqlParameter("@IdAgencia", proyecto.IdAgencia), new SqlParameter("@IdCliente", proyecto.IdCliente), new SqlParameter("@IdPMResponsable", proyecto.IdPMResponsable), new SqlParameter("@Nombre", proyecto.Nombre), new SqlParameter("@Descripcion", (object?)proyecto.Descripcion ?? DBNull.Value), new SqlParameter("@FechaInicio", (object?)proyecto.FechaInicio ?? DBNull.Value), new SqlParameter("@Deadline", (object?)proyecto.Deadline ?? DBNull.Value), new SqlParameter("@HorasEstimadasTotales", proyecto.HorasEstimadasTotales), new SqlParameter("@Estado", proyecto.Estado) };
            return parametros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<SqlParameter> CrearParametrosFiltro(Usuario usuario, FiltroProyecto filtro)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@IdCliente", (object?)filtro.IdCliente ?? DBNull.Value), new SqlParameter("@IdPMResponsable", (object?)filtro.IdPMResponsable ?? DBNull.Value), new SqlParameter("@Estado", (object?)filtro.Estado ?? DBNull.Value), new SqlParameter("@FechaDesde", (object?)filtro.FechaDesde ?? DBNull.Value), new SqlParameter("@FechaHasta", (object?)filtro.FechaHasta ?? DBNull.Value) };
            return parametros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Proyecto> CrearProyectos(DataTable tabla)
    {
        try
        {
            List<Proyecto> proyectos = new List<Proyecto>();
            foreach (DataRow fila in tabla.Rows)
            {
                Proyecto proyecto = new Proyecto(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdCliente"]), Convert.ToInt32(fila["IdPMResponsable"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), fila["FechaInicio"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicio"]), fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]), Convert.ToDecimal(fila["HorasEstimadasTotales"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), Convert.ToDateTime(fila["FechaAlta"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]));
                proyectos.Add(proyecto);
            }
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Tarea> CrearTareas(DataTable tabla)
    {
        try
        {
            List<Tarea> tareas = new List<Tarea>();
            foreach (DataRow fila in tabla.Rows)
            {
                Tarea tarea = new Tarea(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdProyecto"]), fila["IdEmpleadoAsignado"] == DBNull.Value ? null : Convert.ToInt32(fila["IdEmpleadoAsignado"]), fila["IdSkillRequerido"] == DBNull.Value ? null : Convert.ToInt32(fila["IdSkillRequerido"]), Convert.ToInt32(fila["IdEstadoTarea"]), fila["IdTareaPredecesora"] == DBNull.Value ? null : Convert.ToInt32(fila["IdTareaPredecesora"]), Convert.ToString(fila["Titulo"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), Convert.ToString(fila["Estado"]), Convert.ToString(fila["Prioridad"]), Convert.ToString(fila["Complejidad"]), fila["FechaInicio"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicio"]), fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]), fila["FechaFinReal"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaFinReal"]), Convert.ToString(fila["SeniorityRequerido"]), Convert.ToString(fila["ChecklistJson"]), Convert.ToString(fila["ComentariosJson"]), Convert.ToString(fila["ArchivosAdjuntosJson"]), Convert.ToBoolean(fila["Bloqueada"]), Convert.ToString(fila["MotivoBloqueo"]), Convert.ToDecimal(fila["PorcentajeAvance"]), Convert.ToDecimal(fila["HorasEstimadas"]), Convert.ToBoolean(fila["Activo"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]));
                tareas.Add(tarea);
            }
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Usuario> CrearUsuarios(DataTable tabla)
    {
        try
        {
            List<Usuario> usuarios = new List<Usuario>();
            foreach (DataRow fila in tabla.Rows)
            {
                Usuario usuario = new Usuario(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), new Rol(), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Apellido"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, string.Empty, string.Empty, DateTime.MinValue, true);
                usuarios.Add(usuario);
            }
            return usuarios;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
