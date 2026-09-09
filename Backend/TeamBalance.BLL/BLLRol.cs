using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRol
{
    private readonly MPPRol _rolMPP;

    public BLLRol(MPPRol rolMPP)
    {
        _rolMPP = rolMPP;
    }

    public Rol ConsultarRolPorNombre(string nombre)
    {
        return _rolMPP.ConsultarRolPorNombre(nombre);
    }

    public void CargarAutorizacion(Usuario usuario)
    {
        usuario.Roles = _rolMPP.ConsultarRolesUsuario(usuario);
        usuario.Permisos = _rolMPP.ConsultarPermisosUsuario(usuario);
        foreach (Rol rol in usuario.Roles)
        {
            rol.Permisos = _rolMPP.ConsultarPermisosRol(rol);
        }
    }

    public List<Rol> ConsultarRolesActivos()
    {
        List<Rol> roles = _rolMPP.ConsultarRolesActivos();
        foreach (Rol rol in roles)
        {
            rol.Permisos = _rolMPP.ConsultarPermisosRol(rol);
        }
        return roles;
    }

    public List<Permiso> ConsultarPermisosActivos()
    {
        return _rolMPP.ConsultarPermisosActivos();
    }

    public bool TienePermiso(Usuario usuario, string codigoPermiso)
    {
        return usuario.Permisos.Any(permiso => string.Equals(permiso.Codigo, codigoPermiso, StringComparison.OrdinalIgnoreCase));
    }

    public Rol RegistrarRol(Rol rol)
    {
        ValidarRol(rol);
        rol.ID = _rolMPP.RegistrarRol(rol);
        _rolMPP.ReemplazarPermisos(rol);
        return rol;
    }

    public void ModificarRol(Rol rol)
    {
        ValidarRol(rol);
        _rolMPP.ModificarRol(rol);
        _rolMPP.ReemplazarPermisos(rol);
    }

    public void CambiarEstado(Rol rol, bool activo)
    {
        _rolMPP.CambiarEstado(rol, activo);
    }

    private static void ValidarRol(Rol rol)
    {
        if (string.IsNullOrWhiteSpace(rol.Nombre))
        {
            throw new ArgumentException("Ingresá un nombre para el rol.");
        }

        if (rol.Permisos.Count == 0)
        {
            throw new ArgumentException("Asigná al menos un permiso al rol.");
        }
    }
}
