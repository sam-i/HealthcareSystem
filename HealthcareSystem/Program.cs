using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using HealthcareSystem.Models;
using HealthcareSystem.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services for controllers and views
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger UI and configure JWT in Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Healthcare System API",
        Version = "v1"
    });

    // Add JWT Authentication to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["JWT:Secret"];  // Get the Secret from appsettings.json
        var issuer = builder.Configuration["JWT:Issuer"];  // Get the Issuer
        var audience = builder.Configuration["JWT:Audience"];  // Get the Audience

        Debug.WriteLine($"JWT Key: {key}");
        Debug.WriteLine($"Issuer: {issuer}");
        Debug.WriteLine($"Audience: {audience}");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

var app = builder.Build();

// Seed the database (if necessary)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var seeder = new DatabaseSeeder(context);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Middleware configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();  // Enable authentication middleware
app.UseAuthorization();   // Enable authorization middleware

// Define logout route
app.MapPost("/logout", async (HttpContext context) =>
{
    // Invalidate the current JWT token by clearing the cookies or performing any other necessary actions
    await context.SignOutAsync();
    return Results.Ok(new { message = "Logged out successfully" });
});

// Set the default route to the Login action in the AuthController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "admin",
    pattern: "{controller=Admin}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "patient",
    pattern: "{controller=Patient}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "radiologist",
    pattern: "{controller=Radiologist}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "doctor",
    pattern: "{controller=Doctor}/{action=Dashboard}/{id?}");

app.Run();
