namespace tikthumb.Ffmpeg;
public class FfmpegContext
{
    public string TempImageVideoFilePath { set; get; }
    public string TempImageVideoFileName { set; get; }
    //the input text file path
    public string InputFilePath { set; get; }
    public string OutputFilePath { set; get; }
    public string OutputFileName { set; get; }

    public FfmpegContext(string tempPath,
     UploadContext context,
     string timeStamp)
    {
        TempImageVideoFileName = $"{FilePathHelper.ConstructFileNameToSave(context.ImageSaveInfo.FileNameWithoutExtension, timeStamp)}{context.VideoSaveInfo.FileExtension}";

        InputFilePath = FilePathHelper.GetPathToSave(tempPath, FilePathHelper.ConstructFileNameToSave("files_container", timeStamp));
        TempImageVideoFilePath = FilePathHelper.GetPathToSave(tempPath, TempImageVideoFileName);

        OutputFileName = $"{FilePathHelper.ConstructFileNameToSave("output", timeStamp)}{context.VideoSaveInfo.FileExtension}";
        OutputFilePath = FilePathHelper.GetPathToSave(tempPath, OutputFileName);
    }
}
