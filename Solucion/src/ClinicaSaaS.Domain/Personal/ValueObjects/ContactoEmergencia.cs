using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal.ValueObjects;

/// <summary>
/// Value Object: nombre y teléfono siempre viajan juntos y no tienen sentido de negocio por
/// separado (un teléfono de emergencia sin nombre es inútil) — se agrupan para que no puedan
/// quedar parcialmente completos en el estado del Paciente.
/// </summary>
public sealed record ContactoEmergencia
{
    public string Nombre { get; }
    public string Telefono { get; }

    private ContactoEmergencia(string nombre, string telefono)
    {
        Nombre = nombre;
        Telefono = telefono;
    }

    public static Result<ContactoEmergencia> Crear(string nombre, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new Error("ContactoEmergencia.NombreRequerido", "El nombre del contacto de emergencia es requerido.");

        if (string.IsNullOrWhiteSpace(telefono))
            return new Error("ContactoEmergencia.TelefonoRequerido", "El teléfono del contacto de emergencia es requerido.");

        return new ContactoEmergencia(nombre.Trim(), telefono.Trim());
    }
}
