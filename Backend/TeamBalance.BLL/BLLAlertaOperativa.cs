using TeamBalance.BE.Entidades;using TeamBalance.MPP;using TeamBalance.Services;
namespace TeamBalance.BLL;
public class BLLAlertaOperativa
{
 private readonly MPPAlertaOperativa _mpp;private readonly EmailService _email;private readonly SlackPushService _slack;private readonly BLLBitacora _bitacora;
 public BLLAlertaOperativa(MPPAlertaOperativa mpp,EmailService email,SlackPushService slack,BLLBitacora bitacora){_mpp=mpp;_email=email;_slack=slack;_bitacora=bitacora;}
 public ConfiguracionAlertaAgencia Consultar(Usuario u){ValidarDueno(u);return _mpp.ConsultarConfiguracion(new Agencia { ID = u.IdAgencia!.Value });}
 public ConfiguracionAlertaAgencia Guardar(ConfiguracionAlertaAgencia c,Usuario u){ValidarDueno(u);c.IdAgencia=u.IdAgencia!.Value;ConfiguracionAlertaAgencia r=_mpp.GuardarConfiguracion(c);_bitacora.Add(new Bitacora(u.ID,u.IdAgencia,"ConfiguracionAlertaAgencia",u.IdAgencia,"ModificarAlertasOperativas","Se actualizó la configuración de alertas operativas.","Exitoso","Informacion","Configuración"));return r;}
 public async Task<int> Procesar(){List<AlertaOperativaEnvio> alertas=_mpp.Generar();foreach(AlertaOperativaEnvio a in alertas){string mensaje=a.Tipo=="Desvio70_40"?$"{a.Proyecto} consumió {a.Consumo:N1}% de las horas con sólo {a.Avance:N1}% de avance.":$"{a.Proyecto} alcanzó {a.Consumo:N1}% de consumo de horas.";if(!string.IsNullOrWhiteSpace(a.Email))await _email.EnviarAlertaOperativa(a);await _slack.Enviar(new MensajeSlack { IdUsuarioSlack = a.SlackUserId, Contenido = mensaje });}return alertas.Count;}
 private static void ValidarDueno(Usuario u){if(!u.IdAgencia.HasValue||!u.Roles.Any(r=>string.Equals(r.TipoUsuario,"Dueno",StringComparison.OrdinalIgnoreCase)))throw new UnauthorizedAccessException("Solamente el dueño puede configurar las alertas de la agencia.");}
}
