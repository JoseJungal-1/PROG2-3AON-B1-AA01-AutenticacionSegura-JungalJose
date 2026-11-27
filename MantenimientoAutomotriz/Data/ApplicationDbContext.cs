using MantenimientoAutomotriz.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MantenimientoAutomotriz.Data
{
    /// <summary>
    /// Contexto principal de la base de datos en SQL Server.
    /// Hereda de IdentityDbContext<ApplicationUser> para generar todas las tablas de seguridad
    /// (AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserTokens, etc.) incluyendo
    /// los campos personalizados de ApplicationUser.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuración adicional para asegurar integridad en la tabla de usuarios
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.NombreCompleto)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.Activo)
                      .HasDefaultValue(true);
            });
        }
    }
}