using System.Text;

namespace tikthumb.Ffmpeg;
public static class FilePathHelper
{
    public static FileSaveInfo CreateUploadedMediaMetadata(string savePath, string filenameWithExtension, string timeStamp)
    {
        var newFileName = ConstructFileNameToSave(filenameWithExtension, timeStamp);
        return new FileSaveInfo()
        {
            FileNameWithExtension = filenameWithExtension,
            FileNameWithoutExtension = Path.GetFileNameWithoutExtension(filenameWithExtension),
            FileExtension = Path.GetExtension(filenameWithExtension),
            SavedFileNameWithExtension = Path.Combine(savePath, newFileName),
            SavedFileName = newFileName
        };
    }

    public static string GetPathToSave(string basePath, string fileName)
    {
        return Path.Combine(basePath, fileName);
    }

    public static string ConstructFileNameToSave(string fileName, string timeStamp)
    {
        return $"{timeStamp}_{fileName}";
    }

    public static string NormalizeFileName(string fileName)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in fileName)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}