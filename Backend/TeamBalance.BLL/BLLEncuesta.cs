using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public sealed class BLLEncuesta
{
    private readonly MPPEncuesta _encuestaMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLEncuesta(MPPEncuesta encuestaMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _encuestaMPP = encuestaMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<Encuesta> ConsultarPublicas()
    {
        return _encuestaMPP.ConsultarPublicas();
    }

    public Encuesta ConsultarPublica(Encuesta encuesta)
    {
        return _encuestaMPP.ConsultarPublica(encuesta);
    }

    public List<Encuesta> ConsultarGestion(Usuario usuario)
    {
        ExigirGestion(usuario);
        return _encuestaMPP.ConsultarGestion();
    }

    public Encuesta ConsultarGestion(Encuesta encuesta, Usuario usuario)
    {
        ExigirGestion(usuario);
        return _encuestaMPP.ConsultarGestion(encuesta);
    }

    public Encuesta Guardar(Encuesta encuesta, Usuario usuario)
    {
        ExigirGestion(usuario);
        ValidarEncuesta(encuesta);
        Encuesta resultado = _encuestaMPP.Guardar(encuesta);
        string accion = encuesta.ID == 0 ? "CrearEncuesta" : "ModificarEncuesta";
        _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "Encuesta", resultado.ID, accion, "Se guardó una encuesta pública.", "Exitoso", "Informacion", "Encuestas"));
        return resultado;
    }

    public void DarDeBaja(Encuesta encuesta, Usuario usuario)
    {
        ExigirGestion(usuario);
        if (encuesta.ID <= 0) { throw new ArgumentException("Seleccioná una encuesta válida."); }
        _encuestaMPP.DarDeBaja(encuesta);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, null, "Encuesta", encuesta.ID, "BajarEncuesta", "Se dio de baja una encuesta pública.", "Exitoso", "Informacion", "Encuestas"));
    }

    public void Responder(RespuestaEncuesta respuesta)
    {
        ValidarRespuesta(respuesta);
        try
        {
            _encuestaMPP.Responder(respuesta);
        }
        catch (Exception ex) when (EsErrorDeRespuesta(ex.Message)) { throw new ArgumentException(ex.Message); }
    }

    public ResultadoEncuesta ConsultarResultados(Encuesta encuesta, Usuario usuario)
    {
        ExigirGestion(usuario);
        if (encuesta.ID <= 0) { throw new ArgumentException("Seleccioná una encuesta válida."); }
        return _encuestaMPP.ConsultarResultados(encuesta);
    }

    private void ExigirGestion(Usuario usuario)
    {
        if (!usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase)) || !_rolBLL.TienePermiso(usuario, "GestionarEncuestas")) { throw new UnauthorizedAccessException("No tenés permiso para administrar las encuestas."); }
    }

    private static void ValidarEncuesta(Encuesta encuesta)
    {
        if (string.IsNullOrWhiteSpace(encuesta.TituloEs) || encuesta.TituloEs.Trim().Length > 180 || string.IsNullOrWhiteSpace(encuesta.TituloEn) || encuesta.TituloEn.Trim().Length > 180) { throw new ArgumentException("Completá el título de la encuesta en ambos idiomas."); }
        if (string.IsNullOrWhiteSpace(encuesta.DescripcionEs) || encuesta.DescripcionEs.Trim().Length > 600 || string.IsNullOrWhiteSpace(encuesta.DescripcionEn) || encuesta.DescripcionEn.Trim().Length > 600) { throw new ArgumentException("Completá la descripción de la encuesta en ambos idiomas."); }
        if (encuesta.FechaInicio == default || encuesta.FechaVencimiento == default || encuesta.FechaVencimiento <= encuesta.FechaInicio) { throw new ArgumentException("Indicá una vigencia válida para la encuesta."); }
        if (encuesta.Preguntas.Count == 0 || encuesta.Preguntas.Count > 10) { throw new ArgumentException("La encuesta debe tener entre una y diez preguntas."); }
        if (encuesta.Preguntas.Select(pregunta => pregunta.Orden).Distinct().Count() != encuesta.Preguntas.Count) { throw new ArgumentException("El orden de las preguntas no puede repetirse."); }

        foreach (PreguntaEncuesta pregunta in encuesta.Preguntas)
        {
            if (string.IsNullOrWhiteSpace(pregunta.EnunciadoEs) || pregunta.EnunciadoEs.Trim().Length > 300 || string.IsNullOrWhiteSpace(pregunta.EnunciadoEn) || pregunta.EnunciadoEn.Trim().Length > 300) { throw new ArgumentException("Completá cada pregunta en español e inglés."); }
            if (pregunta.Opciones.Count < 2 || pregunta.Opciones.Count > 6) { throw new ArgumentException("Cada pregunta debe tener entre dos y seis opciones cerradas."); }
            if (pregunta.Opciones.Select(opcion => opcion.Orden).Distinct().Count() != pregunta.Opciones.Count) { throw new ArgumentException("El orden de las opciones no puede repetirse."); }
            foreach (OpcionEncuesta opcion in pregunta.Opciones)
            {
                if (string.IsNullOrWhiteSpace(opcion.TextoEs) || opcion.TextoEs.Trim().Length > 200 || string.IsNullOrWhiteSpace(opcion.TextoEn) || opcion.TextoEn.Trim().Length > 200) { throw new ArgumentException("Completá cada opción en español e inglés."); }
                opcion.TextoEs = opcion.TextoEs.Trim();
                opcion.TextoEn = opcion.TextoEn.Trim();
            }
            pregunta.EnunciadoEs = pregunta.EnunciadoEs.Trim();
            pregunta.EnunciadoEn = pregunta.EnunciadoEn.Trim();
        }
        encuesta.TituloEs = encuesta.TituloEs.Trim();
        encuesta.TituloEn = encuesta.TituloEn.Trim();
        encuesta.DescripcionEs = encuesta.DescripcionEs.Trim();
        encuesta.DescripcionEn = encuesta.DescripcionEn.Trim();
    }

    private static void ValidarRespuesta(RespuestaEncuesta respuesta)
    {
        if (respuesta.IdEncuesta <= 0 || string.IsNullOrWhiteSpace(respuesta.IdentificadorParticipante) || respuesta.IdentificadorParticipante.Trim().Length > 64) { throw new ArgumentException("No pudimos validar la respuesta de la encuesta."); }
        if (respuesta.Respuestas.Count == 0) { throw new ArgumentException("Respondé todas las preguntas para enviar la encuesta."); }
        respuesta.IdentificadorParticipante = respuesta.IdentificadorParticipante.Trim();
    }

    private static bool EsErrorDeRespuesta(string mensaje)
    {
        return mensaje.Contains("La encuesta ya no está disponible.", StringComparison.Ordinal) || mensaje.Contains("Ya registramos una respuesta desde este dispositivo.", StringComparison.Ordinal) || mensaje.Contains("Respondé todas las preguntas de la encuesta.", StringComparison.Ordinal) || mensaje.Contains("La encuesta contiene respuestas duplicadas.", StringComparison.Ordinal) || mensaje.Contains("Una de las opciones elegidas no corresponde a la encuesta.", StringComparison.Ordinal);
    }
}
