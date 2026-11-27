using MantenimientoAutomotriz.Data;
using MantenimientoAutomotriz.Data.Entities;
using MantenimientoAutomotriz.Data.Seed;
using MantenimientoAutomotriz.Negocio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configura la conexión a SQL Server.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configura Identity para usuarios y roles.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Requisitos para las contraseñas nuevas.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Bloquea temporalmente la cuenta después de varios intentos fallidos.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // Cada cuenta debe usar un correo único; no se exige confirmarlo para iniciar sesión.
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configura la cookie y las rutas de autenticación.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;

    // Rutas para iniciar sesión, cerrar sesión y mostrar acceso denegado.
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// Registra los servicios de autenticación y administración.
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioAdminService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Aplica migraciones y prepara los roles y el administrador inicial.
await DbInitializer.InitializeAsync(app.Services, app.Configuration);

// Configura el pipeline HTTP según el entorno.
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
app.UseStaticFiles();

app.UseRouting();

// Primero identifica al usuario y luego comprueba sus permisos.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();