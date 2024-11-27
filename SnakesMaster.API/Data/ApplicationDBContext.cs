using Microsoft.EntityFrameworkCore;
using SnakesMaster.API.Models;

namespace SnakesMaster.API.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<HighScore> HighScores { get; set; }

    }
}
