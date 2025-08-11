using System.Threading;
using Audit.Core;
using Audit.EntityFramework;
using Audit.EntityFramework.Providers;
using Audit.SqlServer.Providers;       // viene da Audit.NET.SqlServer
using Audit.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TYT.Data;
using TYT.Models;

// Alias per risolvere l'ambiguità "Configuration"
using CoreConfig = Audit.Core.Configuration;
using EfConfig = Audit.EntityFramework.Configuration;

namespace TYT.Helpers;

public static class AuditHelper
{
    public static void ConfigureAudit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        // EF → salva eventi su tabella AuditLogs
        var efProvider = new EntityFrameworkDataProvider(cfg => cfg
            .AuditTypeMapper(_ => typeof(AuditLog))
            .AuditEntityAction<AuditLog>((ev, entry, entity) =>
            {
                entity.AuditData = entry.ToJson();
                entity.EntityType = entry.EntityType.Name;
                entity.AuditDate = DateTime.UtcNow;
                entity.AuditUser = ev.Environment.UserName;
                entity.TablePk = entry.PrimaryKey.FirstOrDefault().Value?.ToString();
            })
            .IgnoreMatchedProperties()
        );

        // WebAPI → salva eventi su tabella AuditWebs
        var conn = new SqlConnectionStringBuilder(configuration.GetConnectionString("Default")!).ConnectionString;
        var sqlProvider = new SqlDataProvider(cfg => cfg
            .ConnectionString(conn)
            .TableName("AuditWebs")
            .Schema("dbo")
            .IdColumnName("Id")
            .JsonColumnName("JsonData")
            .LastUpdatedColumnName("LastUpdate")
            .CustomColumn("IpAddress", ev => ev is AuditEventWebApi aew ? aew.Action.IpAddress : null)
            .CustomColumn("Method", ev => ev is AuditEventWebApi aew ? aew.Action.HttpMethod : null)
            .CustomColumn("Endpoint", ev => ev is AuditEventWebApi aew ? aew.Action.RequestUrl : null)
            .CustomColumn("UserAgent", ev =>
                ev is AuditEventWebApi aew && aew.Action.Headers.TryGetValue("User-Agent", out var ua) ? ua : null)
            .CustomColumn("UserName", ev => ev.Environment.UserName)
            .CustomColumn("AuditStartDate", ev => ev.StartDate)
            .CustomColumn("AuditEndDate", ev => ev.EndDate)
            .CustomColumn("StatusCode", ev => ev is AuditEventWebApi aew ? aew.Action.ResponseStatusCode : null)
        );

        // Multiprovider: WebAPI → AuditWebs, altrimenti → AuditLogs
        CoreConfig.Setup().UseCustomProvider(new Multiprovider(efProvider, sqlProvider));

        // EF Core: setup sul tuo DbContext (niente UseOptOut in v22)
        EfConfig.Setup()
            .ForContext<TYTDbContext>(ctx => ctx
                .IncludeEntityObjects()
                .AuditEventType("EF:{context}")
            // Se vuoi escludere qualche entità:
            // .Exclude<TuaEntita>()
            );

        // OnScopeCreated: imposta l'utente leggendo da HttpContext (se presente)
        CoreConfig.AddCustomAction(ActionType.OnScopeCreated, scope =>
        {
            // NB: BuildServiceProvider qui va bene per questo scopo (setup one-shot)
            var sp = services.BuildServiceProvider();
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            scope.Event.Environment.UserName = accessor.HttpContext?.User?.Identity?.Name;
        });
    }

    public static void UseAudit(this IApplicationBuilder app)
    {
        // Middleware WebAPI
        app.UseAuditMiddleware(cfg => cfg
            .FilterByRequest(RequestFilter())
            .IncludeHeaders()
            .IncludeRequestBody()
            .IncludeResponseBody()
        );
    }

    private static Func<HttpRequest, bool> RequestFilter() =>
        rq => rq.Path.Value is { } p &&
              !(p.Contains("swagger") || p.Contains("favicon.ico") || p.Contains("hangfire"));

    // ———————————————————————————————————————————————————————————

    public class Multiprovider : AuditDataProvider
    {
        private readonly EntityFrameworkDataProvider _ef;
        private readonly SqlDataProvider _sql;
        private static readonly string[] Skip = ["swagger", "favicon.ico", "hangfire"];

        public Multiprovider(EntityFrameworkDataProvider ef, SqlDataProvider sql)
        {
            _ef = ef; _sql = sql;
        }

        public override object InsertEvent(AuditEvent ev)
        {
            var url = ev.GetWebApiAuditAction()?.RequestUrl;
            if (IsSkip(url)) return "";
            return ev is AuditEventWebApi ? _sql.InsertEvent(ev) : _ef.InsertEvent(ev);
        }

        public override Task<object> InsertEventAsync(AuditEvent ev, CancellationToken ct = default)
        {
            var url = ev.GetWebApiAuditAction()?.RequestUrl;
            if (IsSkip(url)) return Task.FromResult<object>("");
            return ev is AuditEventWebApi ? _sql.InsertEventAsync(ev, ct) : _ef.InsertEventAsync(ev, ct);
        }

        public override void ReplaceEvent(object id, AuditEvent ev)
        {
            if (ev is AuditEventWebApi) { _sql.ReplaceEvent(id, ev); return; }
            _ef.ReplaceEvent(id, ev);
        }

        public override Task ReplaceEventAsync(object id, AuditEvent ev, CancellationToken ct = default)
            => ev is AuditEventWebApi ? _sql.ReplaceEventAsync(id, ev, ct) : _ef.ReplaceEventAsync(id, ev, ct);

        private static bool IsSkip(string? url) => url is not null && Skip.Any(url.Contains);
    }
}
