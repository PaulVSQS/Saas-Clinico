using ClinicaSaaS.Domain.Personal;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Repositories;

/// <summary>
/// Especialización de RepositorioBase para Doctor. Necesaria porque Doctor.Horarios es una
/// colección de Entities internas del agregado (HorarioDoctor, Fase 2/6) y
/// RepositorioBase.ObtenerPorIdAsync no la carga por defecto — no puede: sirve a TODOS los
/// Aggregate Roots por igual, y no todos tienen colecciones hijas (ver comentario en
/// RepositorioBase). Sin este Include, cada ObtenerPorIdAsync desde un DbContext nuevo (un
/// circuito de Blazor Server distinto, cualquier consulta desde cero) devuelve un Doctor con
/// Horarios vacío — el bug reportado donde los horarios "desaparecían" al abrir una pestaña
/// nueva: dentro de la MISMA sesión no se notaba porque el change tracker de EF Core ya tenía
/// el Doctor con sus Horarios cargados en memoria desde el alta.
/// </summary>
public sealed class RepositorioDoctor(ClinicaSaaSDbContext contexto) : RepositorioBase<Doctor>(contexto)
{
    public override async Task<Doctor?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(d => d.Horarios).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
}
