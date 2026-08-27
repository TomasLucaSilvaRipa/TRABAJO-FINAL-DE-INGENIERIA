using System;
using System.Collections.Generic;
using System.Text;

namespace TeamBalance.Services
{
    public class EmailService
    {
        private readonly string? emisor;
        private readonly string? claveAplicacion;
        private readonly string? urlPublicaFrontend;

        public EmailService(string? emisor, string? claveAplicacion, string? urlPublicaFrontend)
        {
            this.emisor = emisor;
            this.claveAplicacion = claveAplicacion;
            this.urlPublicaFrontend = urlPublicaFrontend;
        }

        public async Task<bool> EnviarCorreoValidacion(string receptor, string nombre, string token)
        {
            string? enlace = CrearEnlace("validar-cuenta", "token", token);

            if (string.IsNullOrWhiteSpace(enlace))
            {
                return false;
            }

            string descripcion = $@"
                <p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p>
                <p>Tu agencia fue registrada en TeamBalance. Para habilitar el acceso inicial, confirmá tu correo electrónico.</p>
                <p><a href=""{enlace}"">Confirmar mi correo</a></p>
                <p>El enlace estará disponible durante 24 horas.</p>
                <p>Si no realizaste este registro, podés ignorar este correo.</p>";

            return await EnviarMail("Confirmá tu cuenta de TeamBalance", descripcion, receptor);
        }

        public async Task<bool> EnviarCorreoContinuarRegistro(string receptor, string nombre, string referenciaContratacion)
        {
            string? enlace = CrearEnlace("registrar-agencia", "referencia", referenciaContratacion);

            if (string.IsNullOrWhiteSpace(enlace))
            {
                return false;
            }

            string descripcion = $@"
                <p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p>
                <p>Confirmamos el pago de tu contratación de TeamBalance.</p>
                <p>Cuando quieras, podés completar el registro inicial de tu agencia desde este enlace:</p>
                <p><a href=""{enlace}"">Completar registro de mi agencia</a></p>
                <p>Por seguridad, este enlace sólo funciona mientras la contratación no haya sido utilizada para crear la agencia.</p>";

            return await EnviarMail("Completá el registro de tu agencia en TeamBalance", descripcion, receptor);
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
                    Body = descripcion,
                    IsBodyHtml = true,
                };

                mail.To.Add(receptor);
                await smtpClient.SendMailAsync(mail);

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EmailService] Error al enviar correo a {receptor}: {ex.Message}");
                return false;
            }
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
    }
}
