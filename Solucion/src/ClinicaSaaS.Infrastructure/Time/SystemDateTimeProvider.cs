using ClinicaSaaS.Application.Common.Interfaces;

namespace ClinicaSaaS.Infrastructure.Time;

/// <summary>Implementación real: delega directo en el reloj del sistema operativo, en UTC.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
