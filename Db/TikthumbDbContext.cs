using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace tikthumb.Db;
public class TikthumbDbContext : DbContext, IDataProtectionKeyContext
{
    public TikthumbDbContext(DbContextOptions<TikthumbDbContext> options) : base(options)
    {

    }

    public DbSet<FeatureRequest> FeatureRequests { set; get; }
    public DbSet<BugReport> BugReports { set; get; }
    public DbSet<LeaveAMessage> Messages { set; get; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}