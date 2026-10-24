using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLProyecto
{
    private readonly MPPProyecto _proyectoMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLProyecto(MPPProyecto proyectoMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _proyectoMPP = proyectoMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
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
            ValidarGestionProyectos(usuario);
            List<Proyecto> proyectos = EsDueno(usuario) ? _proyectoMPP.Consultar(usuario) : _proyectoMPP.ConsultarPorResponsable(usuario, new FiltroProyecto());
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Proyecto> Consultar(Usuario usuario, FiltroProyecto filtro)
    {
        try
        {
            ValidarGestionProyectos(usuario);
            List<Proyecto> proyectos = EsDueno(usuario) ? _proyectoMPP.Consultar(usuario, filtro) : _proyectoMPP.ConsultarPorResponsable(usuario, filtro);
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Proyecto ConsultarDetalle(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            proyecto.IdAgencia = ObtenerAgencia(usuario);
            ValidarAccesoProyecto(proyecto, usuario);
            Proyecto resultado = _proyectoMPP.ConsultarDetalle(proyecto);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public GestionProyectoOpciones ConsultarOpciones(Usuario usuario)
    {
        try
        {
            ValidarGestionProyectos(usuario);
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
            ValidarGestionProyectos(usuario);
            if (proyecto.ID == 0) { ValidarDueno(usuario); }
            else { PreservarDatosNoOperativos(proyecto, usuario); }
            if (string.IsNullOrWhiteSpace(proyecto.Nombre) || proyecto.IdCliente <= 0 || proyecto.IdPMResponsable <= 0) { throw new ArgumentException("Completá nombre, cliente y responsable del proyecto."); }
            proyecto.Nombre = proyecto.Nombre.Trim();
            proyecto.Estado = string.IsNullOrWhiteSpace(proyecto.Estado) ? "Planificado" : proyecto.Estado.Trim();
            bool esAlta = proyecto.ID == 0;
            Proyecto resultado = _proyectoMPP.Guardar(proyecto);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Proyecto", resultado.ID, esAlta ? "RegistrarProyecto" : "ModificarProyecto", esAlta ? $"Se registró el proyecto {resultado.Nombre}." : $"Se modificó el proyecto {resultado.Nombre}.", "Exitoso", "Informacion", "Proyectos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Cliente RegistrarCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            ValidarDueno(usuario);
            if (string.IsNullOrWhiteSpace(cliente.Nombre)) { throw new ArgumentException("Ingresá el nombre del cliente."); }
            cliente.Nombre = cliente.Nombre.Trim();
            Cliente resultado = _proyectoMPP.RegistrarCliente(cliente, usuario);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Cliente", resultado.ID, "RegistrarCliente", $"Se registró el cliente {resultado.Nombre}.", "Exitoso", "Informacion", "Proyectos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Cliente ModificarCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            ValidarDueno(usuario);
            if (string.IsNullOrWhiteSpace(cliente.Nombre)) { throw new ArgumentException("Ingresá el nombre del cliente."); }
            cliente.Nombre = cliente.Nombre.Trim();
            Cliente resultado = _proyectoMPP.ModificarCliente(cliente, usuario);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Cliente", resultado.ID, "ModificarCliente", $"Se modificó el cliente {resultado.Nombre}.", "Exitoso", "Informacion", "Proyectos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoCliente(Cliente cliente, Usuario usuario)
    {
        try
        {
            cliente.IdAgencia = ObtenerAgencia(usuario);
            ValidarDueno(usuario);
            bool resultado = _proyectoMPP.CambiarEstadoCliente(cliente);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Cliente", cliente.ID, cliente.Activo ? "ActivarCliente" : "DarBajaCliente", cliente.Activo ? "Se activó un cliente." : "Se dio de baja un cliente.", "Exitoso", "Informacion", "Proyectos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstado(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            proyecto.IdAgencia = ObtenerAgencia(usuario);
            ValidarDueno(usuario);
            bool resultado = _proyectoMPP.CambiarEstado(proyecto);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Proyecto", proyecto.ID, proyecto.Activo ? "ActivarProyecto" : "DarBajaProyecto", proyecto.Activo ? "Se activó un proyecto." : "Se dio de baja un proyecto.", "Exitoso", "Informacion", "Proyectos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool Cerrar(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            proyecto.IdAgencia = ObtenerAgencia(usuario);
            ValidarDueno(usuario);
            Proyecto detalle = _proyectoMPP.ConsultarDetalle(proyecto);
            bool resultado = _proyectoMPP.Cerrar(detalle);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Proyecto", detalle.ID, "CerrarProyecto", $"Se cerró el proyecto {detalle.Nombre}.", "Exitoso", "Informacion", "Proyectos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void PreservarDatosNoOperativos(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            Proyecto actual = _proyectoMPP.ConsultarDetalle(proyecto);
            if (EsDueno(usuario)) { return; }
            if (!_proyectoMPP.EsResponsable(actual, usuario)) { throw new UnauthorizedAccessException("Sólo podés modificar los proyectos a tu cargo."); }
            proyecto.IdCliente = actual.IdCliente;
            proyecto.IdPMResponsable = actual.IdPMResponsable;
            proyecto.Nombre = actual.Nombre;
            proyecto.Activo = actual.Activo;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarAccesoProyecto(Proyecto proyecto, Usuario usuario)
    {
        try
        {
            ValidarGestionProyectos(usuario);
            if (EsDueno(usuario)) { return; }
            if (!_proyectoMPP.EsResponsable(proyecto, usuario)) { throw new UnauthorizedAccessException("No tenés acceso a este proyecto."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarGestionProyectos(Usuario usuario)
    {
        try
        {
            ObtenerAgencia(usuario);
            if (!_rolBLL.TienePermiso(usuario, "GestionarProyectos")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar proyectos."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static bool EsDueno(Usuario usuario)
    {
        try
        {
            bool resultado = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Dueno", StringComparison.OrdinalIgnoreCase));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarDueno(Usuario usuario)
    {
        try
        {
            ValidarGestionProyectos(usuario);
            if (!EsDueno(usuario)) { throw new UnauthorizedAccessException("Esta operación sólo puede realizarla el dueño de la agencia."); }
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
