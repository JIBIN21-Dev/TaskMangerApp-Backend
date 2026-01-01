using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TaskManger.Data;

var builder = WebApplication.CreateBuilder(args);

// Listen on Railway port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Services
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// ⭐ DB CONFIG
var connection = builder.Configuration.GetConnectionString("TasksDatabase");

builder.Services.AddDbContext<TaskDb>(options =>
    options.UseMySql(connection, ServerVersion.AutoDetect(connection)));

var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettingsSection["SecretKey"];

// Swagger + Auth + CORS (same as before…)

var MyCorsPolicy = "_myCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://taskmangerapp-frontend-production.up.railway.app"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(secretKey)
                )
        };
    });

// ⭐ BUILD APP HERE
var app = builder.Build();

// ⭐ CREATE TABLES (AFTER build, BEFORE middleware)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDb>();
    db.Database.EnsureCreated();
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(MyCorsPolicy);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
