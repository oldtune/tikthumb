namespace tikthumb.Ffmpeg;
public record FfprobeVideoInfo
{
    private List<FfmpegStreamInfo> Streams { get; }
    public FfmpegAudioStreamInfo AudioStreamInfo { get; }
    public FfmpegVideoStreamInfo VideoStreamInfo { get; }

    public FfprobeVideoInfo(IEnumerable<FfmpegStreamInfo> streams)
    {
        AudioStreamInfo = new FfmpegAudioStreamInfo(streams.FirstOrDefault(x => x.CodecType == "audio"));
        VideoStreamInfo = new FfmpegVideoStreamInfo(streams.FirstOrDefault(x => x.CodecType == "video"));
    }
}