using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MantenimientoAutomotriz.Data.Entities
{
    /// <summary>
    /// Entidad personalizada de usuario que extiende IdentityUser.
    /// Permite almacenar datos adicionales requeridos por la rúbrica de seguridad
    /// sin comprometer el manejo seguro de contraseñas y tokens de ASP.NET Core Identity.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Nombre completo del usuario (requerido en el formulario de registro y visible en el perfil).
        /// </summary>
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la cuenta está habilitada para iniciar sesión.
        /// Permite al Administrador habilitar o deshabilitar cuentas sin eliminar registros históricos.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Fecha de creación de la cuenta (en formato UTC) para trazabilidad y auditoría.
        /// </summary>
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}