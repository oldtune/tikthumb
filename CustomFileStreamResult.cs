
using Microsoft.AspNetCore.Mvc;

namespace tikthumb;

public class CustomFileStreamResult : FileStreamResult
{
    readonly string _filePath;
    public CustomFileStreamResult(Stream fileStream, string filePath, string fileName) : base(fileStream, "application/octet")
    {
        _filePath = filePath;
        FileDownloadName = fileName;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        await base.ExecuteResultAsync(context);
        File.Delete(_filePath);
    }

    public override void ExecuteResult(ActionContext context)
    {
        base.ExecuteResult(context);
        File.Delete(_filePath);
    }
}