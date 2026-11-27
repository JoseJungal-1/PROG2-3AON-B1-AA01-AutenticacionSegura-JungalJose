using MantenimientoAutomotriz.Data;
using MantenimientoAutomotriz.Data.Entities;
using MantenimientoAutomotriz.Data.Seed;
using MantenimientoAutomotriz.Negocio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de la conexión a SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. Configuración completa de ASP.NET Core Identity con ApplicationUser y Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Política de contraseñas seguras (Punto 3 de la rúbrica)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Bloqueo temporal por intentos fallidos (fuerza bruta)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // Configuración de usuario y confirmación
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false; // Permite ingreso inmediato tras registro (o true si configuras SMTP real)
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Seguridad de Cookies de Sesión y Redirección de Acceso (Punto 4 y 6 de la rúbrica)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;

    // Rutas personalizadas para el controlador de Cuenta/Seguridad
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// 4. Registro de Servicios de la Capa de Negocio (Inyección de Dependencias)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioAdminService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Inicialización controlada de Roles y Administrador inicial (Seeding - Punto 5 de la rúbrica)
await DbInitializer.InitializeAsync(app.Services, app.Configuration);

// 6. Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Asegura compatibilidad de archivos estáticos

app.UseRouting();

// ¡ORDEN OBLIGATORIO! Primero Authentication (quién eres), luego Authorization (qué permisos tienes)
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();