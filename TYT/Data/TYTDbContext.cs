using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TYT.Models;

namespace TYT.Data
{
    public class TYTDbContext : IdentityDbContext<TYTUser>
    {
        public TYTDbContext(DbContextOptions<TYTDbContext> options) 
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
        }
    }
}
