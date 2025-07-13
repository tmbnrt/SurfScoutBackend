using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.IO.Converters;
using NetTopologySuite;
using System.Text;
using Npgsql;
using SurfScoutBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Test connection to postgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Spatial Registration for Npgsql --v7 and higher
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseNetTopologySuite();
var dataSource = dataSourceBuilder.Build();

try
{
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    Console.WriteLine("Connection to database successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR  No connection to database: {ex.Message}");
}

// JSON Web Token - JWT to not always use password authentification - Token stored in client app
var secretKey = "YOUR_SECRET_JWT_KEY_123";      // Change later from appsettings.json
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();            // later: for roles and policies (admin, etc.)

// Add services to container
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(dataSource, npgsql =>
    {
        npgsql.UseNetTopologySuite();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
