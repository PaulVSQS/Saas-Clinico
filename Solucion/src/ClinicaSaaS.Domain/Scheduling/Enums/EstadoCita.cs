namespace ClinicaSaaS.Domain.Scheduling.Enums;

/// <summary>
/// Estados del ciclo de vida de una Cita. Modelado como enum (no strings) para que las
/// transiciones válidas se puedan hacer cumplir en código (ver Cita.cs) en vez de depender de
/// que la capa de aplicación nunca escriba un string incorrecto.
/// </summary>
public enum EstadoCita
{
    Programada = 1,
    Confirmada = 2,
    EnProceso = 3,
    Completada = 4,
    Cancelada = 5,
    NoAsistio = 6
}
