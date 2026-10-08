using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPNovedades
{
    private readonly Conexion _conexion;
    public MPPNovedades(Conexion conexion) => _conexion = conexion;
    public List<CategoriaNoticia> Categorias() => _conexion.Leer("dbo.usp_Novedades_ConsultarCategorias").Rows.Cast<DataRow>().Select(f => new CategoriaNoticia { ID = Convert.ToInt32(f["ID"]), Nombre = Convert.ToString(f["Nombre"]) ?? string.Empty, Descripcion = Convert.ToString(f["Descripcion"]) ?? string.Empty, Activo = Convert.ToBoolean(f["Activo"]) }).ToList();
    public List<Noticia> Publicas() => ConsultarNoticias("dbo.usp_Novedades_ConsultarPublicas");
    public List<Noticia> Gestion() => ConsultarNoticias("dbo.usp_Novedades_ConsultarGestion");
    public Noticia Guardar(GuardarNoticiaRequest solicitud) => CrearNoticia(_conexion.Leer("dbo.usp_Novedades_Guardar", new List<SqlParameter> { new("@IdCategoria", solicitud.IdCategoriaNoticia), new("@Titulo", solicitud.Titulo), new("@Contenido", solicitud.Contenido), new("@ImagenUrl", (object?)solicitud.ImagenUrl ?? DBNull.Value), new("@FechaPublicacion", solicitud.FechaPublicacion ?? DateTime.Now), new("@FechaVencimiento", (object?)solicitud.FechaVencimiento ?? DBNull.Value) }).Rows[0]);
    public void Bajar(int id) => _conexion.Leer("dbo.usp_Novedades_Bajar", new List<SqlParameter> { new("@IdNoticia", id) });
    public List<int> Preferencias(int idUsuario) => _conexion.Leer("dbo.usp_Novedades_ConsultarPreferencias", new List<SqlParameter> { new("@IdUsuario", idUsuario) }).Rows.Cast<DataRow>().Select(f => Convert.ToInt32(f["IdCategoriaNoticia"])).ToList();
    public void GuardarPreferencias(int idUsuario, List<int> categorias) => _conexion.Leer("dbo.usp_Novedades_GuardarPreferencias", new List<SqlParameter> { new("@IdUsuario", idUsuario), new("@CategoriasJson", JsonSerializer.Serialize(categorias.Distinct().ToList())) });
    public List<Noticia> PendientesDifusion() => ConsultarNoticias("dbo.usp_Novedades_ConsultarPendientesDifusion");
    public List<(string Email, string Nombre)> Destinatarios(int idNoticia) => _conexion.Leer("dbo.usp_Novedades_ConsultarDestinatarios", new List<SqlParameter> { new("@IdNoticia", idNoticia) }).Rows.Cast<DataRow>().Select(f => (Convert.ToString(f["Email"]) ?? string.Empty, Convert.ToString(f["Nombre"]) ?? "Usuario")).ToList();
    public void MarcarDifundida(int idNoticia) => _conexion.Leer("dbo.usp_Novedades_MarcarDifundida", new List<SqlParameter> { new("@IdNoticia", idNoticia) });
    private List<Noticia> ConsultarNoticias(string sp) => _conexion.Leer(sp).Rows.Cast<DataRow>().Select(CrearNoticia).ToList();
    private static Noticia CrearNoticia(DataRow f) => new() { ID = Convert.ToInt32(f["ID"]), IdCategoriaNoticia = Convert.ToInt32(f["IdCategoriaNoticia"]), Categoria = Convert.ToString(f["Categoria"]) ?? string.Empty, Titulo = Convert.ToString(f["Titulo"]) ?? string.Empty, Contenido = Convert.ToString(f["Contenido"]) ?? string.Empty, ImagenUrl = f["ImagenUrl"] == DBNull.Value ? null : Convert.ToString(f["ImagenUrl"]), FechaPublicacion = Convert.ToDateTime(f["FechaPublicacion"]), FechaVencimiento = f["FechaVencimiento"] == DBNull.Value ? null : Convert.ToDateTime(f["FechaVencimiento"]), Activo = Convert.ToBoolean(f["Activo"]), DifusionEnviada = Convert.ToBoolean(f["DifusionEnviada"]) };
}
