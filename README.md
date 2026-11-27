# AA01 - Autenticacion segura

Modulo de autenticacion y seguridad para Mantenimiento Automotriz, desarrollado con ASP.NET Core MVC, ASP.NET Core Identity, Entity Framework Core y SQL Server. La aplicacion organiza sus responsabilidades en tres capas logicas dentro de un solo proyecto.

## Requisitos

- .NET 10 SDK.
- SQL Server LocalDB (incluido con SQL Server Express LocalDB o Visual Studio).
- Visual Studio Community 2026 o PowerShell para ejecutar los comandos.

## Estructura

- `MantenimientoAutomotriz/Controllers/` y `MantenimientoAutomotriz/Views/`: presentacion MVC y vistas Razor.
- `MantenimientoAutomotriz/Negocio/`: servicios de autenticacion y administracion de usuarios.
- `MantenimientoAutomotriz/Data/`: contexto de Entity Framework, entidad de usuario, migraciones y seeding.
- `MantenimientoAutomotriz/Database/schema.sql`: script SQL idempotente generado desde las migraciones.

La separacion en capas es logica y se implementa dentro de un solo proyecto. Los controladores de autenticacion y administracion delegan sus operaciones a servicios de negocio. `ProfileController` usa las abstracciones de Identity (`UserManager` y `SignInManager`) para consultar y actualizar el perfil y la contrasena; los controladores no ejecutan consultas SQL directamente.

## Instalacion y ejecucion local

Abre PowerShell en la carpeta raiz del repositorio (la que contiene `MantenimientoAutomotriz.slnx`) y ejecuta:

```powershell
dotnet --version
dotnet restore .\MantenimientoAutomotriz\MantenimientoAutomotriz.csproj
dotnet build .\MantenimientoAutomotriz\MantenimientoAutomotriz.csproj
```

Configura primero las credenciales del administrador como se explica en la seccion siguiente. Luego ejecuta la aplicacion con el perfil HTTPS:

```powershell
dotnet run --project .\MantenimientoAutomotriz\MantenimientoAutomotriz.csproj --launch-profile https
```

El perfil HTTPS de `Properties/launchSettings.json` usa `https://localhost:7103` y tambien escucha en `http://localhost:5062`; el middleware redirige HTTP a HTTPS. Si el certificado local no es de confianza, confialo con `dotnet dev-certs https --trust` y reinicia el navegador.

## Base de datos SQL Server

La cadena `DefaultConnection` esta configurada para SQL Server LocalDB en `MantenimientoAutomotriz/appsettings.json`. La aplicacion ejecuta `DbInitializer.InitializeAsync` al iniciar; este aplica las migraciones pendientes y crea los roles requeridos. No es necesario crear las tablas manualmente para una ejecucion local normal.

Si necesitas aplicar el esquema desde SQL Server Management Studio (SSMS):

1. Conectate a `(localdb)\MSSQLLocalDB` usando autenticacion de Windows.
2. Abre y ejecuta `MantenimientoAutomotriz/Database/schema.sql`.

El script es idempotente y se genero a partir de las migraciones del proyecto. La base configurada se llama `aspnet-MantenimientoAutomotriz-061c8e23-9f99-4652-88fa-e259a880d839`.

## Configurar el administrador inicial sin guardar secretos en el repositorio

`MantenimientoAutomotriz.csproj` ya tiene configurado un `UserSecretsId`. En desarrollo, abre PowerShell en la raiz del repositorio y configura las claves exactas `AdminSeed:Email` y `AdminSeed:Password` con valores ficticios propios:

```powershell
dotnet user-secrets set "AdminSeed:Email" "<CORREO_ADMIN_FICTICIO>" --project .\MantenimientoAutomotriz\MantenimientoAutomotriz.csproj
dotnet user-secrets set "AdminSeed:Password" "<CONTRASENA_ADMIN_DE_PRUEBA>" --project .\MantenimientoAutomotriz\MantenimientoAutomotriz.csproj
```

Reemplaza los marcadores localmente; no uses datos personales ni una contrasena reutilizada. Debe cumplir la politica de Identity: minimo 8 caracteres, con mayuscula, minuscula, digito y simbolo. User Secrets se guarda fuera del repositorio.

En produccion, configura las variables de entorno `AdminSeed__Email` y `AdminSeed__Password` en el entorno seguro donde se ejecuta la aplicacion. Para probarlas localmente en PowerShell, establece ambas en la misma ventana antes de iniciar:

```powershell
$env:AdminSeed__Email = "<CORREO_ADMIN_FICTICIO>"
$env:AdminSeed__Password = "<CONTRASENA_ADMIN_DE_PRUEBA>"
dotnet run --project .\MantenimientoAutomotriz\MantenimientoAutomotriz.csproj --launch-profile https
```

ASP.NET Core traduce `__` a `:` en las claves de configuracion. No guardes valores reales en este README ni en archivos versionados. Si ya existe un usuario con rol `Administrador`, el inicio conserva esa cuenta y no cambia su contrasena. Si no existe ninguno y falta alguna clave, la aplicacion se detiene con un diagnostico sin crear una cuenta insegura ni revelar secretos.

## Registrar un Cliente de prueba

1. Inicia la aplicacion por HTTPS y abre `https://localhost:7103/Auth/Registro`.
2. Registra una cuenta usando un nombre, correo ficticio y una contrasena que cumpla la politica indicada.
3. La aplicacion valida los campos, exige confirmacion de contrasena y asigna automaticamente el rol `Cliente`.
4. Para verificarlo en SSMS, consulta `AspNetUsers`, `AspNetUserRoles` y `AspNetRoles`. No publiques contrasenas, hashes ni datos personales en capturas.

El inicio de sesion es `/Auth/Login`. El cierre de sesion se realiza desde el boton de navegacion. La administracion esta disponible en `/Admin` solo para cuentas con rol `Administrador`.

## Funciones y seguridad incluidas

- ASP.NET Core Identity gestiona el hash de contrasenas, los usuarios, roles y tokens.
- El registro publico crea cuentas con rol `Cliente`; el administrador inicial se crea mediante seeding.
- El usuario puede consultar su perfil y cambiar su contrasena indicando la actual.
- El administrador puede listar usuarios, asignar los roles `Cliente` y `Administrador`, y habilitar o deshabilitar cuentas.
- El ultimo administrador activo no puede deshabilitarse ni perder el rol de administrador.
- Las rutas administrativas usan `[Authorize(Roles = "Administrador")]`; las solicitudes POST usan antiforgery.
- El bloqueo se aplica despues de 3 intentos fallidos y dura 15 minutos.
- Las cookies de autenticacion son `HttpOnly`, `SameSite=Strict` y requieren HTTPS (`SecurePolicy.Always`).
- Para evitar acceso por correo no confirmado, la configuracion actual permite iniciar sesion sin confirmacion (`RequireConfirmedAccount = false`). No hay flujo de confirmacion de correo implementado.

## Recuperacion de contrasena

En desarrollo, el formulario de recuperacion genera un token de Identity y muestra un enlace simulado en pantalla para las cuentas activas. No se envia correo y el enlace solo se muestra en el entorno de desarrollo.

La integracion de un proveedor de correo para produccion no esta implementada en este proyecto. Antes de desplegarlo, se debe configurar un proveedor y entregar el enlace de forma segura, sin exponer el token en la respuesta, la consola, los registros publicos ni la interfaz.
