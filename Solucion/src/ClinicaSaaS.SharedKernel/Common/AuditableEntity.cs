using ClinicaSaaS.SharedKernel.Abstractions;

namespace ClinicaSaaS.SharedKernel.Common;

/// <summary>
/// Clase base para entidades que requieren auditoría completa de creación y modificación.
/// Hereda de EntityBase y extiende con las columnas de auditoría.
/// El SaveChangesInterceptor en Persistence se encarga de poblar estas propiedades
/// automáticamente antes de cada SaveChanges, capturando el usuario del contexto HTTP/Blazor.
/// Las propiedades son de solo escritura interna para protegerlas de modificaciones externas.
/// </summary>
public abstract class AuditableEntity : EntityBase, IAuditableEntity
{
    protected AuditableEntity() { }

    protected AuditableEntity(Guid id) : base(id) { }

    public DateTime FechaCreacion { get; private set; }
    public Guid CreadoPorUsuarioId { get; private set; }
    public DateTime? FechaModificacion { get; private set; }
    public Guid? ModificadoPorUsuarioId { get; private set; }

    // Llamado exclusivamente por el interceptor de EF Core — no llamar desde dominio.
    internal void EstablecerAuditoriaCreacion(Guid usuarioId, DateTime fechaUtc)
    {
        CreadoPorUsuarioId = usuarioId;
        FechaCreacion = fechaUtc;
    }

    // Llamado exclusivamente por el interceptor de EF Core — no llamar desde dominio.
    internal void EstablecerAuditoriaModificacion(Guid usuarioId, DateTime fechaUtc)
    {
        ModificadoPorUsuarioId = usuarioId;
        FechaModificacion = fechaUtc;
    }
}
