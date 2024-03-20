using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace tikthumb.Db;
public class FeatureRequest
{
    public string Id { set; get; }
    public string RequestContent { set; get; }
}