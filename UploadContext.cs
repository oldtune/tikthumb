using tikthumb.Ffmpeg;

namespace tikthumb;
public record UploadContext
{
    public FileSaveInfo ImageSaveInfo { set; get; }
    public FileSaveInfo VideoSaveInfo { set; get; }
    public string CurrentTimestamp { set; get; }

    public UploadContext(string tempPath,
        string timeStamp,
        string videoFileNameWithExtension,
        string imageFileNameWithExtension)
    {

        VideoSaveInfo = FilePathHelper.CreateUploadedMediaMetadata(tempPath, videoFileNameWithExtension, timeStamp);
        ImageSaveInfo = FilePathHelper.CreateUploadedMediaMetadata(tempPath, imageFileNameWithExtension, timeStamp);

        CurrentTimestamp = timeStamp;
    }
}