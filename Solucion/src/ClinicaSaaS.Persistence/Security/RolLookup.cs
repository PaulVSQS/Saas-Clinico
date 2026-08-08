namespace ClinicaSaaS.Persistence.Security;

/// <summary>
/// Mapea Security.Roles. NO es una entidad de dominio: el dominio representa deliberadamente
/// los roles como el enum RolClinica (catálogo cerrado, conocido en tiempo de compilación —
/// ver justificación en Domain.Security.Enums.RolClinica). Esta clase existe únicamente para
/// que EF Core pueda materializar la tabla catálogo y la llave foránea de
/// Security.UsuarioClinicaRoles.RolId contra una fila real, y para sembrar (seed) las 5 filas
/// fijas vía HasData. Nada fuera de Persistence debe referenciar este tipo.
/// </summary>
public sealed class RolLookup
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;

    private RolLookup() { } // EF Core

    public RolLookup(int id, string nombre)
    {
        Id = id;
        Nombre = nombre;
    }
}
