using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace tikthumb.Db;
public class BugReport
{
    public string Id { set; get; }
    public string BugReportContent { set; get; }
}