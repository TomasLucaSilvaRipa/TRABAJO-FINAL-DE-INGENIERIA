using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLProyecto
{
    private readonly MPPProyecto _proyectoMPP;

    public BLLProyecto(MPPProyecto proyectoMPP)
    {
        _proyectoMPP = proyectoMPP;
    }

    public void CrearEstadosBase(Agencia agencia)
    {
        try
        {
            _proyectoMPP.CrearEstadosBase(agencia);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Proyecto> Consultar(Usuario usuario)
    {
        try
        {
            ObtenerAgencia(usuario);
            List<Proyecto> proyectos = _proyectoMPP.Consultar(usuario);
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public GestionProyectoOpciones ConsultarOpciones(Usuario usuario)
    {
        try
        {
            ObtenerAgencia(usuario);
            GestionProyectoOpciones opciones = _proyectoMPP.ConsultarOpciones(usuario);
            return opciones;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Proyecto Guardar(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            proyecto.IdAgencia = ObtenerAgencia(usuario);
            if (string.IsNullOrWhiteSpace(proyecto.Nombre) || proyecto.IdCliente <= 0 || proyecto.IdPMResponsable <= 0) { throw new ArgumentException("Completá nombre, cliente y responsable del proyecto."); }
            proyecto.Nombre = proyecto.Nombre.Trim();
            proyecto.Estado = string.IsNullOrWhiteSpace(proyecto.Estado) ? "Planificado" : proyecto.Estado.Trim();
            Proyecto resultado = _proyectoMPP.Guardar(proyecto);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Cliente RegistrarCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            ObtenerAgencia(usuario);
            if (string.IsNullOrWhiteSpace(cliente.Nombre)) { throw new ArgumentException("Ingresá el nombre del cliente."); }
            cliente.Nombre = cliente.Nombre.Trim();
            Cliente resultado = _proyectoMPP.RegistrarCliente(cliente, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Cliente ModificarCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            ObtenerAgencia(usuario);
            if (string.IsNullOrWhiteSpace(cliente.Nombre)) { throw new ArgumentException("Ingresá el nombre del cliente."); }
            cliente.Nombre = cliente.Nombre.Trim();
            Cliente resultado = _proyectoMPP.ModificarCliente(cliente, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            cliente.IdAgencia = ObtenerAgencia(usuario);
            bool resultado = _proyectoMPP.CambiarEstadoCliente(cliente);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstado(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            proyecto.IdAgencia = ObtenerAgencia(usuario);
            bool resultado = _proyectoMPP.CambiarEstado(proyecto);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static int ObtenerAgencia(Usuario usuario)
    {
        try
        {
            if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
            int idAgencia = usuario.IdAgencia.Value;
            return idAgencia;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
