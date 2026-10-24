using System.Text.RegularExpressions;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLIdioma
{
    private static readonly Regex CodigoValido = new("^[a-z]{2,10}(?:-[A-Z]{2})?$", RegexOptions.CultureInvariant);
    private readonly MPPIdioma _idiomaMPP;
    private readonly BLLRol _rolBLL;

    public BLLIdioma(MPPIdioma idiomaMPP, BLLRol rolBLL) { _idiomaMPP = idiomaMPP; _rolBLL = rolBLL; }

    public List<Idioma> ConsultarActivos() => _idiomaMPP.ConsultarActivos();

    public List<Idioma> ConsultarGestion(Usuario solicitante)
    {
        ValidarGestion(solicitante);
        return _idiomaMPP.ConsultarGestion();
    }

    public Idioma Registrar(Idioma idioma, Usuario solicitante)
    {
        ValidarGestion(solicitante);
        Normalizar(idioma);
        return _idiomaMPP.Registrar(idioma);
    }

    public void Modificar(Idioma idioma, Usuario solicitante)
    {
        ValidarGestion(solicitante);
        if (idioma.ID <= 0) throw new ArgumentException("El idioma indicado no es válido.");
        Idioma? idiomaRegistrado = _idiomaMPP.ConsultarPorId(idioma);
        if (idiomaRegistrado is null) throw new ArgumentException("No existe el idioma indicado.");
        Normalizar(idioma);
        bool cambiaCodigo = !string.Equals(idiomaRegistrado.Codigo, idioma.Codigo, StringComparison.Ordinal);
        bool cambiaNombre = !string.Equals(idiomaRegistrado.Nombre, idioma.Nombre, StringComparison.Ordinal);
        if (idiomaRegistrado.Codigo is "es" or "en" or "fr" && (cambiaCodigo || cambiaNombre)) throw new ArgumentException("El código y el nombre de un idioma base no pueden modificarse.");
        if (idiomaRegistrado.Activo && cambiaCodigo) throw new ArgumentException("Para corregir el código, primero tenés que dar de baja el idioma.");
        if (idiomaRegistrado.Codigo is "es" or "en" or "fr" && !idioma.Activo) throw new ArgumentException("Los idiomas base no pueden darse de baja.");
        if (idiomaRegistrado.Activo && !idioma.Activo) throw new ArgumentException("La baja debe realizarse desde la acción Dar de baja idioma.");
        _idiomaMPP.Modificar(idioma);
    }

    public void DarDeBaja(Idioma idioma, Usuario solicitante)
    {
        ValidarGestion(solicitante);
        if (idioma.ID <= 0) throw new ArgumentException("El idioma indicado no es válido.");
        Idioma? idiomaRegistrado = _idiomaMPP.ConsultarPorId(idioma);
        if (idiomaRegistrado is null) throw new ArgumentException("No existe el idioma indicado.");
        if (idiomaRegistrado.Codigo is "es" or "en" or "fr") throw new ArgumentException("Los idiomas base no pueden darse de baja.");
        if (!idiomaRegistrado.Activo) throw new ArgumentException("El idioma ya se encuentra dado de baja.");
        _idiomaMPP.DarDeBaja(idiomaRegistrado);
    }

    private void ValidarGestion(Usuario solicitante)
    {
        if (solicitante.ID <= 0 || !_rolBLL.TienePermiso(solicitante, "GestionarIdiomas"))
            throw new UnauthorizedAccessException("No tenés permiso para gestionar idiomas.");
    }

    private static void Normalizar(Idioma idioma)
    {
        idioma.Codigo = (idioma.Codigo ?? string.Empty).Trim();
        idioma.Nombre = (idioma.Nombre ?? string.Empty).Trim();
        if (!CodigoValido.IsMatch(idioma.Codigo)) throw new ArgumentException("Usá un código ISO válido, por ejemplo fr o pt-BR.");
        if (idioma.Nombre.Length is < 2 or > 100) throw new ArgumentException("El nombre del idioma debe tener entre 2 y 100 caracteres.");
        if (idioma.Traducciones is null) idioma.Traducciones = new Dictionary<string, string>();
        if (idioma.Traducciones.Count > 5000) throw new ArgumentException("El idioma supera la cantidad máxima de traducciones.");
        idioma.Traducciones = idioma.Traducciones
            .Where(item => !string.IsNullOrWhiteSpace(item.Key) && item.Key.Length <= 160 && item.Value?.Length <= 4000)
            .ToDictionary(item => item.Key.Trim(), item => item.Value?.Trim() ?? string.Empty, StringComparer.Ordinal);
    }
}
