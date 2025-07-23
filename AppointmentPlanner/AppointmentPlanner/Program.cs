using AppointmentPlanner.Client;
using AppointmentPlanner.Components;
using AppointmentPlanner.Data;
using AppointmentPlanner.Models;
using AppointmentPlanner.Models.Identity;
using AppointmentPlanner.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework and PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.Admin));
    options.AddPolicy("ProfessionalOnly", policy => policy.RequireRole(UserRoles.Professional));
    options.AddPolicy("ClientOnly", policy => policy.RequireRole(UserRoles.Client));
    options.AddPolicy("AdminOrProfessional", policy =>
        policy.RequireRole(UserRoles.Admin, UserRoles.Professional));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add client-side authentication services for server-side pre-rendering
builder.Services.AddScoped<AppointmentPlanner.Client.Services.ILocalStorageService, AppointmentPlanner.Client.Services.LocalStorageService>();
builder.Services.AddScoped<AppointmentPlanner.Client.Services.IAuthenticationService, AppointmentPlanner.Client.Services.AuthenticationService>();
builder.Services.AddScoped<AuthenticationStateProvider, AppointmentPlanner.Client.Services.CustomAuthStateProvider>();

// Add custom services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<SfDialogService>();
builder.Services.AddSingleton<AppointmentService, AppointmentService>();
builder.Services.AddSingleton<Appointment, Appointment>();

// Add API controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map API controllers
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    try
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting database initialization...");

        context.Database.EnsureCreated();
        logger.LogInformation("Database created/verified successfully.");

        await SeedDefaultAdminUser(userManager, roleManager, logger);
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database or seeding data.");
    }
}

app.Run();

// Helper method to seed default admin user
static async Task SeedDefaultAdminUser(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger logger)
{
    // Ensure roles exist
    logger.LogInformation("Checking and creating roles...");
    foreach (var roleName in UserRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            logger.LogInformation($"Creating role: {roleName}");
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                Description = $"Default {roleName} role"
            });
        }
        else
        {
            logger.LogInformation($"Role already exists: {roleName}");
        }
    }

    // Create default admin user
    const string adminEmail = "admin@appointmentplanner.com";
    logger.LogInformation($"Checking for admin user: {adminEmail}");
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        logger.LogInformation("Admin user not found. Creating new admin user...");
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            logger.LogInformation("Admin user created successfully. Adding to Admin role...");
            await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            logger.LogInformation("Admin user added to Admin role successfully.");
        }
        else
        {
            logger.LogError($"Failed to create admin user. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
    else
    {
        logger.LogInformation("Admin user already exists in database.");
    }
}
