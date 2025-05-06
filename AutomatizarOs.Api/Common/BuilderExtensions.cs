using System.Text;
using System.Text.Json;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Api.Handlers;
using AutomatizarOs.Api.Models;
using AutomatizarOs.Api.Repositories;
using AutomatizarOs.Api.Services;
using AutomatizarOs.Core;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;

namespace AutomatizarOs.Api.Common;

public static class BuilderExtensions
{
    public static void AddDbConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AutomatizarDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
                .EnableSensitiveDataLogging());
    }
    
    public static void AddJwtConfiguration(this WebApplicationBuilder builder)
    {
        var key = Encoding.ASCII.GetBytes(ApiConfiguration.Key);
        
        builder.Services.AddAuthentication(x =>
            {	
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        ctx.Request.Cookies.TryGetValue("accessToken", out var accessToken);
                        if (!string.IsNullOrEmpty(accessToken))
                            ctx.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });
    }
    
    public static void AddIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityCore<User>()
            .AddRoles<IdentityRole<long>>()
            .AddEntityFrameworkStores<AutomatizarDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }
    
    public static void AddConfigurationApiUrl(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables();
        
        ApiConfiguration.Key = builder.Configuration.GetValue<string>("Jwt:Secret") ?? string.Empty;
        ApiConfiguration.AccessConnection = builder.Configuration.GetValue<string>("AccessConnection") ?? string.Empty;
        Configuration.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        Configuration.BackendUrl = builder.Configuration.GetValue<string>("BackendUrl") ?? string.Empty;
        Configuration.FrontendUrl = builder.Configuration.GetValue<string>("FrontendUrl") ?? string.Empty;
    }
    
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<JwtService>();
        builder.Services.AddScoped<IIdentityHandler, IdentityHandler>();
        builder.Services.AddScoped<IServiceOrderHandler, ServiceOrderHandler>();
        builder.Services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        builder.Services.AddSignalR();
        builder.Services.AddQuartz(q =>
        {
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
    
            var jobKey = new JobKey("ServiceOrderSyncJob");
            q.AddJob<ServiceOrderJob>(jobKey, j => j
                .StoreDurably()
                .WithDescription("Job de sincronização de ordens de serviço")
            );
    
            q.AddTrigger(t => t
                .WithIdentity("ServiceOrderTrigger")
                .ForJob(jobKey)
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(30)
                    .RepeatForever())
            );
        });
        builder.Services.AddQuartzHostedService(q => 
        {
            q.WaitForJobsToComplete = true;
            q.StartDelay = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddScoped<ServiceOrderJob>();
    }
    
    public static void AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true; 
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; 
            });
    }
    
    public static void AddSwaggerGen(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                BearerFormat = "JWT",
                Description = "Insira o token JWT no formato 'Bearer {token}'"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });
    }
    
    public static void AddCorsConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options => options.AddPolicy(
            ApiConfiguration.CorsPolicyName,
            policy => policy
                .WithOrigins(
                [
                    Configuration.BackendUrl,
                    Configuration.FrontendUrl
                ])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
        ));
    }
}