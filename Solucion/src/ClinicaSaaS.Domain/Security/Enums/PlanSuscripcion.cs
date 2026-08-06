namespace ClinicaSaaS.Domain.Security.Enums;

/// <summary>
/// Plan de suscripción SaaS de una clínica. La BD lo modela como VARCHAR(50) abierto
/// (default 'Basico'); se cierra a enum en el dominio porque el negocio SaaS solo va a
/// vender un conjunto controlado de planes y un enum evita strings mágicos dispersos en la
/// capa de aplicación. Si el catálogo comercial de planes cambia con frecuencia, revisar esta
/// decisión (podría justificar volverlo una entidad de catálogo más adelante).
/// </summary>
public enum PlanSuscripcion
{
    Basico = 1,
    Profesional = 2,
    Empresarial = 3
}
