using APIBlazor.Model;
using Microsoft.EntityFrameworkCore;

namespace APIBlazor.DataBaseContext
{
    public class ContextDB : DbContext
    {
        public ContextDB(DbContextOptions options) : base(options) 
        {
            
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Logins>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
        public DbSet<Users> Users { get; set; }
        public DbSet<Logins> Logins { get; set; }
        public DbSet<Role> Role {  get; set; }
    }
}
