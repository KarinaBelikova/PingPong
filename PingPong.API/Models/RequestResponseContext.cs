using Microsoft.EntityFrameworkCore;

namespace PingPong.Models;

public class RequestResponseContext : DbContext
{
    public RequestResponseContext(DbContextOptions<RequestResponseContext> options) : base(options) { }

    public DbSet<RequestResponseLog> RequestResponseLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequestResponseLog>()
            .HasKey(r => r.CorrelatedId);
    }
}