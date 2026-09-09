using Kanban.Api.Models; 
using Microsoft.EntityFrameworkCore;

namespace Kanban.Api.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<Canban> KanbanTasks => Set<Canban>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<TeamMember>()
                .HasIndex(tm => new { tm.TeamId, tm.UserId })
                .IsUnique();

            modelBuilder.Entity<TeamMember>()
                .HasIndex(tm => tm.UserId);

            modelBuilder.Entity<TeamMember>()
                .HasIndex(tm => tm.TeamId);

            modelBuilder.Entity<Canban>()
                .HasIndex(c => c.UserId);

            modelBuilder.Entity<Canban>()
                .HasIndex(c => c.TeamId);
        }
    }
}