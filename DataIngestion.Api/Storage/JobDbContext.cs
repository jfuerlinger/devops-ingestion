using Microsoft.EntityFrameworkCore;

namespace DevOpsIngestion.Api.Storage
{
    public class JobDbContext(DbContextOptions o) : DbContext(o)
    {
        public DbSet<JobRecord> Jobs { get; set; }
    }
}
