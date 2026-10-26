using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPAdjuntoTarea
{
    private readonly Conexion _conexion;
    public MPPAdjuntoTarea(Conexion conexion) { _conexion=conexion; }

    public ArchivoAdjuntoTarea Registrar(ArchivoAdjuntoTarea archivo,Agencia agencia)
    {
        DataTable tabla=_conexion.Leer("dbo.usp_AdjuntoTarea_Registrar",new List<SqlParameter>{new("@IdTarea",archivo.IdTarea),new("@IdAgencia",agencia.ID),new("@IdUsuario",archivo.IdUsuarioAlta),new("@NombreOriginal",archivo.NombreOriginal),new("@ClaveAlmacenamiento",archivo.ClaveAlmacenamiento),new("@TipoContenido",archivo.TipoContenido),new("@TamanoBytes",archivo.TamanoBytes)});
        return Crear(tabla.Rows.Cast<DataRow>().Single());
    }
    public List<ArchivoAdjuntoTarea> Consultar(Tarea tarea,Agencia agencia)=>_conexion.Leer("dbo.usp_AdjuntoTarea_Consultar",new List<SqlParameter>{new("@IdTarea",tarea.ID),new("@IdAgencia",agencia.ID)}).Rows.Cast<DataRow>().Select(Crear).ToList();
    public ArchivoAdjuntoTarea? ConsultarDetalle(ArchivoAdjuntoTarea archivo,Agencia agencia)=>_conexion.Leer("dbo.usp_AdjuntoTarea_ConsultarDetalle",new List<SqlParameter>{new("@ID",archivo.ID),new("@IdTarea",archivo.IdTarea),new("@IdAgencia",agencia.ID)}).Rows.Cast<DataRow>().Select(Crear).SingleOrDefault();
    public bool Desactivar(ArchivoAdjuntoTarea archivo,Agencia agencia)=>_conexion.Escribir("dbo.usp_AdjuntoTarea_Desactivar",new List<SqlParameter>{new("@ID",archivo.ID),new("@IdTarea",archivo.IdTarea),new("@IdAgencia",agencia.ID)});
    private static ArchivoAdjuntoTarea Crear(DataRow f)=>new(){ID=Convert.ToInt32(f["ID"]),IdTarea=Convert.ToInt32(f["IdTarea"]),IdUsuarioAlta=Convert.ToInt32(f["IdUsuarioAlta"]),NombreOriginal=Convert.ToString(f["NombreOriginal"])??string.Empty,ClaveAlmacenamiento=Convert.ToString(f["ClaveAlmacenamiento"])??string.Empty,TipoContenido=Convert.ToString(f["TipoContenido"])??"application/octet-stream",TamanoBytes=Convert.ToInt64(f["TamanoBytes"]),FechaAlta=Convert.ToDateTime(f["FechaAlta"]),Archivado=Convert.ToBoolean(f["Archivado"]),FechaArchivado=f["FechaArchivado"]==DBNull.Value?null:Convert.ToDateTime(f["FechaArchivado"]),Activo=Convert.ToBoolean(f["Activo"])};
}
