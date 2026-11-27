using System.ComponentModel.DataAnnotations;

namespace MantenimientoAutomotriz.Models
{
    // DTO encargado de recibir y validar los datos del formulario de registro
    public class RegistroDto
    {
        // Nombre completo del usuario que se va a registrar
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string NombreCompleto { get; set; } = string.Empty;

        // Correo electrónico que funcionará como identificador de acceso
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        // Contraseña principal con validación de longitud mínima por seguridad
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Campo para confirmar que el usuario escribió bien su contraseña
        [Required(ErrorMessage = "Debes confirmar la contraseña.")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}