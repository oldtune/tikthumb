using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace tikthumb.Db;
public class LeaveAMessage
{
    public string Id { set; get; }
    public string Content { set; get; }
}