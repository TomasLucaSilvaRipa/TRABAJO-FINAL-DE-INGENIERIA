using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLBitacora
{
    private readonly MPPBitacora _bitacoraMPP;
    private readonly MPPAnaliticaOperativa _analiticaMPP;
    private readonly EmailService _emailService;

    public BLLBitacora(MPPBitacora bitacoraMPP, MPPAnaliticaOperativa analiticaMPP, EmailService emailService)
    {
        _bitacoraMPP = bitacoraMPP;
        _analiticaMPP = analiticaMPP;
        _emailService = emailService;
    }

    public bool Add(Bitacora bitacora)
    {
        if (string.IsNullOrWhiteSpace(bitacora.Accion) || string.IsNullOrWhiteSpace(bitacora.Mensaje))
        {
            return false;
        }

        bitacora.Accion = bitacora.Accion.Trim();
        bitacora.Mensaje = bitacora.Mensaje.Trim();
        bitacora.Entidad = string.IsNullOrWhiteSpace(bitacora.Entidad) ? null : bitacora.Entidad.Trim();
        ResultadoBitacora? resultado = Bitacora.ObtenerResultado(bitacora.Resultado);
        CriticidadBitacora? criticidad = Bitacora.ObtenerCriticidad(bitacora.Criticidad);
        if (!string.IsNullOrWhiteSpace(bitacora.Resultado) && !resultado.HasValue) { throw new ArgumentException("El resultado de la bitácora no es válido."); }
        if (!string.IsNullOrWhiteSpace(bitacora.Criticidad) && !criticidad.HasValue) { throw new ArgumentException("La criticidad de la bitácora no es válida."); }
        bitacora.ResultadoEnum = resultado;
        bitacora.CriticidadEnum = criticidad ?? CriticidadBitacora.Informacion;
        ModuloBitacora? modulo = Bitacora.ObtenerModulo(bitacora.Modulo);
        if (!string.IsNullOrWhiteSpace(bitacora.Modulo) && !modulo.HasValue) { throw new ArgumentException("El módulo de la bitácora no es válido."); }
        bitacora.ModuloEnum = modulo ?? ModuloBitacora.General;
        bitacora.DireccionIP = string.IsNullOrWhiteSpace(bitacora.DireccionIP) ? null : bitacora.DireccionIP.Trim();
        bitacora.FechaHora = bitacora.FechaHora == default ? DateTime.Now : bitacora.FechaHora;

        return _bitacoraMPP.Add(bitacora);
    }

    public List<Bitacora> LeerBitacora(FiltroBitacora filtro)
    {
        return _bitacoraMPP.LeerBitacora(filtro);
    }

    public List<Bitacora> FiltrarBitacora(FiltroBitacora filtro)
    {
        try
        {
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");
            }
            return _bitacoraMPP.Filtrar(filtro);
        }
        catch (Exception ex)
        {
            throw new Exception("Error al filtrar la bitácora: " + ex.Message, ex);
        }
    }

    public async Task<int> GenerarYEnviarInforme(FiltroBitacora filtro, Usuario solicitante)
    {
        ValidarConsultaBitacora(solicitante);
        if (!filtro.IdAgencia.HasValue || filtro.IdAgencia.Value <= 0)
        {
            throw new ArgumentException("Seleccioná una agencia para generar y enviar el informe.");
        }

        List<Bitacora> eventos = FiltrarBitacora(filtro);
        if (eventos.Count == 0)
        {
            throw new InvalidOperationException("No existen eventos para generar el informe con los filtros seleccionados.");
        }

        Usuario agenciaConsultada = new Usuario { IdAgencia = filtro.IdAgencia };
        List<DestinatarioDesvioHoras> destinatarios = _analiticaMPP.ConsultarDuenosAgencia(agenciaConsultada)
            .Where(destinatario => !string.IsNullOrWhiteSpace(destinatario.Email))
            .ToList();
        if (destinatarios.Count == 0)
        {
            throw new InvalidOperationException("La agencia seleccionada no posee un Dueño activo con correo registrado.");
        }

        int enviados = 0;
        foreach (DestinatarioDesvioHoras destinatario in destinatarios)
        {
            if (await _emailService.EnviarInformeBitacora(destinatario, filtro, eventos))
            {
                enviados++;
            }
        }

        string resultado = enviados == destinatarios.Count ? "Exitoso" : enviados > 0 ? "Parcial" : "Error";
        string criticidad = enviados == destinatarios.Count ? "Informacion" : "Advertencia";
        Add(new Bitacora(
            solicitante.ID,
            filtro.IdAgencia,
            "Bitacora",
            null,
            "GenerarInformeBitacora",
            $"Se generó un informe con {eventos.Count} evento(s) y se envió a {enviados} de {destinatarios.Count} Dueño(s).",
            resultado,
            criticidad,
            "Seguridad"));

        if (enviados == 0)
        {
            throw new InvalidOperationException("El informe fue generado, pero no pudo enviarse a los Dueños de la agencia.");
        }

        return enviados;
    }

    private static void ValidarConsultaBitacora(Usuario solicitante)
    {
        bool esSoporte = solicitante.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase));
        bool tienePermiso = solicitante.Permisos.Any(permiso => string.Equals(permiso.Codigo, "ConsultarBitacora", StringComparison.OrdinalIgnoreCase));
        if (!esSoporte || !tienePermiso)
        {
            throw new UnauthorizedAccessException("No tenés permiso para generar informes de la bitácora.");
        }
    }
}
