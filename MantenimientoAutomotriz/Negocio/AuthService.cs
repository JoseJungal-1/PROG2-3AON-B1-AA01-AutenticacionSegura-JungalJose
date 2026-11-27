using MantenimientoAutomotriz.Data.Entities;
using MantenimientoAutomotriz.Models;
using Microsoft.AspNetCore.Identity;

namespace MantenimientoAutomotriz.Negocio
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Método para registrar un nuevo usuario con rol "Cliente" por defecto
        public async Task<IdentityResult> RegistrarUsuarioAsync(RegistroDto modelo)
        {
            var usuario = new ApplicationUser
            {
                UserName = modelo.Email,
                Email = modelo.Email,
                NombreCompleto = modelo.NombreCompleto,
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            };

            var resultado = await _userManager.CreateAsync(usuario, modelo.Password);

            if (resultado.Succeeded)
            {
                // Asigna el rol Cliente de forma controlada por la aplicación
                await _userManager.AddToRoleAsync(usuario, "Cliente");
                await _signInManager.SignInAsync(usuario, isPersistent: false);
            }

            return resultado;
        }

        // Método para iniciar sesión validando estado Activo y bloqueo por intentos fallidos
        public async Task<SignInResult> IniciarSesionAsync(string email, string password, bool recordarMe)
        {
            var usuario = await _userManager.FindByEmailAsync(email);

            // Si el usuario no existe o fue deshabilitado por el Administrador, deniega acceso
            if (usuario == null || !usuario.Activo)
            {
                return SignInResult.NotAllowed;
            }

            return await _signInManager.PasswordSignInAsync(
                usuario,
                password,
                recordarMe,
                lockoutOnFailure: true
            );
        }

        // Método para cerrar sesión
        public async Task CerrarSesionAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<(bool UsuarioEncontrado, string Email, string? Token)> SolicitarRecuperacionAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, string.Empty, null);
            }

            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null || !usuario.Activo)
            {
                return (false, string.Empty, null);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            return (true, usuario.Email ?? email, token);
        }

        public async Task<IdentityResult> RestablecerPasswordAsync(string email, string token, string nuevaPassword)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null || !usuario.Activo)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "El restablecimiento no pudo completarse."
                });
            }

            return await _userManager.ResetPasswordAsync(usuario, token, nuevaPassword);
        }
    }
}