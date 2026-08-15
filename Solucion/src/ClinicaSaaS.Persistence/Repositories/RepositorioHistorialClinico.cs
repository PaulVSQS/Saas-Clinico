using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Clinical;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Repositories;

/// <summary>
/// Especialización de RepositorioBase para HistorialClinico. Necesaria por el mismo motivo que
/// RepositorioDoctor (Módulo 6): Entradas es una colección de Entities internas del agregado
/// (HistorialClinicoEntrada, Fase 2) que RepositorioBase.ObtenerPorIdAsync no carga por defecto
/// — sin Include, cada consulta nueva devolvería un historial con Entradas vacío.
/// </summary>
public sealed class RepositorioHistorialClinico(ClinicaSaaSDbContext contexto)
    : RepositorioBase<HistorialClinico>(contexto), IRepositorioHistorialClinico
{
    public override async Task<HistorialClinico?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(h => h.Entradas).FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<HistorialClinico?> ObtenerPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default) =>
        await DbSet.Include(h => h.Entradas).FirstOrDefaultAsync(h => h.PacienteId == pacienteId, cancellationToken);
}