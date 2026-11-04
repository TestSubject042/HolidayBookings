using Bookings.Data;
using Bookings.Data.Entities;
using Bookings.API.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddAuthorization();
builder.Services
    .AddIdentityApiEndpoints<User>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 12;
        //o.SignIn.RequireConfirmedEmail = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<BookingsDbContext>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must contain at least one origin.");

builder.Services.AddCors(o => o.AddPolicy("web", p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));


builder.Services.AddControllers().AddJsonOptions(
        o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
    );
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
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/auth").MapIdentityApi<User>();
app.MapControllers();

app.Run();