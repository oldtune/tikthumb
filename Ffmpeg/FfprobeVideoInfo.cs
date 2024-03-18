using Newtonsoft.Json;

namespace tikthumb.Ffmpeg;
public class FfprobeVideoInfo
{
    public List<FfmpegStreamInfo> Streams { get; set; }
    [JsonIgnore]
    public FfmpegAudioStreamInfo? AudioStreamInfo
    {
        get
        {
            var audioStream = Streams.FirstOrDefault(x => x.CodecType == "audio");
            if (audioStream == null)
            {
                return null;
            }

            return new FfmpegAudioStreamInfo(audioStream);
        }
    }

    [JsonIgnore]
    public FfmpegVideoStreamInfo VideoStreamInfo => new FfmpegVideoStreamInfo(Streams.FirstOrDefault(x => x.CodecType == "video"));

    public FfprobeVideoInfo()
    {

    }
}