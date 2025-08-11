using Carter;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using TYT.Data;
using TYT.Helpers;
using TYT.Models;
using TYT.Services.Auth;
using TYT.Services.EmailService;
using TYT.Services.Security;
using TYT.Shared;


var builder = WebApplication.CreateBuilder(args);

// Connection string
var cs = builder.Configuration.GetConnectionString("Default");

// DbContext
builder.Services.AddDbContext<TYTDbContext>(opt =>
    opt.UseSqlServer(cs));

// Identity (cookie)
builder.Services
    .AddIdentity<TYTUser, IdentityRole>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.SignIn.RequireConfirmedEmail = true; // Email confermata per loggare
    })
    .AddEntityFrameworkStores<TYTDbContext>()
    .AddDefaultTokenProviders();

// Bind opzioni JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // true in prod dietro HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SecretKey)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = nameof(TYT.Shared.Enums.TYTRole)
        };
    });

// Authorization policy già centralizzata in Shared/PolicyAuthorization.cs
builder.Services.AddTytAuthorization();

// Servizio JWT
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Registro servizio per Scrittura/Lettura ruoli ( in AspnetUserClaims )
builder.Services.AddScoped<IRoleClaimsService, RoleClaimsService>();

// Carter + MediatR + FluentValidation
builder.Services.AddCarter();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TYT API", Version = "v1" });

    // Security: Bearer
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci: Bearer {access_token}"
    };

    c.AddSecurityDefinition("Bearer", scheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

// EmailService
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<EmailSenderHelper>();

// Hangfire
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(cs, new SqlServerStorageOptions
    {
        PrepareSchemaIfNecessary = true
    })
);

builder.Services.AddHangfireServer();

// Gestione Claims
builder.Services.AddScoped<IJwtClaimsFactory, JwtClaimsFactory>();

// HTTPContext
builder.Services.AddHttpContextAccessor();

// CORS per comunicare col frontend
var corsOrigin = builder.Configuration["Cors:FrontendOrigin"]
    ?? throw new InvalidOperationException("Cors:FrontendOrigin non configurato.");

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORSConst.AllowFrontend, policy =>
    {
        policy.WithOrigins(corsOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Audit
builder.Services.ConfigureAudit(builder.Configuration);

var app = builder.Build();

// Exception centralizzata
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception");

        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Title = "Errore imprevisto",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Qualcosa è andato storto. Riprova più tardi."
        };

        await ctx.Response.WriteAsJsonAsync(problem);
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Dashboard Hangfire 
    app.UseHangfireDashboard("/hangfire");

    // Helper per le migrazioni all'avvio dell'app
    app.UseMigration();
}

// CORS
app.UseCors(CORSConst.AllowFrontend);

app.UseAuthentication();
app.UseAuthorization();

app.UseAudit();

app.MapCarter();

app.Run();
