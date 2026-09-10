using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;
public class BLLProyecto
{
    private readonly MPPProyecto _proyectoMPP;
    public BLLProyecto(MPPProyecto proyectoMPP) { _proyectoMPP = proyectoMPP; }
    public void CrearEstadosBase(int idAgencia) { _proyectoMPP.CrearEstadosBase(idAgencia); }
    public List<Proyecto> Consultar(Usuario usuario) { return _proyectoMPP.Consultar(ObtenerAgencia(usuario)); }
    public GestionProyectoOpciones ConsultarOpciones(Usuario usuario) { return _proyectoMPP.ConsultarOpciones(ObtenerAgencia(usuario)); }
    public Proyecto Guardar(Proyecto proyecto, Usuario usuario) { proyecto.IdAgencia = ObtenerAgencia(usuario); if (string.IsNullOrWhiteSpace(proyecto.Nombre) || proyecto.IdCliente <= 0 || proyecto.IdPMResponsable <= 0) { throw new ArgumentException("Completá nombre, cliente y responsable del proyecto."); } proyecto.Nombre = proyecto.Nombre.Trim(); proyecto.Estado = string.IsNullOrWhiteSpace(proyecto.Estado) ? "Planificado" : proyecto.Estado.Trim(); return _proyectoMPP.Guardar(proyecto); }
    public Cliente RegistrarCliente(Cliente cliente, Usuario usuario) { if (string.IsNullOrWhiteSpace(cliente.Nombre)) { throw new ArgumentException("Ingresá el nombre del cliente."); } cliente.Nombre = cliente.Nombre.Trim(); return _proyectoMPP.RegistrarCliente(cliente, ObtenerAgencia(usuario)); }
    public Cliente ModificarCliente(Cliente cliente, Usuario usuario) { if (string.IsNullOrWhiteSpace(cliente.Nombre)) { throw new ArgumentException("Ingresá el nombre del cliente."); } cliente.Nombre = cliente.Nombre.Trim(); return _proyectoMPP.ModificarCliente(cliente, ObtenerAgencia(usuario)); }
    public bool CambiarEstadoCliente(int id, bool activo, Usuario usuario) { return _proyectoMPP.CambiarEstadoCliente(id, ObtenerAgencia(usuario), activo); }
    public bool CambiarEstado(int id, bool activo, Usuario usuario) { return _proyectoMPP.CambiarEstado(id, ObtenerAgencia(usuario), activo); }
    private static int ObtenerAgencia(Usuario usuario) { if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); } return usuario.IdAgencia.Value; }
}
