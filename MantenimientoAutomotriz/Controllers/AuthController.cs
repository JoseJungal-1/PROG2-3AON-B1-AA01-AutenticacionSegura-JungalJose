using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MantenimientoAutomotriz.Models;
using MantenimientoAutomotriz.Negocio;

namespace MantenimientoAutomotriz.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly IWebHostEnvironment _environment;

        public AuthController(AuthService authService, IWebHostEnvironment environment)
        {
            _authService = authService;
            _environment = environment;
        }

        // ==========================================
        // REGISTRO DE USUARIOS (Rol "Cliente" automático)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Registro()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroDto modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var resultado = await _authService.RegistrarUsuarioAsync(modelo);

            if (resultado.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var errores = resultado.Errors.ToList();
            var errorCorreoDuplicado = errores.FirstOrDefault(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName");

            if (errorCorreoDuplicado != null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Email),
                    $"El correo electrónico '{modelo.Email}' ya está tomado.");
            }

            foreach (var error in errores.Where(error =>
                         error.Code is not "DuplicateEmail" and not "DuplicateUserName"))
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        // ==========================================
        // INICIO DE SESIÓN (LOGIN)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool recordarMe = false, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Debes ingresar tu correo electrónico y contraseña.");
                return View();
            }

            var resultado = await _authService.IniciarSesionAsync(email, password, recordarMe);

            if (resultado.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta ha sido bloqueada por 15 minutos debido a 3 intentos fallidos. Intenta nuevamente dentro de 15 minutos.");
                return View();
            }

            if (resultado.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Acceso denegado. Tu cuenta se encuentra deshabilitada o no tiene permisos para ingresar.");
                return View();
            }

            ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
            return View();
        }

        // ==========================================
        // CERRAR SESIÓN (LOGOUT)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.CerrarSesionAsync();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // ACCESO DENEGADO (Autorización por Roles)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var resultado = await _authService.SolicitarRecuperacionAsync(email);

            ViewBag.MensajeSolicitado = true;

            if (_environment.IsDevelopment() && resultado.UsuarioEncontrado)
            {
                ViewBag.ResetUrl = Url.Action(
                    "ResetPassword",
                    "Auth",
                    new { email = resultado.Email, token = resultado.Token },
                    Request.Scheme);
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var resultado = await _authService.RestablecerPasswordAsync(
                modelo.Email,
                modelo.Token,
                modelo.Password);

            if (resultado.Succeeded)
            {
                TempData["MensajeExito"] = "Tu contraseña fue restablecida correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, "El enlace no es válido, expiró o ya fue utilizado.");
                break;
            }

            return View(modelo);
        }

            }

            public class ResetPasswordViewModel
            {
                [Required]
                public string Email { get; set; } = string.Empty;

                [Required]
                public string Token { get; set; } = string.Empty;

                [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
                [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
                [DataType(DataType.Password)]
                [Display(Name = "Nueva contraseña")]
                public string Password { get; set; } = string.Empty;

                [Required(ErrorMessage = "Debes confirmar la contraseña.")]
                [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
                [DataType(DataType.Password)]
                [Display(Name = "Confirmar contraseña")]
                public string ConfirmPassword { get; set; } = string.Empty;
            }
        }