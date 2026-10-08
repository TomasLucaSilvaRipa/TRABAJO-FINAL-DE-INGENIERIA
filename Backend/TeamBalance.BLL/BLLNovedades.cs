using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public sealed class BLLNovedades
{
    private readonly MPPNovedades _novedadesMPP;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;
    private readonly BLLRol _rolBLL;
    public BLLNovedades(MPPNovedades novedadesMPP, BLLBitacora bitacoraBLL, EmailService emailService, BLLRol rolBLL) { _novedadesMPP = novedadesMPP; _bitacoraBLL = bitacoraBLL; _emailService = emailService; _rolBLL = rolBLL; }
    public List<CategoriaNoticia> Categorias() => _novedadesMPP.Categorias();
    public List<Noticia> Publicas() => _novedadesMPP.Publicas();
    public List<int> Preferencias(Usuario usuario) { ExigirPermiso(usuario, "GestionarNewsletter"); return _novedadesMPP.Preferencias(usuario); }
    public void ValidarPreferencias(PreferenciasNewsletterRequest solicitud)
    {
        List<int> validas = _novedadesMPP.Categorias().Where(c => c.Activo).Select(c => c.ID).ToList();
        if (solicitud.CategoriasIds.Any(id => !validas.Contains(id))) throw new ArgumentException("Una de las categorías seleccionadas ya no está disponible.");
    }
    public void GuardarPreferencias(Usuario usuario, PreferenciasNewsletterRequest solicitud)
    {
        ExigirPermiso(usuario, "GestionarNewsletter");
        ValidarPreferencias(solicitud);
        _novedadesMPP.GuardarPreferencias(usuario, solicitud);
    }
    public List<Noticia> Gestion(Usuario usuario) { ExigirGestionNovedades(usuario); return _novedadesMPP.Gestion(); }
    public async Task<Noticia> Guardar(Usuario usuario, GuardarNoticiaRequest solicitud)
    {
        ExigirGestionNovedades(usuario); ValidarNoticia(solicitud);
        Noticia noticia = _novedadesMPP.Guardar(solicitud);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "Noticia", noticia.ID, "PublicarNoticia", $"Se publicó la novedad '{noticia.Titulo}'.", "Exitoso", "Informacion", "Novedades"));
        await DifundirPendientes();
        return noticia;
    }
    public void Bajar(Usuario usuario, Noticia noticia) { ExigirGestionNovedades(usuario); _novedadesMPP.Bajar(noticia); _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "Noticia", noticia.ID, "BajarNoticia", "Se dio de baja una novedad.", "Exitoso", "Informacion", "Novedades")); }
    public async Task<int> DifundirPendientes()
    {
        int enviadas = 0;
        foreach (Noticia noticia in _novedadesMPP.PendientesDifusion())
        {
            foreach ((string email, string nombre) in _novedadesMPP.Destinatarios(noticia)) if (await _emailService.EnviarNewsletter(email, nombre, noticia.Titulo, noticia.Contenido, noticia.Categoria)) enviadas++;
            _novedadesMPP.MarcarDifundida(noticia);
        }
        return enviadas;
    }
    private void ExigirGestionNovedades(Usuario usuario) { if (!usuario.Roles.Any(r => string.Equals(r.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase)) || !_rolBLL.TienePermiso(usuario, "GestionarNovedades")) throw new UnauthorizedAccessException("No tenés permiso para administrar novedades."); }
    private void ExigirPermiso(Usuario usuario, string permiso) { if (!_rolBLL.TienePermiso(usuario, permiso)) throw new UnauthorizedAccessException("No tenés permiso para realizar esta operación."); }
    private static void ValidarNoticia(GuardarNoticiaRequest r)
    {
        if (r.IdCategoriaNoticia <= 0 || string.IsNullOrWhiteSpace(r.Titulo) || r.Titulo.Trim().Length > 200 || string.IsNullOrWhiteSpace(r.Contenido) || r.Contenido.Trim().Length > 8000) throw new ArgumentException("Completá categoría, título y contenido de la novedad.");
        if (r.FechaVencimiento.HasValue && r.FechaVencimiento.Value <= (r.FechaPublicacion ?? DateTime.Now)) throw new ArgumentException("La fecha de vencimiento debe ser posterior a la publicación.");
        r.Titulo = r.Titulo.Trim(); r.Contenido = r.Contenido.Trim(); r.ImagenUrl = string.IsNullOrWhiteSpace(r.ImagenUrl) ? null : r.ImagenUrl.Trim();
    }
}
