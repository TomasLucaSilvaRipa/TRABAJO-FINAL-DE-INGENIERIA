using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public sealed class BLLCuentaCorriente
{
    private readonly MPPCuentaCorriente _cuentaCorrienteMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLCuentaCorriente(MPPCuentaCorriente cuentaCorrienteMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _cuentaCorrienteMPP = cuentaCorrienteMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<DocumentoComercial> Consultar(Usuario usuario)
    {
        ValidarDueño(usuario);
        return _cuentaCorrienteMPP.Consultar(usuario);
    }

    public DocumentoComercial CancelarSuscripcion(DocumentoComercial documento, Usuario usuario)
    {
        ValidarDueño(usuario);
        if (documento.IdSuscripcion.GetValueOrDefault() <= 0) { throw new ArgumentException("No encontramos la suscripción a cancelar."); }
        if (string.IsNullOrWhiteSpace(documento.Motivo)) { throw new ArgumentException("Indicá el motivo de la cancelación."); }

        documento.Motivo = documento.Motivo.Trim();
        DocumentoComercial notaCredito = _cuentaCorrienteMPP.CancelarSuscripcion(documento, usuario);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Suscripcion", documento.IdSuscripcion, "CancelarSuscripcion", "Se canceló el servicio y se emitió la nota de crédito interna " + notaCredito.Numero + ".", "Exitoso", "Advertencia", "Suscripción"));
        return notaCredito;
    }

    public DocumentoComercial ReactivarConNotaCredito(DocumentoComercial documento, Usuario usuario)
    {
        ValidarDueño(usuario);
        if (documento.IdSuscripcion.GetValueOrDefault() <= 0) { throw new ArgumentException("No encontramos la suscripción a regularizar."); }
        if (documento.IdPlanComercial.GetValueOrDefault() <= 0) { throw new ArgumentException("Seleccioná una modalidad para reactivar."); }

        DocumentoComercial factura = _cuentaCorrienteMPP.ReactivarConNotaCredito(documento, usuario);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Suscripcion", documento.IdSuscripcion, "ReactivarConNotaCredito", "Se reactivó la suscripción aplicando una nota de crédito interna a la factura " + factura.Numero + ".", "Exitoso", "Informacion", "Suscripción"));
        return factura;
    }

    private void ValidarDueño(Usuario usuario)
    {
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        if (!_rolBLL.TienePermiso(usuario, "GestionarSuscripcion")) { throw new UnauthorizedAccessException("No tenés permiso para consultar la cuenta corriente de la agencia."); }
    }
}
