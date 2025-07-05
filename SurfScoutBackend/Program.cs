using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;
using SurfScoutBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Test connection to postgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
    ));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
