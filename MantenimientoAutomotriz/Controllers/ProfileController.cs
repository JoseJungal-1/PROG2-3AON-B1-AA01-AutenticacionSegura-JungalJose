using System.ComponentModel.DataAnnotations;
using MantenimientoAutomotriz.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MantenimientoAutomotriz.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return RedirectToAction("Login", "Auth");

            var roles = await _userManager.GetRolesAsync(usuario);

            var modelo = new PerfilViewModel
            {
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email ?? string.Empty,
                Rol = roles.FirstOrDefault() ?? "Cliente",
                FechaRegistro = usuario.FechaRegistro
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfil(PerfilViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", modelo);
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return RedirectToAction("Login", "Auth");

            usuario.NombreCompleto = modelo.NombreCompleto;
            var resultado = await _userManager.UpdateAsync(usuario);

            if (resultado.Succeeded)
            {
                TempData["MensajeExito"] = "Datos del perfil actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Index", modelo);
        }

        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View(new CambiarPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return RedirectToAction("Login", "Auth");

            var resultado = await _userManager.ChangePasswordAsync(
                usuario,
                modelo.PasswordActual,
                modelo.NuevaPassword);

            if (resultado.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(usuario);
                TempData["MensajeExito"] = "Contraseña actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }
    }

    public class PerfilViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Rol Asignado")]
        public string Rol { get; set; } = string.Empty;

        [Display(Name = "Fecha de registro (hora local)")]
        public DateTime FechaRegistro { get; set; }
    }

    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "Ingresa tu contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña Actual")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa la nueva contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string NuevaPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nueva Contraseña")]
        [Compare("NuevaPassword", ErrorMessage = "La nueva contraseña y su confirmación no coinciden.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}