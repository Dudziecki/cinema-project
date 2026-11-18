using System.Text;
using Cinema.Api.Data;
using Cinema.Api.Endpoints;
using Cinema.Api.Entities;
using Cinema.Api.Repositories;
using Cinema.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=cinema_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<CinemaDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IMovieRepository, EfMovieRepository>();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdoNetTicketRepository>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtKey = jwtSettings.Key;
if (string.IsNullOrWhiteSpace(jwtKey) && !isTesting)
    throw new InvalidOperationException("Не задан секрет Jwt:Key. Добавьте его через user-secrets или переменную окружения.");
jwtKey ??= "TEST_ONLY_CINEMA_JWT_KEY_12345678901234567890";
var jwtIssuer = jwtSettings.Issuer;
var jwtAudience = jwtSettings.Audience;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("CinemaClient", policy =>
    policy.WithOrigins("http://localhost:5001", "http://localhost:8081")
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("CinemaClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapCinemaEndpoints();

if (!isTesting)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public partial class Program { }
