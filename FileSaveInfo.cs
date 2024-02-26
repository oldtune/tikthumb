namespace tikthumb;
public record FileSaveInfo
{
    public string FileNameWithoutExtension { set; get; }
    public string FileNameWithExtension { set; get; }
    public string SavedFileNameWithExtension { set; get; }
    public string SavedFileName { set; get; }
    public string FileExtension { set; get; }
}