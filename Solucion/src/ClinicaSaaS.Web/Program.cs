using ClinicaSaaS.Infrastructure.DependencyInjection;
using ClinicaSaaS.Persistence.DependencyInjection;
using ClinicaSaaS.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---- Infraestructura (Fase 4) ----
// ICurrentUserContext/ITenantContext reales (HttpUserContext), IDateTimeProvider e
// IFileStorageService. Reemplaza los stubs de "arranque" que se usaban desde Fase 3
// (DesignTimeCurrentUserContext/DesignTimeTenantContext) — esos quedan ahora reservados
// exclusivamente para ClinicaSaaSDbContextFactory (herramientas `dotnet ef`), que nunca corre
// dentro de un circuito de Blazor Server y por lo tanto no tiene HttpContext disponible.
builder.Services.AddInfrastructure();

// ---- Persistencia (Fase 3) ----
// DbContext + los 4 interceptores + IRepositorio<T>/IUnitOfWork (Fase 4).
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
