using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.IO.Converters;
using NetTopologySuite;
using System.Text;
using Npgsql;
using SurfScoutBackend.Data;
using SurfScoutBackend.Weather;
using SurfScoutBackend.BackgroundTasks;
using System.Net.Http.Headers;

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
var secretKey = builder.Configuration["Jwt:Key"];
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

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS policy to allow requests from Angular Dev-Server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")     // Angular Dev-Server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Backgroundservice for polling wind forecast data
builder.Services.AddHostedService<WindForecastPoller>();

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

// Open Meteo weather client
builder.Services.AddHttpClient("OpenMeteoClient", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
});
builder.Services.AddTransient<OpenMeteoWeatherClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("OpenMeteoClient");

    return new OpenMeteoWeatherClient(httpClient);
});

// Stormglass weather client
builder.Services.AddHttpClient("StormglassClient", client =>
{
    var config = builder.Configuration;
    var apiKey = config["Stormglass:ApiKey"];
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
});
builder.Services.AddTransient<StormglassWeatherClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["Stormglass:ApiKey"];
    var httpClient = factory.CreateClient("StormglassClient");

    return new StormglassWeatherClient(httpClient, apiKey);
});

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

// Enable CORS policy
app.UseCors("AllowAngularDev");

app.MapControllers();

app.Run();
