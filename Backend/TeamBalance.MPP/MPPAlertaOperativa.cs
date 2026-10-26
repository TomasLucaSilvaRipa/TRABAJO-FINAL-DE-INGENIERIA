using System.Data;using Microsoft.Data.SqlClient;using TeamBalance.BE.Entidades;using TeamBalance.DAL;
namespace TeamBalance.MPP;
public class MPPAlertaOperativa
{
 private readonly Conexion _conexion;public MPPAlertaOperativa(Conexion conexion){_conexion=conexion;}
 public ConfiguracionAlertaAgencia ConsultarConfiguracion(Agencia agencia)=>Config(_conexion.Leer("dbo.usp_AlertaOperativa_ConsultarConfiguracion",new List<SqlParameter>{new("@IdAgencia",agencia.ID)}).Rows[0]);
 public ConfiguracionAlertaAgencia GuardarConfiguracion(ConfiguracionAlertaAgencia c)=>Config(_conexion.Leer("dbo.usp_AlertaOperativa_GuardarConfiguracion",new List<SqlParameter>{new("@IdAgencia",c.IdAgencia),new("@UmbralConsumoHoras",c.UmbralConsumoHoras),new("@NotificacionInternaActiva",c.NotificacionInternaActiva),new("@EmailActivo",c.EmailActivo),new("@SlackActivo",c.SlackActivo)}).Rows[0]);
 public List<AlertaOperativaEnvio> Generar()=>_conexion.Leer("dbo.usp_AlertaOperativa_Generar").Rows.Cast<DataRow>().Select(f=>new AlertaOperativaEnvio{ID=Convert.ToInt32(f["ID"]),IdProyecto=Convert.ToInt32(f["IdProyecto"]),Tipo=Convert.ToString(f["Tipo"])??"",Proyecto=Convert.ToString(f["Proyecto"])??"",Consumo=Convert.ToDecimal(f["Consumo"]),Avance=Convert.ToDecimal(f["Avance"]),NombreDestinatario=Convert.ToString(f["NombreDestinatario"])??"",Email=Convert.ToBoolean(f["EmailActivo"])?Convert.ToString(f["Email"])??"":"",SlackUserId=Convert.ToBoolean(f["SlackActivo"])?Convert.ToString(f["SlackUserId"]):null}).ToList();
 private static ConfiguracionAlertaAgencia Config(DataRow f)=>new(){IdAgencia=Convert.ToInt32(f["IdAgencia"]),UmbralConsumoHoras=Convert.ToDecimal(f["UmbralConsumoHoras"]),NotificacionInternaActiva=Convert.ToBoolean(f["NotificacionInternaActiva"]),EmailActivo=Convert.ToBoolean(f["EmailActivo"]),SlackActivo=Convert.ToBoolean(f["SlackActivo"])};
}
