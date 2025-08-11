// Shared/Policy.cs
using Microsoft.Extensions.DependencyInjection;

namespace TYT.Shared
{
    public static class PolicyNames
    {
        public const string UserOnly = "UserOnly";
        public const string AdminOnly = "AdminOnly";
        public const string SuperAdminOnly = "SuperAdminOnly";
        public const string AdminOrSuperAdmin = "AdminOrSuperAdmin";
        public const string AnyAuthenticated = "AnyAuthenticated"; 
    }

    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddTytAuthorization(this IServiceCollection services)
        {
            const string roleType = nameof(Enums.TYTRole);

            services.AddAuthorization(options =>
            {
                // Solo User
                options.AddPolicy(PolicyNames.UserOnly,
                    p => p.RequireClaim(roleType, Enums.TYTRole.User.ToString()));

                // Solo Admin
                options.AddPolicy(PolicyNames.AdminOnly,
                    p => p.RequireClaim(roleType, Enums.TYTRole.Admin.ToString()));

                // Solo SuperAdmin
                options.AddPolicy(PolicyNames.SuperAdminOnly,
                    p => p.RequireClaim(roleType, Enums.TYTRole.SuperAdmin.ToString()));


                // Admin o SuperAdmin
                options.AddPolicy(PolicyNames.AdminOrSuperAdmin,
                    p => p.RequireClaim(roleType,
                        Enums.TYTRole.Admin.ToString(),
                        Enums.TYTRole.SuperAdmin.ToString()));

                // Chiunque sia autenticato
                options.AddPolicy(PolicyNames.AnyAuthenticated, p => p.RequireAuthenticatedUser());
            });

            return services;
        }
    }
}
