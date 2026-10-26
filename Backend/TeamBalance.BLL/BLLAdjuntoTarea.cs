using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLAdjuntoTarea
{
    private static readonly HashSet<string> ExtensionesPermitidas=new(StringComparer.OrdinalIgnoreCase){".jpg",".jpeg",".png",".webp",".gif",".pdf",".doc",".docx",".xls",".xlsx",".txt",".zip"};
    private readonly MPPAdjuntoTarea _mpp; private readonly MPPTarea _tareas; private readonly BLLBitacora _bitacora; private readonly AttachmentStorageSettings _settings;
    public BLLAdjuntoTarea(MPPAdjuntoTarea mpp,MPPTarea tareas,BLLBitacora bitacora,AttachmentStorageSettings settings){_mpp=mpp;_tareas=tareas;_bitacora=bitacora;_settings=settings;}

    public List<ArchivoAdjuntoTarea> Consultar(Tarea tarea,Usuario usuario){ValidarAcceso(new AccesoAdjuntoTarea{Tarea=tarea,Usuario=usuario});return _mpp.Consultar(tarea,ObtenerAgencia(usuario));}
    public ArchivoAdjuntoTarea ConsultarDetalle(ArchivoAdjuntoTarea archivo,Usuario usuario){ValidarAcceso(new AccesoAdjuntoTarea{Tarea=new Tarea{ID=archivo.IdTarea},Usuario=usuario});return _mpp.ConsultarDetalle(archivo,ObtenerAgencia(usuario))??throw new FileNotFoundException("No se encontró el adjunto.");}
    public string RutaFisica(ArchivoAdjuntoTarea archivo){string raiz=Path.GetFullPath(archivo.Archivado?_settings.ArchiveDirectory:_settings.Directory);string ruta=Path.GetFullPath(Path.Combine(raiz,archivo.ClaveAlmacenamiento));if(!ruta.StartsWith(raiz+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("La ruta del adjunto no es válida.");return ruta;}
    public async Task<ArchivoAdjuntoTarea> Registrar(CargaAdjuntoTarea carga,Usuario usuario)
    {
        ValidarAcceso(new AccesoAdjuntoTarea{Tarea=carga.Tarea,Usuario=usuario,RequiereEscritura=true});ArchivoAdjuntoTarea archivo=carga.Archivo;if(archivo.TamanoBytes<=0||archivo.TamanoBytes>25L*1024*1024)throw new ArgumentException("Cada archivo debe pesar como máximo 25 MB.");
        string nombre=Path.GetFileName(archivo.NombreOriginal);string extension=Path.GetExtension(nombre);if(!ExtensionesPermitidas.Contains(extension))throw new ArgumentException("El tipo de archivo no está permitido.");
        string clave=Path.Combine(usuario.IdAgencia!.Value.ToString(),carga.Tarea.ID.ToString(),$"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");string ruta=Path.GetFullPath(Path.Combine(_settings.Directory,clave));Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await using(FileStream destino=new(ruta,FileMode.CreateNew,FileAccess.Write,FileShare.None)){await carga.Contenido.CopyToAsync(destino);}
        archivo.IdTarea=carga.Tarea.ID;archivo.IdUsuarioAlta=usuario.ID;archivo.NombreOriginal=nombre;archivo.ClaveAlmacenamiento=clave;archivo.TipoContenido=string.IsNullOrWhiteSpace(archivo.TipoContenido)?"application/octet-stream":archivo.TipoContenido;
        try{ArchivoAdjuntoTarea resultado=_mpp.Registrar(archivo,ObtenerAgencia(usuario));_bitacora.Add(new Bitacora(usuario.ID,usuario.IdAgencia,"ArchivoAdjuntoTarea",resultado.ID,"AdjuntarArchivoTarea",$"Se adjuntó {resultado.NombreOriginal} a la tarea {carga.Tarea.ID}.","Exitoso","Informacion","Planificación"));return resultado;}catch{if(File.Exists(ruta))File.Delete(ruta);throw;}
    }
    public bool Eliminar(ArchivoAdjuntoTarea archivo,Usuario usuario){ValidarAcceso(new AccesoAdjuntoTarea{Tarea=new Tarea{ID=archivo.IdTarea},Usuario=usuario,RequiereEscritura=true});ArchivoAdjuntoTarea persistido=ConsultarDetalle(archivo,usuario);bool resultado=_mpp.Desactivar(persistido,ObtenerAgencia(usuario));string ruta=RutaFisica(persistido);if(File.Exists(ruta))File.Delete(ruta);_bitacora.Add(new Bitacora(usuario.ID,usuario.IdAgencia,"ArchivoAdjuntoTarea",persistido.ID,"EliminarAdjuntoTarea",$"Se eliminó {persistido.NombreOriginal} de la tarea {persistido.IdTarea}.","Exitoso","Advertencia","Planificación"));return resultado;}
    private void ValidarAcceso(AccesoAdjuntoTarea acceso)
    {
        Usuario usuario=acceso.Usuario;int idTarea=acceso.Tarea.ID;
        if(!usuario.IdAgencia.HasValue)throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
        bool dueno=usuario.Roles.Any(r=>string.Equals(r.TipoUsuario,"Dueno",StringComparison.OrdinalIgnoreCase));
        bool pm=usuario.Roles.Any(r=>string.Equals(r.TipoUsuario,"PM",StringComparison.OrdinalIgnoreCase));
        bool empleado=usuario.Roles.Any(r=>string.Equals(r.TipoUsuario,"Empleado",StringComparison.OrdinalIgnoreCase));
        bool permitido=dueno&&!acceso.RequiereEscritura&&_tareas.Consultar(usuario).Any(t=>t.ID==idTarea)||(pm&&_tareas.ConsultarPorPM(usuario).Any(t=>t.ID==idTarea))||(empleado&&_tareas.ConsultarPorEmpleado(usuario).Any(t=>t.ID==idTarea));
        if(!permitido)throw new UnauthorizedAccessException("No tenés acceso a los adjuntos de esta tarea.");
    }
    private static Agencia ObtenerAgencia(Usuario usuario)=>new(){ID=usuario.IdAgencia??throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")};
}
