namespace ClinicaSaaS.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    Guid? UsuarioId { get; }
    string? DireccionIp { get; }
    string? Dispositivo { get; }
}
