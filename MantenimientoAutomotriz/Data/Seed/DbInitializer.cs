using MantenimientoAutomotriz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MantenimientoAutomotriz.Data.Seed
{
    /// <summary>Inicializa la base de datos, los roles y la cuenta administradora inicial.</summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Actualiza la base de datos con las migraciones pendientes.
            await context.Database.MigrateAsync();

            // Crea los roles del sistema si todavía no existen.
            string[] roles = { "Administrador", "Cliente" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Lee los datos configurados para crear al administrador inicial.
            string? adminEmail = configuration["AdminSeed:Email"];
            string? adminPassword = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                // Si ya hay un administrador, no hace falta crear otro.
                var administrators = await userManager.GetUsersInRoleAsync("Administrador");
                if (administrators.Count > 0)
                {
                    return;
                }

                throw new InvalidOperationException(
                    "No se puede crear el administrador inicial: configure AdminSeed:Email y AdminSeed:Password mediante User Secrets en desarrollo o variables de entorno en producción.");
            }

            // Comprueba si ya existe una cuenta con ese correo.
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

                // Crea la cuenta con Identity y comprueba el resultado.
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(error => error.Code));
                    throw new InvalidOperationException(
                        $"No se pudo crear el administrador inicial. Códigos de error de Identity: {errors}");
                }

                // Asigna el rol de Administrador a la cuenta creada.
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