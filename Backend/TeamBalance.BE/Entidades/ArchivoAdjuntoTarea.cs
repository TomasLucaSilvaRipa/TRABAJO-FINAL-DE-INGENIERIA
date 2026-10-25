namespace TeamBalance.BE.Entidades;

public class ArchivoAdjuntoTarea
{
    public int ID { get; set; }
    public int IdTarea { get; set; }
    public int IdUsuarioAlta { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string ClaveAlmacenamiento { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = "application/octet-stream";
    public long TamanoBytes { get; set; }
    public DateTime FechaAlta { get; set; }
    public bool Archivado { get; set; }
    public DateTime? FechaArchivado { get; set; }
    public bool Activo { get; set; }
    public bool EsImagen => TipoContenido.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

public class AttachmentStorageSettings
{
    public string Directory { get; set; } = "C:\\TeamBalance\\Attachments";
    public string ArchiveDirectory { get; set; } = "C:\\TeamBalance\\AttachmentsArchive";
}
