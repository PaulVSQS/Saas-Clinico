using ClinicaSaaS.Domain.Scheduling.Enums;
using ClinicaSaaS.Domain.Scheduling.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Services;

/// <summary>
/// Implementación del Domain Service IValidadorDisponibilidadDoctor (Scheduling, Fase 2) — vive
/// en Persistence porque necesita consultar OTRAS instancias de Cita, algo que ningún agregado
/// puede hacer por sí solo. Usa la fórmula estándar de solapamiento de intervalos:
/// [A,B) se solapa con [C,D) si A &lt; D Y C &lt; B. Una cita Cancelada o NoAsistio no ocupa la
/// agenda del doctor — no cuenta como conflicto.
/// </summary>
public sealed class ValidadorDisponibilidadDoctor(ClinicaSaaSDbContext contexto) : IValidadorDisponibilidadDoctor
{
    public async Task<bool> EstaDisponibleAsync(
        Guid doctorId, DateTime fechaHoraInicio, DateTime fechaHoraFin, Guid? citaAExcluirId, CancellationToken cancellationToken)
    {
        var hayConflicto = await contexto.Citas
            .Where(c => c.DoctorId == doctorId)
            .Where(c => c.Estado != EstadoCita.Cancelada && c.Estado != EstadoCita.NoAsistio)
            .Where(c => citaAExcluirId == null || c.Id != citaAExcluirId)
            .AnyAsync(c => fechaHoraInicio < c.FechaHoraFin && c.FechaHoraInicio < fechaHoraFin, cancellationToken);

        return !hayConflicto;
    }
}