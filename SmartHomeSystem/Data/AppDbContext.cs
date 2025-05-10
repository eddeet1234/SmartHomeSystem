using Microsoft.EntityFrameworkCore;
using SmartHomeSystem.Data.Model;

namespace SmartHomeSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<LightSchedule> LightSchedules { get; set; }
        public DbSet<UserToken> UserTokens { get; set; } = null!;
        public DbSet<Temperature> Temperatures { get; set; } = null!;
    }
}
