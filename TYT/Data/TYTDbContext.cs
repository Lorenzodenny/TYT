using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;
using TYT.Models;

namespace TYT.Data
{
    public class TYTDbContext : AuditIdentityDbContext<TYTUser>
    {
        public TYTDbContext(DbContextOptions options) : base(options) { }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AuditWeb> AuditWebs => Set<AuditWeb>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Refresh Token
            builder.Entity<RefreshToken>(cfg =>
            {
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.Token).IsRequired();
                cfg.Property(x => x.UserId).IsRequired();
                cfg.HasIndex(x => x.Token).IsUnique();
            });

            // Log DB
            builder.Entity<AuditLog>(cfg =>
            {
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.AuditDate).IsRequired();
                cfg.Property(x => x.AuditData).HasColumnType("nvarchar(max)");
            });

            // Log API
            builder.Entity<AuditWeb>(cfg =>
            {
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.JsonData).HasColumnType("nvarchar(max)");
                cfg.Property(x => x.LastUpdate);
                cfg.Property(x => x.Method).HasMaxLength(16);
                cfg.Property(x => x.Endpoint).HasMaxLength(2048);
                cfg.Property(x => x.UserAgent).HasMaxLength(1024);
                cfg.Property(x => x.UserName).HasMaxLength(256);
            });
        }
    }
}
