using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Persistence;
using ClinicaSaaS.Persistence.DesignTime;
using ClinicaSaaS.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using ClinicaSaaS.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---- Persistencia (Fase 3) ----
// ICurrentUserContext/ITenantContext: implementaciones "de arranque" (siempre sin usuario/
// sin tenant) hasta que la Fase 4 (Infrastructure) las reemplace leyendo el circuito real de
// Blazor Server. Registradas aquí para que la solución compile y corra end-to-end hoy mismo.
builder.Services.AddScoped<ICurrentUserContext, DesignTimeCurrentUserContext>();
builder.Services.AddScoped<ITenantContext, DesignTimeTenantContext>();

builder.Services.AddScoped<AuditableEntitySaveChangesInterceptor>();
builder.Services.AddScoped<AuditoriaSaveChangesInterceptor>();
builder.Services.AddScoped<FacturaTotalesSaveChangesInterceptor>();
builder.Services.AddScoped<TenantSessionContextConnectionInterceptor>();

builder.Services.AddDbContext<ClinicaSaaSDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly(typeof(ClinicaSaaSDbContext).Assembly.FullName));

    options.AddInterceptors(
        serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
        serviceProvider.GetRequiredService<AuditoriaSaveChangesInterceptor>(),
        serviceProvider.GetRequiredService<FacturaTotalesSaveChangesInterceptor>(),
        serviceProvider.GetRequiredService<TenantSessionContextConnectionInterceptor>());
});

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
