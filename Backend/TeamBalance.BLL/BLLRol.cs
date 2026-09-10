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
        if (usuario.Roles.Count > 0)
        {
            usuario.Rol = usuario.Roles[0];
        }
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

    public List<Rol> ConsultarRolesGestion(Usuario solicitante)
    {
        List<Rol> roles = ConsultarRolesActivos();
        if (EsSoporte(solicitante))
        {
            return roles;
        }

        return roles.Where(rol => rol.TipoUsuario != "Soporte" && (rol.EsRolBase || rol.IdAgencia == solicitante.IdAgencia)).ToList();
    }

    public List<Rol> ConsultarRolesAsignablesAgencia(Usuario solicitante)
    {
        return ConsultarRolesGestion(solicitante).Where(rol => rol.TipoUsuario != "Soporte").ToList();
    }

    public List<Permiso> ConsultarPermisosActivos()
    {
        return _rolMPP.ConsultarPermisosActivos();
    }

    public bool TienePermiso(Usuario usuario, string codigoPermiso)
    {
        return usuario.Permisos.Any(permiso => string.Equals(permiso.Codigo, codigoPermiso, StringComparison.OrdinalIgnoreCase));
    }

    public Rol RegistrarRol(Rol rol, Usuario solicitante)
    {
        ValidarRol(rol);
        ValidarGestionRol(rol, solicitante);
        rol.ID = _rolMPP.RegistrarRol(rol);
        _rolMPP.ReemplazarPermisos(rol);
        return rol;
    }

    public void ModificarRol(Rol rol, Usuario solicitante)
    {
        ValidarRol(rol);
        if (!EsSoporte(solicitante) && !ConsultarRolesGestion(solicitante).Any(item => item.ID == rol.ID && !item.EsRolBase))
        {
            throw new UnauthorizedAccessException("No tenés permiso para modificar este rol.");
        }
        ValidarGestionRol(rol, solicitante);
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

        if (rol.TipoUsuario != "Dueno" && rol.TipoUsuario != "PM" && rol.TipoUsuario != "Empleado" && rol.TipoUsuario != "Soporte")
        {
            throw new ArgumentException("Seleccioná el tipo de usuario del rol.");
        }

        if (rol.TipoUsuario != "Soporte" && rol.Permisos.Any(permiso => string.Equals(permiso.Codigo, "ConsultarBitacora", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("La bitácora sólo puede asignarse a roles de tipo Soporte.");
        }
    }

    private static bool EsSoporte(Usuario usuario)
    {
        return usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidarGestionRol(Rol rol, Usuario solicitante)
    {
        if (EsSoporte(solicitante))
        {
            return;
        }

        if (rol.TipoUsuario == "Soporte")
        {
            throw new UnauthorizedAccessException("Sólo Soporte puede gestionar roles internos de TeamBalance.");
        }

        if (solicitante.IdAgencia is null)
        {
            throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.");
        }

        rol.IdAgencia = solicitante.IdAgencia;
    }
}
