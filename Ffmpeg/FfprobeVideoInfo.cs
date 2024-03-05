using Newtonsoft.Json;

namespace tikthumb.Ffmpeg;
public class FfprobeVideoInfo
{
    public List<FfmpegStreamInfo> Streams { get; set; }
    [JsonIgnore]
    public FfmpegAudioStreamInfo AudioStreamInfo
        => new FfmpegAudioStreamInfo(Streams.FirstOrDefault(x => x.CodecType == "audio"));

    [JsonIgnore]
    public FfmpegVideoStreamInfo VideoStreamInfo => new FfmpegVideoStreamInfo(Streams.FirstOrDefault(x => x.CodecType == "video"));

    public FfprobeVideoInfo()
    {

    }
}