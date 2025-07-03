using Microsoft.EntityFrameworkCore;
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

app.UseAuthorization();

app.MapControllers();

app.Run();
