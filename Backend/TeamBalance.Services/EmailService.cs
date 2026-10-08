using System;
using System.Collections.Generic;
using System.Text;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services
{
    public class EmailService
    {
        private readonly string? emisor;
        private readonly string? claveAplicacion;
        private readonly string? urlPublicaFrontend;
        private readonly string? urlLogo;

        public EmailService(string? emisor, string? claveAplicacion, string? urlPublicaFrontend, string? urlLogo = null)
        {
            this.emisor = emisor;
            this.claveAplicacion = claveAplicacion;
            this.urlPublicaFrontend = urlPublicaFrontend;
            this.urlLogo = urlLogo;
        }

        public async Task<bool> EnviarCorreoValidacion(Usuario usuario, string token)
        {
            string receptor = usuario.Email.Trim().ToLowerInvariant();
            string nombre = string.IsNullOrWhiteSpace(usuario.Nombre) ? "Usuario" : usuario.Nombre.Trim();  
            string? enlace = CrearEnlace("validar-cuenta", "token", token);

            if (string.IsNullOrWhiteSpace(enlace))
            {
                return false;
            }

            string descripcion = $@"
                <p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p>
                <p>Tu agencia fue registrada en TeamBalance. Para habilitar el acceso inicial, confirmá tu correo electrónico.</p>
                {CrearBoton(enlace, "Confirmar mi correo")}
                <p>El enlace estará disponible durante 24 horas.</p>
                <p>Si no realizaste este registro, podés ignorar este correo.</p>";

            return await EnviarMail("Confirmá tu cuenta de TeamBalance", descripcion, receptor);
        }

        public async Task<bool> EnviarCorreoContinuarRegistro(ContratacionServicio contratacionServicio)
        {
            string? enlace = CrearEnlace("registrar-agencia", "referencia", contratacionServicio.ReferenciaContratacion);

            if (string.IsNullOrWhiteSpace(enlace))
            {
                return false;
            }

            string descripcion = $@"
                <p>Hola {System.Net.WebUtility.HtmlEncode(contratacionServicio.NombreResponsable)},</p>
                <p>Confirmamos el pago de tu contratación de TeamBalance.</p>
                <p>Cuando quieras, podés completar el registro inicial de tu agencia desde este enlace:</p>
                {CrearBoton(enlace, "Completar registro de mi agencia")}
                <p>Por seguridad, este enlace sólo funciona mientras la contratación no haya sido utilizada para crear la agencia.</p>";

            return await EnviarMail("Completá el registro de tu agencia en TeamBalance", descripcion, contratacionServicio.EmailLaboralResponsable);
        }

        public async Task<bool> EnviarCorreoRecuperoPassword(string receptor, string nombre, string token)
        {
            string? enlace = CrearEnlace("restablecer-contrasena", "token", token);

            if (string.IsNullOrWhiteSpace(enlace))
            {
                return false;
            }

            string descripcion = $@"
                <p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p>
                <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta de TeamBalance.</p>
                {CrearBoton(enlace, "Restablecer mi contraseña")}
                <p>El enlace estará disponible durante 30 minutos y sólo podrá utilizarse una vez.</p>
                <p>Si no solicitaste este cambio, podés ignorar este correo.</p>";

            return await EnviarMail("Restablecé tu contraseña de TeamBalance", descripcion, receptor);
        }

        public async Task<bool> EnviarCorreoPasswordModificada(string receptor, string nombre)
        {
            string descripcion = $@"
                <p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p>
                <p>La contraseña de tu cuenta de TeamBalance fue modificada correctamente.</p>
                <p>Por seguridad, cerramos las sesiones activas. Volvé a iniciar sesión con tu nueva contraseña.</p>
                <p>Si no realizaste este cambio, contactanos de inmediato.</p>";

            return await EnviarMail("Tu contraseña de TeamBalance fue modificada", descripcion, receptor);
        }

        public async Task<bool> EnviarCorreoContacto(string nombre, string? organizacion, string email, string mensaje)
        {
            string descripcion = $@"<p><strong>Nombre:</strong> {System.Net.WebUtility.HtmlEncode(nombre)}</p><p><strong>Organización:</strong> {System.Net.WebUtility.HtmlEncode(organizacion ?? "-")}</p><p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}</p><p><strong>Consulta:</strong></p><p>{System.Net.WebUtility.HtmlEncode(mensaje).Replace(Environment.NewLine, "<br />")}</p>";
            return await EnviarMail("Nueva consulta desde el sitio público de TeamBalance", descripcion, emisor ?? string.Empty);
        }

        public async Task<bool> EnviarRespuestaSoporte(string receptor, string nombre, string asunto, string respuesta)
        {
            string descripcion = $@"<p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p><p>El equipo de Soporte respondió tu consulta <strong>{System.Net.WebUtility.HtmlEncode(asunto)}</strong>:</p><p>{System.Net.WebUtility.HtmlEncode(respuesta).Replace(Environment.NewLine, "<br />")}</p><p>Ingresá a TeamBalance para continuar la conversación o cerrar el ticket.</p>";
            return await EnviarMail("Respondimos tu consulta de TeamBalance", descripcion, receptor);
        }

        public async Task<bool> EnviarNewsletter(string receptor, string nombre, string titulo, string contenido, string categoria)
        {
            string descripcion = $@"<p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p><p><strong>{System.Net.WebUtility.HtmlEncode(categoria)}</strong></p><h2>{System.Net.WebUtility.HtmlEncode(titulo)}</h2><p>{System.Net.WebUtility.HtmlEncode(contenido).Replace(Environment.NewLine, "<br />")}</p><p>Podés actualizar tus preferencias de novedades desde tu perfil de TeamBalance.</p>";
            return await EnviarMail($"TeamBalance · {titulo}", descripcion, receptor);
        }

        private async Task<bool> EnviarMail(string tema, string descripcion, string receptor)
        {
            if (string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(claveAplicacion) || string.IsNullOrWhiteSpace(receptor))
            {
                Console.Error.WriteLine("[EmailService] No se pudo enviar el correo: falta configurar emisor, contraseña de aplicación o receptor.");
                return false;
            }

            try
            {
                using System.Net.Mail.SmtpClient smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential(emisor, claveAplicacion),
                    EnableSsl = true,
                };

                using System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(emisor, "TeamBalance"),
                    Subject = tema,
                    Body = CrearPlantilla(descripcion),
                    IsBodyHtml = true,
                };

                mail.To.Add(receptor);
                await smtpClient.SendMailAsync(mail);

                return true;
            }
            catch (Exception ex){ Console.Error.WriteLine($"[EmailService] Error al enviar correo a {receptor}: {ex.Message}"); return false; }
        }

        private string? CrearEnlace(string pagina, string parametro, string valor)
        {
            if (string.IsNullOrWhiteSpace(urlPublicaFrontend) ||
                !Uri.TryCreate(urlPublicaFrontend, UriKind.Absolute, out Uri? urlBase) ||
                (urlBase.Scheme != Uri.UriSchemeHttp && urlBase.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return $"{urlPublicaFrontend.TrimEnd('/')}/{pagina}?{parametro}={Uri.EscapeDataString(valor)}";
        }

        private string CrearPlantilla(string contenido)
        {
            string? logo = CrearUrlLogo();
            string encabezado = string.IsNullOrWhiteSpace(logo)
                ? "<div style='font-size:25px;font-weight:800;letter-spacing:-.6px;color:#ffffff'>Team<span style='color:#78d646'>Balance</span></div><div style='margin-top:5px;font-size:10px;letter-spacing:2px;text-transform:uppercase;color:#a6e8f4'>Gestión inteligente de equipos</div>"
                : $"<span style='display:inline-block;padding:7px 12px;background:#ffffff;border-radius:10px'><img src='{System.Net.WebUtility.HtmlEncode(logo)}' width='160' alt='TeamBalance · Gestión inteligente de equipos' style='display:block;width:160px;max-width:100%;height:auto;border:0;outline:none;text-decoration:none' /></span>";

            return $@"<!doctype html>
<html lang='es'>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#061e2e;font-family:Arial,Helvetica,sans-serif;color:#163243'>
  <div style='display:none;max-height:0;overflow:hidden;opacity:0;color:transparent'>Actualización de TeamBalance</div>
  <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='width:100%;background:#061e2e'>
    <tr><td align='center' style='padding:38px 16px 30px'>
      <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='width:100%;max-width:640px;background:#0a354e;border:1px solid #165570;border-radius:20px;overflow:hidden'>
        <tr><td style='padding:28px 32px 23px;background:#082f49'>
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'><tr>
            <td valign='top'>{encabezado}</td>
            <td width='116' valign='top' align='right'>
              <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='margin:2px 0 0 auto'>
                <tr><td width='34' height='5' style='height:5px;background:#20c9e5;font-size:0;line-height:0'>&nbsp;</td><td width='6'>&nbsp;</td><td width='60' height='5' style='height:5px;background:#91dc49;font-size:0;line-height:0'>&nbsp;</td></tr>
                <tr><td colspan='3' height='8' style='height:8px;font-size:0;line-height:0'>&nbsp;</td></tr>
                <tr><td width='62' height='5' colspan='2' style='height:5px;background:#0e6f9b;font-size:0;line-height:0'>&nbsp;</td><td width='38' height='5' style='height:5px;background:#20c9e5;font-size:0;line-height:0'>&nbsp;</td></tr>
                <tr><td colspan='3' height='8' style='height:8px;font-size:0;line-height:0'>&nbsp;</td></tr>
                <tr><td width='22' height='5' style='height:5px;background:#91dc49;font-size:0;line-height:0'>&nbsp;</td><td width='6'>&nbsp;</td><td width='72' height='5' style='height:5px;background:#13aebf;font-size:0;line-height:0'>&nbsp;</td></tr>
              </table>
            </td>
          </tr></table>
        </td></tr>
        <tr><td style='padding:0 32px;background:#082f49'><table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'><tr><td width='20%' height='4' style='height:4px;background:#91dc49;font-size:0;line-height:0'>&nbsp;</td><td width='49%' height='4' style='height:4px;background:#20c9e5;font-size:0;line-height:0'>&nbsp;</td><td width='31%' height='4' style='height:4px;background:#0e6590;font-size:0;line-height:0'>&nbsp;</td></tr></table></td></tr>
        <tr><td style='padding:24px 24px 22px;background:#dcecf1'>
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background:#f3f9fa;border:1px solid #b9dbe3'>
            <tr><td width='7' style='width:7px;background:#91dc49'>&nbsp;</td><td style='padding:29px 27px 26px;font-size:16px;line-height:1.65;color:#183545'>
              <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'><tr><td height='3' style='height:3px;background:#20c9e5;font-size:0;line-height:0'>&nbsp;</td><td width='82' height='3' style='height:3px;background:#91dc49;font-size:0;line-height:0'>&nbsp;</td></tr></table>
              <div style='padding-top:20px'>{contenido}</div>
            </td></tr>
          </table>
        </td></tr>
        <tr><td style='padding:22px 32px 24px;background:#092f46'>
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'><tr><td width='5' style='width:5px;background:#20c9e5'>&nbsp;</td><td style='padding-left:14px'><p style='margin:0 0 7px;font-size:12px;line-height:1.55;color:#d4e8ed'>Este correo fue enviado por <strong style='color:#ffffff'>TeamBalance</strong>.</p><p style='margin:0;font-size:12px;line-height:1.55;color:#a9c9d2'>Por seguridad, nunca compartas contraseñas, códigos de verificación ni datos de tarjetas.</p></td></tr></table>
        </td></tr>
      </table>
      <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='width:100%;max-width:640px'><tr><td align='center' style='padding-top:16px'><span style='display:inline-block;width:30px;height:3px;background:#20c9e5;font-size:0;line-height:0'>&nbsp;</span><span style='display:inline-block;width:9px'>&nbsp;</span><span style='display:inline-block;width:52px;height:3px;background:#91dc49;font-size:0;line-height:0'>&nbsp;</span><p style='margin:11px 0 0;font-size:11px;line-height:1.5;color:#95b5c0'>TeamBalance · Gestión inteligente de equipos</p></td></tr></table>
    </td></tr>
  </table>
</body></html>";
        }

        private string? CrearUrlLogo()
        {
            if (!string.IsNullOrWhiteSpace(urlLogo) && Uri.TryCreate(urlLogo, UriKind.Absolute, out Uri? logoConfigurado) && (logoConfigurado.Scheme == Uri.UriSchemeHttp || logoConfigurado.Scheme == Uri.UriSchemeHttps))
            {
                return logoConfigurado.ToString();
            }

            if (Uri.TryCreate(urlPublicaFrontend, UriKind.Absolute, out Uri? frontend) && (frontend.Scheme == Uri.UriSchemeHttp || frontend.Scheme == Uri.UriSchemeHttps))
            {
                return new Uri(frontend, "/brand/teambalance-logo-header.png").ToString();
            }

            return null;
        }

        private static string CrearBoton(string enlace, string texto)
        {
            return $"<p style='margin:24px 0'><a href='{System.Net.WebUtility.HtmlEncode(enlace)}' style='display:inline-block;padding:12px 20px;background:#008db8;border-radius:8px;color:#ffffff;font-weight:700;text-decoration:none'>{System.Net.WebUtility.HtmlEncode(texto)}</a></p>";
        }
    }
}
