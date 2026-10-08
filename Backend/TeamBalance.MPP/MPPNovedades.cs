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
    public void Bajar(Noticia noticia) => _conexion.Leer("dbo.usp_Novedades_Bajar", new List<SqlParameter>() { new SqlParameter("@IdNoticia", noticia.ID) });
    public List<int> Preferencias(Usuario usuario) => _conexion.Leer("dbo.usp_Novedades_ConsultarPreferencias", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) }).Rows.Cast<DataRow>().Select(f => Convert.ToInt32(f["IdCategoriaNoticia"])).ToList();
    public void GuardarPreferencias(Usuario usuario, PreferenciasNewsletterRequest solicitud) => _conexion.Leer("dbo.usp_Novedades_GuardarPreferencias", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@CategoriasJson", JsonSerializer.Serialize(solicitud.CategoriasIds.Distinct().ToList())) });
    public List<Noticia> PendientesDifusion() => ConsultarNoticias("dbo.usp_Novedades_ConsultarPendientesDifusion");
    public List<(string Email, string Nombre)> Destinatarios(Noticia noticia) => _conexion.Leer("dbo.usp_Novedades_ConsultarDestinatarios", new List<SqlParameter>() { new SqlParameter("@IdNoticia", noticia.ID) }).Rows.Cast<DataRow>().Select(f => (Convert.ToString(f["Email"]) ?? string.Empty, Convert.ToString(f["Nombre"]) ?? "Usuario")).ToList();
    public void MarcarDifundida(Noticia noticia) => _conexion.Leer("dbo.usp_Novedades_MarcarDifundida", new List<SqlParameter>() { new SqlParameter("@IdNoticia", noticia.ID) });
    private List<Noticia> ConsultarNoticias(string sp) => _conexion.Leer(sp).Rows.Cast<DataRow>().Select(CrearNoticia).ToList();
    private static Noticia CrearNoticia(DataRow fila)
    {
        Noticia noticia = new Noticia();
        noticia.ID = Convert.ToInt32(fila["ID"]);
        noticia.IdCategoriaNoticia = Convert.ToInt32(fila["IdCategoriaNoticia"]);
        noticia.Categoria = Convert.ToString(fila["Categoria"]) ?? string.Empty;
        noticia.Titulo = Convert.ToString(fila["Titulo"]) ?? string.Empty;
        noticia.Contenido = Convert.ToString(fila["Contenido"]) ?? string.Empty;
        noticia.ImagenUrl = fila["ImagenUrl"] == DBNull.Value ? null : Convert.ToString(fila["ImagenUrl"]);
        noticia.FechaPublicacion = Convert.ToDateTime(fila["FechaPublicacion"]);
        noticia.FechaVencimiento = fila["FechaVencimiento"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaVencimiento"]);
        noticia.Activo = Convert.ToBoolean(fila["Activo"]);
        noticia.DifusionEnviada = Convert.ToBoolean(fila["DifusionEnviada"]);
        return noticia;
    }
}
