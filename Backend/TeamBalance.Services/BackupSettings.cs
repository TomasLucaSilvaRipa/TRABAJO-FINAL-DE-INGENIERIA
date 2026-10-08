namespace TeamBalance.Services;

public sealed class BackupSettings
{
    public bool Habilitado { get; set; } = true;
    public string DatabaseName { get; set; } = "TeamBalance";
    public string Directory { get; set; } = @"C:\TeamBalance\Backups";
    public string RecoveryDirectory { get; set; } = @"C:\TeamBalance\Recovery";
    public int RetentionDays { get; set; } = 30;
    public int DailyHourLocal { get; set; } = 3;
    public string TimeZoneId { get; set; } = "Argentina Standard Time";
    public string XmlHistoryFile { get; set; } = @"C:\TeamBalance\Backups\continuity\historial-continuidad.xml";
    
    public List<string> ConfigurationFiles { get; set; } = ["appsettings.json", "appsettings.Production.json"];
}
