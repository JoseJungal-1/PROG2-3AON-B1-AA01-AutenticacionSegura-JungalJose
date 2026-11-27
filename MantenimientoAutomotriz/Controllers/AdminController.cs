using System.ComponentModel.DataAnnotations;
using MantenimientoAutomotriz.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MantenimientoAutomotriz.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UsuarioAdminService _usuarioAdminService;

        public AdminController(UsuarioAdminService usuarioAdminService)
        {
            _usuarioAdminService = usuarioAdminService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _usuarioAdminService.ObtenerTodosLosUsuariosAsync();
            return View(usuarios);
        }

        [HttpGet]
        public IActionResult CrearUsuario()
        {
            return View(new CrearUsuarioAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioAdminViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var (exito, mensaje) = await _usuarioAdminService.CrearUsuarioConRolAsync(
                modelo.NombreCompleto,
                modelo.Email,
                modelo.Password,
                modelo.Rol);

            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, mensaje);
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(string userId, string nuevoRol)
        {
            var (exito, mensaje) = await _usuarioAdminService.CambiarRolUsuarioAsync(userId, nuevoRol);

            if (exito)
                TempData["MensajeExito"] = mensaje;
            else
                TempData["MensajeError"] = mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(string userId, bool nuevoEstado)
        {
            var (exito, mensaje) = await _usuarioAdminService.CambiarEstadoActivoAsync(userId, nuevoEstado);

            if (exito)
                TempData["MensajeExito"] = mensaje;
            else
                TempData["MensajeError"] = mensaje;

            return RedirectToAction(nameof(Index));
        }
    }

    public class CrearUsuarioAdminViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un rol.")]
        [Display(Name = "Rol del Usuario")]
        public string Rol { get; set; } = "Cliente";
    }
}