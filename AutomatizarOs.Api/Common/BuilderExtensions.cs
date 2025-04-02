using System.Text;
using System.Text.Json;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Api.Models;
using AutomatizarOs.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AutomatizarOs.Api.Common;

public static class BuilderExtensions
{
    public static void AddDbConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AutomatizarDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
    }
    
    public static void AddJwtConfiguration(this WebApplicationBuilder builder)
    {
        var key = Encoding.ASCII.GetBytes(ApiConfiguration.JwtKey);
        
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
        ApiConfiguration.JwtKey = builder.Configuration["Jwt:Secret"] ?? string.Empty;
        Configuration.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? String.Empty;
        Configuration.BackendUrl = builder.Configuration.GetValue<string>("BackendUrl") ?? String.Empty;
        Configuration.FrontendUrl = builder.Configuration.GetValue<string>("FrontendUrl") ?? String.Empty;
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