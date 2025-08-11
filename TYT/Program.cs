using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TYT.Data;
using TYT.Helpers;
using TYT.Models;
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
        // Impostazioni minime per ora; puoi personalizzare dopo
        opt.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<TYTDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt =>
    {
        opt.LoginPath = "/api/auth/login";
        opt.LogoutPath = "/api/auth/logout";
        opt.Cookie.Name = "tyt_auth";
        opt.SlidingExpiration = true;
        opt.ExpireTimeSpan = TimeSpan.FromHours(1);
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax; 
        opt.Cookie.SecurePolicy = CookieSecurePolicy.None;

        // niente redirect: restituiamo 401/403
        opt.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

// Authenticazione gestita in PolicyAuthorization
builder.Services.AddTytAuthorization();

// Registro servizio per Scrittura/Lettura ruoli ( in AspnetUserClaims )
builder.Services.AddScoped<IRoleClaimsService, RoleClaimsService>();

// Carter + MediatR + FluentValidation
builder.Services.AddCarter();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EmailService
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<EmailSenderHelper>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Helper per le migrazioni all'avvio dell'app
    app.UseMigration();
}

// CORS
app.UseCors(CORSConst.AllowFrontend);

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.Run();
