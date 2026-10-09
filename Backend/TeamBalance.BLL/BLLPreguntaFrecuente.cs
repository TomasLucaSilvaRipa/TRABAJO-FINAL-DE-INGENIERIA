using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public sealed class BLLPreguntaFrecuente
{
    private readonly MPPPreguntaFrecuente _preguntaFrecuenteMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLPreguntaFrecuente(MPPPreguntaFrecuente preguntaFrecuenteMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL) {
        _preguntaFrecuenteMPP = preguntaFrecuenteMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<PreguntaFrecuente> ConsultarPublicas() {
        return _preguntaFrecuenteMPP.ConsultarPublicas();
    }

    public List<PreguntaFrecuente> ConsultarGestion(Usuario usuario) {
        ExigirGestion(usuario);
        return _preguntaFrecuenteMPP.ConsultarGestion();
    }

    public PreguntaFrecuente Guardar(PreguntaFrecuente pregunta, Usuario usuario) {
        ExigirGestion(usuario);
        Validar(pregunta);
        PreguntaFrecuente resultado = _preguntaFrecuenteMPP.Guardar(pregunta);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "PreguntaFrecuente", resultado.ID, pregunta.ID == 0 ? "CrearPreguntaFrecuente" : "ModificarPreguntaFrecuente", "Se guardó una pregunta frecuente.", "Exitoso", "Informacion", "FAQs"));
        return resultado;
    }

    public void DarDeBaja(PreguntaFrecuente pregunta, Usuario usuario) {
        ExigirGestion(usuario);
        if (pregunta.ID <= 0) { throw new ArgumentException("Seleccioná una pregunta frecuente válida."); }
        _preguntaFrecuenteMPP.DarDeBaja(pregunta);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "PreguntaFrecuente", pregunta.ID, "BajarPreguntaFrecuente", "Se dio de baja una pregunta frecuente.", "Exitoso", "Informacion", "FAQs"));
    }

    private void ExigirGestion(Usuario usuario) {
        if (!usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase)) || !_rolBLL.TienePermiso(usuario, "GestionarFaqs")) { throw new UnauthorizedAccessException("No tenés permiso para administrar las preguntas frecuentes."); }
    }

    private static void Validar(PreguntaFrecuente pregunta) {
        if (!Enum.IsDefined(pregunta.Categoria)) { throw new ArgumentException("Seleccioná una categoría válida."); }
        if (string.IsNullOrWhiteSpace(pregunta.PreguntaEs) || pregunta.PreguntaEs.Trim().Length > 300 || string.IsNullOrWhiteSpace(pregunta.RespuestaEs) || pregunta.RespuestaEs.Trim().Length > 4000) { throw new ArgumentException("Completá la pregunta y la respuesta en español."); }
        if (string.IsNullOrWhiteSpace(pregunta.PreguntaEn) || pregunta.PreguntaEn.Trim().Length > 300 || string.IsNullOrWhiteSpace(pregunta.RespuestaEn) || pregunta.RespuestaEn.Trim().Length > 4000) { throw new ArgumentException("Completá la pregunta y la respuesta en inglés."); }
        pregunta.PreguntaEs = pregunta.PreguntaEs.Trim();
        pregunta.RespuestaEs = pregunta.RespuestaEs.Trim();
        pregunta.PreguntaEn = pregunta.PreguntaEn.Trim();
        pregunta.RespuestaEn = pregunta.RespuestaEn.Trim();
    }
}
