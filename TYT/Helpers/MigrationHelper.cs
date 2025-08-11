using Microsoft.EntityFrameworkCore;
using TYT.Data;

namespace TYT.Helpers
{
    public static class MigrationHelper
    {
        public static IApplicationBuilder UseMigration(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TYTDbContext>();

            // Applica le migrazioni
            dbContext.Database.Migrate();

            return app;
        }
    }
}
