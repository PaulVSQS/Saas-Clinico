using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.Domain.Personal.ValueObjects;

namespace ClinicaSaaS.UnitTests;

public class DoctorHorariosTests
{
    [Fact]
    public void AgregarHorarioEnVariosDias_DebeCrearUnHorarioPorCadaDiaConInstanciasClonadas()
    {
        // Arrange
        var doctor = Doctor.Registrar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cardiología", "EX-1234").Value;
        var bloque = BloqueHorario.Crear(new TimeOnly(8, 0), new TimeOnly(12, 0)).Value;
        var vigencia = PeriodoVigencia.Crear(new DateOnly(2026, 1, 1), null).Value;

        var dias = new Dictionary<DayOfWeek, Guid>
        {
            { DayOfWeek.Monday, Guid.NewGuid() },
            { DayOfWeek.Wednesday, Guid.NewGuid() },
            { DayOfWeek.Friday, Guid.NewGuid() }
        };

        // Act
        var resultado = doctor.AgregarHorarioEnVariosDias(dias, null, TipoHorario.Fijo, bloque, vigencia);

        // Assert
        Assert.True(resultado.EsExitoso);
        Assert.Equal(3, doctor.Horarios.Count);
        Assert.Contains(doctor.Horarios, h => h.DiaSemana == DayOfWeek.Monday);
        Assert.Contains(doctor.Horarios, h => h.DiaSemana == DayOfWeek.Wednesday);
        Assert.Contains(doctor.Horarios, h => h.DiaSemana == DayOfWeek.Friday);

        // Verificar que cada horario tiene su propia instancia de Bloque y Vigencia (Owned Types en EF Core)
        var horarios = doctor.Horarios.ToList();
        Assert.False(ReferenceEquals(horarios[0].Bloque, horarios[1].Bloque));
        Assert.False(ReferenceEquals(horarios[1].Bloque, horarios[2].Bloque));
        Assert.False(ReferenceEquals(horarios[0].Vigencia, horarios[1].Vigencia));
        Assert.False(ReferenceEquals(horarios[1].Vigencia, horarios[2].Vigencia));
    }

    [Fact]
    public void AgregarHorarioEnVariosDias_SiUnoSeSolapa_NoAgregaNinguno()
    {
        // Arrange
        var doctor = Doctor.Registrar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cardiología", "EX-1234").Value;
        var bloqueExistente = BloqueHorario.Crear(new TimeOnly(9, 0), new TimeOnly(11, 0)).Value;
        var vigencia = PeriodoVigencia.Crear(new DateOnly(2026, 1, 1), null).Value;
        doctor.AgregarHorario(Guid.NewGuid(), null, TipoHorario.Fijo, DayOfWeek.Wednesday, bloqueExistente, vigencia);

        var nuevoBloque = BloqueHorario.Crear(new TimeOnly(8, 0), new TimeOnly(12, 0)).Value;
        var dias = new Dictionary<DayOfWeek, Guid>
        {
            { DayOfWeek.Monday, Guid.NewGuid() },
            { DayOfWeek.Wednesday, Guid.NewGuid() } // Choca con el existente
        };

        // Act
        var resultado = doctor.AgregarHorarioEnVariosDias(dias, null, TipoHorario.Fijo, nuevoBloque, vigencia);

        // Assert
        Assert.True(resultado.EsFallido);
        Assert.Equal("HorarioDoctor.Solapado", resultado.Error.Codigo);
        Assert.Single(doctor.Horarios); // Solo el previo
    }

    [Fact]
    public void ReactivarHorario_SiSeSolapaConOtroActivo_Falla()
    {
        // Arrange
        var doctor = Doctor.Registrar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cardiología", "EX-1234").Value;
        var bloque1 = BloqueHorario.Crear(new TimeOnly(8, 0), new TimeOnly(12, 0)).Value;
        var vigencia = PeriodoVigencia.Crear(new DateOnly(2026, 1, 1), null).Value;
        var id1 = Guid.NewGuid();
        doctor.AgregarHorario(id1, null, TipoHorario.Fijo, DayOfWeek.Monday, bloque1, vigencia);
        doctor.DesactivarHorario(id1);

        // Agregamos otro en el mismo horario mientras el primero está inactivo
        doctor.AgregarHorario(Guid.NewGuid(), null, TipoHorario.Fijo, DayOfWeek.Monday, bloque1, vigencia);

        // Act
        var resultadoReactivar = doctor.ReactivarHorario(id1);

        // Assert
        Assert.True(resultadoReactivar.EsFallido);
        Assert.Equal("HorarioDoctor.Solapado", resultadoReactivar.Error.Codigo);
    }

    [Fact]
    public void EliminarHorario_SiEstaActivo_Falla()
    {
        // Arrange
        var doctor = Doctor.Registrar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cardiología", "EX-1234").Value;
        var bloque = BloqueHorario.Crear(new TimeOnly(8, 0), new TimeOnly(12, 0)).Value;
        var vigencia = PeriodoVigencia.Crear(new DateOnly(2026, 1, 1), null).Value;
        var id = Guid.NewGuid();
        doctor.AgregarHorario(id, null, TipoHorario.Fijo, DayOfWeek.Monday, bloque, vigencia);

        // Act
        var resultado = doctor.EliminarHorario(id);

        // Assert
        Assert.True(resultado.EsFallido);
        Assert.Equal("HorarioDoctor.DebeDesactivarsePrimero", resultado.Error.Codigo);
    }

    [Fact]
    public void EliminarHorario_SiEstaInactivo_TieneExito()
    {
        // Arrange
        var doctor = Doctor.Registrar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cardiología", "EX-1234").Value;
        var bloque = BloqueHorario.Crear(new TimeOnly(8, 0), new TimeOnly(12, 0)).Value;
        var vigencia = PeriodoVigencia.Crear(new DateOnly(2026, 1, 1), null).Value;
        var id = Guid.NewGuid();
        doctor.AgregarHorario(id, null, TipoHorario.Fijo, DayOfWeek.Monday, bloque, vigencia);
        doctor.DesactivarHorario(id);

        // Act
        var resultado = doctor.EliminarHorario(id);

        // Assert
        Assert.True(resultado.EsExitoso);
        Assert.Empty(doctor.Horarios);
    }
}
