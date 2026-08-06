using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Security;

/// <summary>
/// Aggregate Root. Es el tenant raíz de todo el sistema: prácticamente cada otro agregado del
/// dominio referencia a Clinica por Id (ClinicaId) para el aislamiento multi-tenant. No carga
/// navegación hacia pacientes/doctores/citas/etc. — eso violaría el límite de agregado (una
/// Clinica con miles de pacientes no puede ser "un objeto en memoria").
/// </summary>
public sealed class Clinica : SoftDeleteEntity, ITenantEntity
{
    // ClinicaId de sí misma: una Clinica es su propio tenant a efectos de ITenantEntity/EF Core.
    public Guid ClinicaId => Id;

    public string RazonSocial { get; private set; }
    public string NombreComercial { get; private set; }
    public Rnc Rnc { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public Email? Email { get; private set; }
    public PlanSuscripcion PlanSuscripcion { get; private set; }
    public bool Activo { get; private set; }

    private Clinica() { } // EF Core

    private Clinica(Guid id, string razonSocial, string nombreComercial, Rnc rnc) : base(id)
    {
        RazonSocial = razonSocial;
        NombreComercial = nombreComercial;
        Rnc = rnc;
        PlanSuscripcion = Enums.PlanSuscripcion.Basico;
        Activo = true;
    }

    public static Result<Clinica> Registrar(Guid id, string razonSocial, string nombreComercial, Rnc rnc)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraNulo(rnc, nameof(rnc));

        if (string.IsNullOrWhiteSpace(razonSocial))
            return new Error("Clinica.RazonSocialRequerida", "La razón social es requerida.");

        if (string.IsNullOrWhiteSpace(nombreComercial))
            return new Error("Clinica.NombreComercialRequerido", "El nombre comercial es requerido.");

        return new Clinica(id, razonSocial.Trim(), nombreComercial.Trim(), rnc);
    }

    public Result ActualizarDatosDeContacto(string? direccion, string? telefono, Email? email)
    {
        Direccion = direccion?.Trim();
        Telefono = telefono?.Trim();
        Email = email;
        return Result.Exitoso();
    }

    public Result CambiarPlan(PlanSuscripcion nuevoPlan)
    {
        if (!Activo)
            return new Error("Clinica.Inactiva", "No se puede cambiar el plan de una clínica inactiva.");

        PlanSuscripcion = nuevoPlan;
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("Clinica.YaInactiva", "La clínica ya está inactiva.");

        Activo = false;
        return Result.Exitoso();
    }

    public Result Reactivar()
    {
        if (Activo)
            return new Error("Clinica.YaActiva", "La clínica ya está activa.");

        Activo = true;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("Clinica.DebeDesactivarsePrimero", "Una clínica activa no puede eliminarse; desactívela primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
