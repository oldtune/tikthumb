using Newtonsoft.Json;

namespace tikthumb.Ffmpeg;
public record FfprobeVideoInfo
{
    public List<FfmpegStreamInfo> Streams { get; }
    [JsonIgnore]
    public FfmpegAudioStreamInfo AudioStreamInfo { get; }
    [JsonIgnore]
    public FfmpegVideoStreamInfo VideoStreamInfo { get; }

    public FfprobeVideoInfo(IEnumerable<FfmpegStreamInfo> streams)
    {
        AudioStreamInfo = new FfmpegAudioStreamInfo(streams.FirstOrDefault(x => x.CodecType == "audio"));
        VideoStreamInfo = new FfmpegVideoStreamInfo(streams.FirstOrDefault(x => x.CodecType == "video"));
    }
}