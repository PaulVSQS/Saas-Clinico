namespace ClinicaSaaS.Persistence.Audit;

/// <summary>
/// Mapea Audit.Auditoria. NO es una entidad de dominio (no vive en ClinicaSaaS.Domain):
/// es una fila de bitácora insert-only, sin invariantes de negocio ni comportamiento —
/// generarla como Aggregate Root del dominio solo para satisfacer a EF Core sería forzar
/// un concepto de infraestructura dentro del núcleo de negocio. Solo el
/// AuditoriaSaveChangesInterceptor la crea; nada más en la solución debe instanciarla.
/// </summary>
public sealed class AuditLogEntry
{
    public long Id { get; private set; }
    public Guid? ClinicaId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Accion { get; private set; } = null!;
    public string EntidadNombre { get; private set; } = null!;
    public string EntidadId { get; private set; } = null!;
    public string? DatosAnteriores { get; private set; }
    public string? DatosNuevos { get; private set; }
    public DateTime FechaHora { get; private set; }
    public string? DireccionIp { get; private set; }
    public string? Dispositivo { get; private set; }

    private AuditLogEntry() { } // EF Core

    public AuditLogEntry(
        Guid? clinicaId, Guid? usuarioId, string accion, string entidadNombre, string entidadId,
        string? datosAnteriores, string? datosNuevos, DateTime fechaUtc, string? direccionIp, string? dispositivo)
    {
        ClinicaId = clinicaId;
        UsuarioId = usuarioId;
        Accion = accion;
        EntidadNombre = entidadNombre;
        EntidadId = entidadId;
        DatosAnteriores = datosAnteriores;
        DatosNuevos = datosNuevos;
        FechaHora = fechaUtc;
        DireccionIp = direccionIp;
        Dispositivo = dispositivo;
    }
}
