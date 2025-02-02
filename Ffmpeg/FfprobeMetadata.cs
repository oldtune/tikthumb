using Newtonsoft.Json;

namespace tikthumb.Ffmpeg;
public class FfmpegStreamInfo
{
    #region common
    [JsonProperty("codec_type")]
    public string CodecType { get; set; }
    [JsonProperty("codec_name")]
    public string CodecName { get; set; }
    [JsonProperty("profile")]
    public string Profile { get; set; }
    [JsonProperty("bit_rate")]
    public string BitRate { get; set; }
    #endregion

    #region video
    [JsonProperty("pix_fmt")]
    public string PixelFormat { get; set; }
    [JsonProperty("level")]
    public string Level { get; set; }
    [JsonProperty("avg_frame_rate")]
    public string AverageFrameRate { get; set; }
    #endregion

    #region audio
    [JsonProperty("sample_fmt")]
    public string SampleFormat { get; set; }
    [JsonProperty("sample_rate")]
    public string SampleRate { get; set; }
    [JsonProperty("channels")]
    public string Channels { get; set; }
    [JsonProperty("channel_layout")]
    public string ChannelLayout { get; set; }
    #endregion
}

public record FfmpegVideoStreamInfo
{
    #region common
    [JsonProperty("codec_type")]
    public string CodecType { get; }
    [JsonProperty("codec_name")]
    public string CodecName { get; }
    [JsonProperty("profile")]
    public string Profile { get; }
    [JsonProperty("bit_rate")]
    public string BitRate { get; }
    #endregion

    #region video
    [JsonProperty("pix_fmt")]
    public string PixelFormat { get; }
    [JsonProperty("level")]
    public string Level { get; }
    [JsonProperty("avg_frame_rate")]
    public string AverageFrameRate { get; }
    [JsonIgnore]
    public string FrameRate => string.IsNullOrWhiteSpace(AverageFrameRate) ? "0" : AverageFrameRate.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    #endregion

    public FfmpegVideoStreamInfo()
    {

    }

    public FfmpegVideoStreamInfo(FfmpegStreamInfo streamInfo)
    {
        CodecType = streamInfo.CodecType;
        CodecName = streamInfo.CodecName;
        Profile = streamInfo.Profile;
        BitRate = streamInfo.BitRate;
        PixelFormat = streamInfo.PixelFormat;
        Level = streamInfo.Level;
        AverageFrameRate = streamInfo.AverageFrameRate;
    }
}

public record FfmpegAudioStreamInfo
{
    #region common
    [JsonProperty("codec_type")]
    public string CodecType { get; }
    [JsonProperty("codec_name")]
    public string CodecName { get; }
    [JsonProperty("profile")]
    public string Profile { get; }
    [JsonProperty("bit_rate")]
    public string BitRate { get; }
    #endregion

    #region audio
    [JsonProperty("sample_fmt")]
    public string SampleFormat { get; }
    [JsonProperty("sample_rate")]
    public string SampleRate { get; }
    [JsonProperty("channels")]
    public string Channels { get; }
    [JsonProperty("channel_layout")]
    public string ChannelLayout { get; }
    #endregion

    public FfmpegAudioStreamInfo()
    {

    }

    public FfmpegAudioStreamInfo(FfmpegStreamInfo streamInfo)
    {
        CodecType = streamInfo.CodecType;
        CodecName = streamInfo.CodecName;
        Profile = streamInfo.Profile;
        BitRate = streamInfo.BitRate;
        SampleFormat = streamInfo.SampleFormat;
        SampleRate = streamInfo.SampleRate;
        Channels = streamInfo.Channels;
        ChannelLayout = streamInfo.ChannelLayout;
    }
}