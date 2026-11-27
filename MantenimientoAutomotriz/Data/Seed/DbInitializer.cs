using MantenimientoAutomotriz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MantenimientoAutomotriz.Data.Seed
{
    /// <summary>
    /// Clase encargada de la inicialización controlada (Seeding) de la base de datos.
    /// Crea los roles obligatorios (Administrador, Cliente) y el usuario Administrador inicial
    /// cumpliendo con la rúbrica de seguridad (sin exposición de credenciales reales).
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // 2. Crear roles obligatorios si no existen
            string[] roles = { "Administrador", "Cliente" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Crear el administrador inicial solo si hay credenciales configuradas.
            string? adminEmail = configuration["AdminSeed:Email"];
            string? adminPassword = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                var administrators = await userManager.GetUsersInRoleAsync("Administrador");
                if (administrators.Count > 0)
                {
                    return;
                }

                throw new InvalidOperationException(
                    "No se puede crear el administrador inicial: configure AdminSeed:Email y AdminSeed:Password mediante User Secrets en desarrollo o variables de entorno en producción.");
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    NombreCompleto = "Administrador Principal",
                    EmailConfirmed = true,
                    Activo = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(error => error.Code));
                    throw new InvalidOperationException(
                        $"No se pudo crear el administrador inicial. Códigos de error de Identity: {errors}");
                }

                var roleResult = await userManager.AddToRoleAsync(adminUser, "Administrador");
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(error => error.Code));
                    throw new InvalidOperationException(
                        $"No se pudo asignar el rol de Administrador. Códigos de error de Identity: {errors}");
                }
            }
        }
    }
}