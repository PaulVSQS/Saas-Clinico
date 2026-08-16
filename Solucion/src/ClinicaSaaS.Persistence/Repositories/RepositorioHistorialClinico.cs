using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Clinical;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Repositories;

/// <summary>
/// Especialización de RepositorioBase para HistorialClinico. Necesaria por el mismo motivo que
/// RepositorioDoctor (Módulo 6): tanto Entradas (HistorialClinicoEntrada, Fase 2) como, dentro
/// de cada entrada, Archivos (ArchivoClinico, Fase 2) son colecciones de Entities internas del
/// agregado que RepositorioBase.ObtenerPorIdAsync no carga por defecto — sin este Include
/// encadenado, cada consulta nueva devolvería un historial con Entradas pobladas pero cada
/// Entrada con Archivos vacío (Módulo 10 de Fase 6, extiende el Include que Módulo 9 dejó
/// limitado a Entradas nada más).
/// </summary>
public sealed class RepositorioHistorialClinico(ClinicaSaaSDbContext contexto)
    : RepositorioBase<HistorialClinico>(contexto), IRepositorioHistorialClinico
{
    public override async Task<HistorialClinico?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(h => h.Entradas).ThenInclude(e => e.Archivos)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<HistorialClinico?> ObtenerPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default) =>
        await DbSet.Include(h => h.Entradas).ThenInclude(e => e.Archivos)
            .FirstOrDefaultAsync(h => h.PacienteId == pacienteId, cancellationToken);
}