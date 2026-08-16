using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Clinical;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Repositories;

/// <summary>
/// Especialización de RepositorioBase para HistorialClinico. Necesaria por el mismo motivo que
/// RepositorioDoctor (Módulo 6): Entradas (HistorialClinicoEntrada, Fase 2) y, dentro de cada
/// entrada, tanto Archivos (ArchivoClinico, Módulo 10) como ProcedimientosRealizados
/// (ProcedimientoRealizado, Módulo 11) son colecciones de Entities internas del agregado que
/// RepositorioBase.ObtenerPorIdAsync no carga por defecto — sin este Include encadenado, cada
/// consulta nueva devolvería Entradas pobladas pero cada Entrada con esas dos colecciones vacías.
/// </summary>
public sealed class RepositorioHistorialClinico(ClinicaSaaSDbContext contexto)
    : RepositorioBase<HistorialClinico>(contexto), IRepositorioHistorialClinico
{
    public override async Task<HistorialClinico?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(h => h.Entradas).ThenInclude(e => e.Archivos)
            .Include(h => h.Entradas).ThenInclude(e => e.ProcedimientosRealizados)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<HistorialClinico?> ObtenerPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default) =>
        await DbSet.Include(h => h.Entradas).ThenInclude(e => e.Archivos)
            .Include(h => h.Entradas).ThenInclude(e => e.ProcedimientosRealizados)
            .FirstOrDefaultAsync(h => h.PacienteId == pacienteId, cancellationToken);
}