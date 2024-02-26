using Newtonsoft.Json;

namespace tikthumb.Ffmpeg;
public record FfmpegStreamInfo
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
    #endregion
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
}