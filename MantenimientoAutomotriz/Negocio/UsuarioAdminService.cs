using MantenimientoAutomotriz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MantenimientoAutomotriz.Negocio
{
    /// <summary>
    /// DTO auxiliar de negocio para transportar la información de cada usuario junto con su rol actual
    /// hacia la capa de presentación sin exponer directamente la entidad de base de datos.
    /// </summary>
    public class UsuarioConRolDto
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    /// <summary>
    /// Servicio de negocio encargado de la administración de usuarios y roles.
    /// Solo debe ser invocado por controladores protegidos con [Authorize(Roles = "Administrador")].
    /// </summary>
    public class UsuarioAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuarioAdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Obtiene el listado completo de usuarios registrados junto con su rol asignado.
        /// </summary>
        public async Task<List<UsuarioConRolDto>> ObtenerTodosLosUsuariosAsync()
        {
            var usuarios = await _userManager.Users
                .OrderByDescending(u => u.FechaRegistro)
                .ToListAsync();

            var listaResultado = new List<UsuarioConRolDto>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                listaResultado.Add(new UsuarioConRolDto
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email ?? string.Empty,
                    Rol = roles.FirstOrDefault() ?? "Sin Rol",
                    Activo = usuario.Activo,
                    FechaRegistro = usuario.FechaRegistro
                });
            }

            return listaResultado;
        }

        /// <summary>
        /// Obtiene los datos de un usuario específico por su ID.
        /// </summary>
        public async Task<UsuarioConRolDto?> ObtenerUsuarioPorIdAsync(string userId)
        {
            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null) return null;

            var roles = await _userManager.GetRolesAsync(usuario);

            return new UsuarioConRolDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email ?? string.Empty,
                Rol = roles.FirstOrDefault() ?? "Cliente",
                Activo = usuario.Activo,
                FechaRegistro = usuario.FechaRegistro
            };
        }

        /// <summary>
        /// Permite al Administrador crear un nuevo usuario y asignarle un rol explícito (Administrador o Cliente).
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> CrearUsuarioConRolAsync(
            string nombreCompleto, string email, string password, string rolSeleccionado)
        {
            if (rolSeleccionado != "Administrador" && rolSeleccionado != "Cliente")
            {
                return (false, "El rol seleccionado no es válido.");
            }

            if (!await _roleManager.RoleExistsAsync(rolSeleccionado))
            {
                return (false, $"El rol '{rolSeleccionado}' no existe en el sistema.");
            }

            var existente = await _userManager.FindByEmailAsync(email);
            if (existente != null)
            {
                return (false, "Ya existe un usuario registrado con ese correo electrónico.");
            }

            var nuevoUsuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NombreCompleto = nombreCompleto,
                Activo = true,
                EmailConfirmed = true,
                FechaRegistro = DateTime.UtcNow
            };

            var resultadoCreacion = await _userManager.CreateAsync(nuevoUsuario, password);
            if (!resultadoCreacion.Succeeded)
            {
                var errores = string.Join(" ", resultadoCreacion.Errors.Select(e => e.Description));
                return (false, errores);
            }

            await _userManager.AddToRoleAsync(nuevoUsuario, rolSeleccionado);
            return (true, "Usuario creado y rol asignado correctamente.");
        }

        /// <summary>
        /// Cambia el rol de un usuario existente validando que nunca se degrade al último Administrador activo.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> CambiarRolUsuarioAsync(string userId, string nuevoRol)
        {
            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null)
                return (false, "Usuario no encontrado.");

            if (nuevoRol != "Administrador" && nuevoRol != "Cliente")
                return (false, "El rol seleccionado no es válido.");

            if (!await _roleManager.RoleExistsAsync(nuevoRol))
                return (false, "El rol seleccionado no existe.");

            var rolesActuales = await _userManager.GetRolesAsync(usuario);

            // No permite quitar el rol al último administrador activo.
            if (rolesActuales.Contains("Administrador") && nuevoRol != "Administrador")
            {
                if (await EsUltimoAdministradorActivoAsync(usuario.Id))
                {
                    return (false, "Operación denegada: no se puede quitar el rol al último Administrador activo del sistema.");
                }
            }

            await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            var resultado = await _userManager.AddToRoleAsync(usuario, nuevoRol);

            if (!resultado.Succeeded)
                return (false, "Ocurrió un error al actualizar el rol del usuario.");

            // Cambia el SecurityStamp para que Identity invalide la sesión al validarlo.
            await _userManager.UpdateSecurityStampAsync(usuario);

            return (true, $"Rol actualizado correctamente a '{nuevoRol}'.");
        }

        /// <summary>
        /// Habilita o deshabilita una cuenta de usuario.
        /// Al deshabilitarla, cambia su SecurityStamp para invalidar la sesión cuando Identity la valide.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> CambiarEstadoActivoAsync(string userId, bool nuevoEstado)
        {
            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null)
                return (false, "Usuario no encontrado.");

            // Regla de seguridad: no permitir deshabilitar al último Administrador activo
            if (!nuevoEstado && await _userManager.IsInRoleAsync(usuario, "Administrador"))
            {
                if (await EsUltimoAdministradorActivoAsync(usuario.Id))
                {
                    return (false, "Operación denegada: no se puede deshabilitar al último Administrador activo.");
                }
            }

            usuario.Activo = nuevoEstado;
            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
                return (false, "No se pudo actualizar el estado del usuario.");

            // Al desactivar la cuenta, cambia el SecurityStamp para invalidar la sesión al validarla.
            if (!nuevoEstado)
            {
                await _userManager.UpdateSecurityStampAsync(usuario);
            }

            string estadoTexto = nuevoEstado ? "habilitada" : "deshabilitada";
            return (true, $"La cuenta ha sido {estadoTexto} correctamente.");
        }

        /// <summary>
        /// Verifica si el usuario indicado es el único usuario con rol Administrador y estado Activo == true.
        /// </summary>
        private async Task<bool> EsUltimoAdministradorActivoAsync(string userIdExcluir)
        {
            var administradores = await _userManager.GetUsersInRoleAsync("Administrador");
            int otrosAdminsActivos = administradores.Count(a => a.Id != userIdExcluir && a.Activo);
            return otrosAdminsActivos == 0;
        }
    }
}