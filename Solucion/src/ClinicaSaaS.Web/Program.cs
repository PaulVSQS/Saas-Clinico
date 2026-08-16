using ClinicaSaaS.Application.DependencyInjection;
using ClinicaSaaS.Infrastructure.DependencyInjection;
using ClinicaSaaS.Persistence.DependencyInjection;
using ClinicaSaaS.Web;
using ClinicaSaaS.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---- Infraestructura (Fase 4) ----
// ICurrentUserContext/ITenantContext reales (HttpUserContext), IDateTimeProvider,
// IFileStorageService e IPasswordHasher (Fase 5). Reemplaza los stubs de "arranque" que se
// usaban desde Fase 3 (DesignTimeCurrentUserContext/DesignTimeTenantContext) — esos quedan
// ahora reservados exclusivamente para ClinicaSaaSDbContextFactory (herramientas `dotnet ef`),
// que nunca corre dentro de un circuito de Blazor Server y por lo tanto no tiene HttpContext
// disponible.
builder.Services.AddInfrastructure();

// ---- Persistencia (Fase 3) ----
// DbContext + los 4 interceptores + IRepositorio<T>/IUnitOfWork (Fase 4).
builder.Services.AddPersistence(builder.Configuration);

// ---- Application (Fase 5) ----
// IAutenticacionService.
builder.Services.AddApplication();

// ---- Seguridad (Fase 5) ----
// Autenticación por cookie, Policies de autorización (una por RolClinica + SuperAdminSaaS) y
// el estado de autenticación en cascada para <AuthorizeView>/<AuthorizeRouteView>.
builder.Services.AddClinicaSaaSSecurity();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Orden importante: autenticación primero (identifica quién es el usuario a partir de la
// cookie), luego autorización (decide si ese usuario puede hacer lo que está pidiendo), y solo
// después antiforgery — es el orden que exige el propio middleware pipeline de ASP.NET Core.
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoints de login/logout (Fase 5) — ver AuthEndpoints.cs sobre por qué viven fuera de Blazor.
app.MapAuthEndpoints();

// Descarga de archivos clínicos (Fase 6, Módulo 10) — mismo motivo que los de arriba: Blazor
// Server no puede empujar una descarga de archivo por el circuito SignalR.
app.MapArchivosClinicosEndpoints();

// SOLO DESARROLLO — crea un Usuario SuperAdmin de prueba si la base de datos todavía no tiene
// ninguno. Ver DevDataSeeder.cs.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await DevDataSeeder.SembrarUsuarioSuperAdminAsync(scope.ServiceProvider);
}

app.Run();