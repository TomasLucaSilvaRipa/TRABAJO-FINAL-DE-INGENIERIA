using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLOpinionServicio
{
    private readonly MPPOpinionServicio _opinionMPP;
    private readonly BLLBitacora _bitacoraBLL;
    public BLLOpinionServicio(MPPOpinionServicio opinionMPP, BLLBitacora bitacoraBLL) { _opinionMPP = opinionMPP; _bitacoraBLL = bitacoraBLL; }
    public List<OpinionServicio> ConsultarPublicas() { List<OpinionServicio> opiniones = _opinionMPP.ConsultarPublicas(); return opiniones; }
    public OpinionServicio Guardar(OpinionServicio opinion, Usuario usuario)
    {
        if (opinion.Calificacion < 1 || opinion.Calificacion > 5) { throw new ArgumentException("Seleccioná una calificación entre 1 y 5."); }
        if (string.IsNullOrWhiteSpace(opinion.Titulo) || string.IsNullOrWhiteSpace(opinion.Comentario)) { throw new ArgumentException("Completá el título y el comentario de tu opinión."); }
        opinion.IdUsuario = usuario.ID; opinion.IdAgencia = usuario.IdAgencia; opinion.Titulo = opinion.Titulo.Trim(); opinion.Comentario = opinion.Comentario.Trim();
        OpinionServicio resultado = _opinionMPP.Guardar(opinion);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "OpinionServicio", resultado.ID, "RegistrarOpinionServicio", "El usuario registró una opinión sobre el servicio.", "Exitoso", "Informacion", "General"));
        return resultado;
    }
}
