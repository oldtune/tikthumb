namespace tikthumb.Ffmpeg;
public record FfprobeVideoInfo
{
    private List<FfmpegStreamInfo> Streams { get; }
    public FfmpegAudioStreamInfo AudioStreamInfo { get; }
    public FfmpegVideoStreamInfo VideoStreamInfo { get; }

    public FfprobeVideoInfo(IEnumerable<FfmpegStreamInfo> streams)
    {
        // this.AudioStreamInfo = streams.FirstOrDefault(x => x.CodecType == "audio");
        // this.VideoStreamInfo = streams.FirstOrDefault(x => x.CodecType == "video");
    }
}