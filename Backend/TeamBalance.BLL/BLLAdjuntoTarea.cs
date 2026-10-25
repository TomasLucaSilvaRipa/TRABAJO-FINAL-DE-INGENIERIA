using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLAdjuntoTarea
{
    private static readonly HashSet<string> ExtensionesPermitidas=new(StringComparer.OrdinalIgnoreCase){".jpg",".jpeg",".png",".webp",".gif",".pdf",".doc",".docx",".xls",".xlsx",".txt",".zip"};
    private readonly MPPAdjuntoTarea _mpp; private readonly MPPTarea _tareas; private readonly BLLBitacora _bitacora; private readonly AttachmentStorageSettings _settings;
    public BLLAdjuntoTarea(MPPAdjuntoTarea mpp,MPPTarea tareas,BLLBitacora bitacora,AttachmentStorageSettings settings){_mpp=mpp;_tareas=tareas;_bitacora=bitacora;_settings=settings;}

    public List<ArchivoAdjuntoTarea> Consultar(int idTarea,Usuario usuario){ValidarAcceso(idTarea,usuario,false);return _mpp.Consultar(idTarea,usuario.IdAgencia!.Value);}
    public ArchivoAdjuntoTarea ConsultarDetalle(int idTarea,int id,Usuario usuario){ValidarAcceso(idTarea,usuario,false);return _mpp.ConsultarDetalle(id,idTarea,usuario.IdAgencia!.Value)??throw new FileNotFoundException("No se encontró el adjunto.");}
    public string RutaFisica(ArchivoAdjuntoTarea archivo){string raiz=Path.GetFullPath(archivo.Archivado?_settings.ArchiveDirectory:_settings.Directory);string ruta=Path.GetFullPath(Path.Combine(raiz,archivo.ClaveAlmacenamiento));if(!ruta.StartsWith(raiz+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("La ruta del adjunto no es válida.");return ruta;}
    public async Task<ArchivoAdjuntoTarea> Registrar(int idTarea,string nombreOriginal,string tipoContenido,long tamano,Stream contenido,Usuario usuario)
    {
        ValidarAcceso(idTarea,usuario,true);if(tamano<=0||tamano>25L*1024*1024)throw new ArgumentException("Cada archivo debe pesar como máximo 25 MB.");
        string nombre=Path.GetFileName(nombreOriginal);string extension=Path.GetExtension(nombre);if(!ExtensionesPermitidas.Contains(extension))throw new ArgumentException("El tipo de archivo no está permitido.");
        string clave=Path.Combine(usuario.IdAgencia!.Value.ToString(),idTarea.ToString(),$"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");string ruta=Path.GetFullPath(Path.Combine(_settings.Directory,clave));Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await using(FileStream destino=new(ruta,FileMode.CreateNew,FileAccess.Write,FileShare.None)){await contenido.CopyToAsync(destino);}
        try{ArchivoAdjuntoTarea resultado=_mpp.Registrar(new(){IdTarea=idTarea,IdUsuarioAlta=usuario.ID,NombreOriginal=nombre,ClaveAlmacenamiento=clave,TipoContenido=string.IsNullOrWhiteSpace(tipoContenido)?"application/octet-stream":tipoContenido,TamanoBytes=tamano},usuario.IdAgencia.Value);_bitacora.Add(new Bitacora(usuario.ID,usuario.IdAgencia,"ArchivoAdjuntoTarea",resultado.ID,"AdjuntarArchivoTarea",$"Se adjuntó {resultado.NombreOriginal} a la tarea {idTarea}.","Exitoso","Informacion","Planificación"));return resultado;}catch{if(File.Exists(ruta))File.Delete(ruta);throw;}
    }
    public bool Eliminar(int idTarea,int id,Usuario usuario){ValidarAcceso(idTarea,usuario,true);ArchivoAdjuntoTarea archivo=ConsultarDetalle(idTarea,id,usuario);bool resultado=_mpp.Desactivar(id,idTarea,usuario.IdAgencia!.Value);string ruta=RutaFisica(archivo);if(File.Exists(ruta))File.Delete(ruta);_bitacora.Add(new Bitacora(usuario.ID,usuario.IdAgencia,"ArchivoAdjuntoTarea",id,"EliminarAdjuntoTarea",$"Se eliminó {archivo.NombreOriginal} de la tarea {idTarea}.","Exitoso","Advertencia","Planificación"));return resultado;}
    private void ValidarAcceso(int idTarea,Usuario usuario,bool escritura)
    {
        if(!usuario.IdAgencia.HasValue)throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
        bool dueno=usuario.Roles.Any(r=>string.Equals(r.TipoUsuario,"Dueno",StringComparison.OrdinalIgnoreCase));
        bool pm=usuario.Roles.Any(r=>string.Equals(r.TipoUsuario,"PM",StringComparison.OrdinalIgnoreCase));
        bool empleado=usuario.Roles.Any(r=>string.Equals(r.TipoUsuario,"Empleado",StringComparison.OrdinalIgnoreCase));
        bool permitido=dueno&&!escritura&&_tareas.Consultar(usuario).Any(t=>t.ID==idTarea)||(pm&&_tareas.ConsultarPorPM(usuario).Any(t=>t.ID==idTarea))||(empleado&&_tareas.ConsultarPorEmpleado(usuario).Any(t=>t.ID==idTarea));
        if(!permitido)throw new UnauthorizedAccessException("No tenés acceso a los adjuntos de esta tarea.");
    }
}
