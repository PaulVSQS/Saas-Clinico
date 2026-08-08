using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicaSaaS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialEsquemaCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Clinical");

            migrationBuilder.EnsureSchema(
                name: "Audit");

            migrationBuilder.EnsureSchema(
                name: "Scheduling");

            migrationBuilder.EnsureSchema(
                name: "Security");

            migrationBuilder.EnsureSchema(
                name: "Personal");

            migrationBuilder.EnsureSchema(
                name: "Billing");

            migrationBuilder.CreateTable(
                name: "Auditoria",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntidadNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatosAnteriores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosNuevos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DireccionIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Dispositivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                    table.CheckConstraint("CK_Auditoria_Accion", "Accion IN ('Create','Update','Delete','Login','Logout','Anular','Exportar')");
                });

            migrationBuilder.CreateTable(
                name: "Clinicas",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreComercial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RNC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PlanSuscripcion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Consultorios",
                schema: "Scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Piso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultorios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctores",
                schema: "Personal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Especialidad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NumeroExequatur = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                schema: "Personal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaIngreso = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoFactura = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NumeroComprobante = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EstadoDGII = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstaAnulada = table.Column<bool>(type: "bit", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistorialesClinicos",
                schema: "Clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesClinicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                schema: "Personal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Documento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ContactoEmergenciaNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactoEmergenciaTelefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Sexo = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcedimientosCatalogo",
                schema: "Clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrecioBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedimientosCatalogo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecuenciasComprobante",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoComprobante = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    EsElectronico = table.Column<bool>(type: "bit", nullable: false),
                    SecuenciaActual = table.Column<long>(type: "bigint", nullable: false),
                    SecuenciaDesde = table.Column<long>(type: "bigint", nullable: false),
                    SecuenciaHasta = table.Column<long>(type: "bigint", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuenciasComprobante", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EsSuperAdminSaaS = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipamiento",
                schema: "Scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultorioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NumeroSerie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaAdquisicion = table.Column<DateOnly>(type: "date", nullable: true),
                    ProximoMantenimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipamiento_Consultorios_ConsultorioId",
                        column: x => x.ConsultorioId,
                        principalSchema: "Scheduling",
                        principalTable: "Consultorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorariosDoctor",
                schema: "Scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultorioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoHorario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: true),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false),
                    FechaInicioVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFinVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosDoctor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosDoctor_Doctores_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "Personal",
                        principalTable: "Doctores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegistradoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstaAnulado = table.Column<bool>(type: "bit", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FacturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "Billing",
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialClinicoEntradas",
                schema: "Clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EntradaAnteriorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoConsulta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Diagnostico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tratamiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignosVitalesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsVersionActual = table.Column<bool>(type: "bit", nullable: false),
                    HistorialClinicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialClinicoEntradas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialClinicoEntradas_HistorialClinicoEntradas_EntradaAnteriorId",
                        column: x => x.EntradaAnteriorId,
                        principalSchema: "Clinical",
                        principalTable: "HistorialClinicoEntradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialClinicoEntradas_HistorialesClinicos_HistorialClinicoId",
                        column: x => x.HistorialClinicoId,
                        principalSchema: "Clinical",
                        principalTable: "HistorialesClinicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                schema: "Scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultorioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaHoraInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoConsulta = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citas_Consultorios_ConsultorioId",
                        column: x => x.ConsultorioId,
                        principalSchema: "Scheduling",
                        principalTable: "Consultorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Doctores_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "Personal",
                        principalTable: "Doctores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalSchema: "Personal",
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClinicaRoles",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AsignadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClinicaRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioClinicaRoles_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalSchema: "Security",
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioClinicaRoles_Usuarios_AsignadoPorUsuarioId",
                        column: x => x.AsignadoPorUsuarioId,
                        principalSchema: "Security",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioClinicaRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "Security",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchivosClinicos",
                schema: "Clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoArchivo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RutaAlmacenamiento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HashSHA256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    SubidoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstaEliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HistorialClinicoEntradaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosClinicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivosClinicos_HistorialClinicoEntradas_HistorialClinicoEntradaId",
                        column: x => x.HistorialClinicoEntradaId,
                        principalSchema: "Clinical",
                        principalTable: "HistorialClinicoEntradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcedimientosRealizados",
                schema: "Clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcedimientoCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PiezaDental = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PrecioAplicado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HistorialClinicoEntradaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedimientosRealizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcedimientosRealizados_HistorialClinicoEntradas_HistorialClinicoEntradaId",
                        column: x => x.HistorialClinicoEntradaId,
                        principalSchema: "Clinical",
                        principalTable: "HistorialClinicoEntradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcedimientosRealizados_ProcedimientosCatalogo_ProcedimientoCatalogoId",
                        column: x => x.ProcedimientoCatalogoId,
                        principalSchema: "Clinical",
                        principalTable: "ProcedimientosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacturaDetalles",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcedimientoRealizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "Billing",
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_ProcedimientosRealizados_ProcedimientoRealizadoId",
                        column: x => x.ProcedimientoRealizadoId,
                        principalSchema: "Clinical",
                        principalTable: "ProcedimientosRealizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Security",
                table: "Roles",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "AdminClinica" },
                    { 2, "Recepcion" },
                    { 3, "Doctor" },
                    { 4, "Empleado" },
                    { 5, "Auditor" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosClinicos_HistorialClinicoEntradaId",
                schema: "Clinical",
                table: "ArchivosClinicos",
                column: "HistorialClinicoEntradaId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_ClinicaId_FechaHora",
                schema: "Audit",
                table: "Auditoria",
                columns: new[] { "ClinicaId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_EntidadNombre_EntidadId",
                schema: "Audit",
                table: "Auditoria",
                columns: new[] { "EntidadNombre", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioId",
                schema: "Audit",
                table: "Auditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ClinicaId_FechaHoraInicio",
                schema: "Scheduling",
                table: "Citas",
                columns: new[] { "ClinicaId", "FechaHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ConsultorioId",
                schema: "Scheduling",
                table: "Citas",
                column: "ConsultorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_DoctorId_FechaHoraInicio",
                schema: "Scheduling",
                table: "Citas",
                columns: new[] { "DoctorId", "FechaHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PacienteId",
                schema: "Scheduling",
                table: "Citas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinicas_RNC",
                schema: "Security",
                table: "Clinicas",
                column: "RNC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctores_UsuarioId_ClinicaId",
                schema: "Personal",
                table: "Doctores",
                columns: new[] { "UsuarioId", "ClinicaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_UsuarioId_ClinicaId",
                schema: "Personal",
                table: "Empleados",
                columns: new[] { "UsuarioId", "ClinicaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipamiento_ConsultorioId",
                schema: "Scheduling",
                table: "Equipamiento",
                column: "ConsultorioId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_FacturaId",
                schema: "Billing",
                table: "FacturaDetalles",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_ProcedimientoRealizadoId",
                schema: "Billing",
                table: "FacturaDetalles",
                column: "ProcedimientoRealizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_ClinicaId_FechaEmision",
                schema: "Billing",
                table: "Facturas",
                columns: new[] { "ClinicaId", "FechaEmision" });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_ClinicaId_NumeroComprobante",
                schema: "Billing",
                table: "Facturas",
                columns: new[] { "ClinicaId", "NumeroComprobante" },
                unique: true,
                filter: "[NumeroComprobante] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_PacienteId",
                schema: "Billing",
                table: "Facturas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinicoEntradas_DoctorId",
                schema: "Clinical",
                table: "HistorialClinicoEntradas",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinicoEntradas_EntradaAnteriorId",
                schema: "Clinical",
                table: "HistorialClinicoEntradas",
                column: "EntradaAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinicoEntradas_HistorialClinicoId_EsVersionActual",
                schema: "Clinical",
                table: "HistorialClinicoEntradas",
                columns: new[] { "HistorialClinicoId", "EsVersionActual" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinicoEntradas_HistorialClinicoId_Version",
                schema: "Clinical",
                table: "HistorialClinicoEntradas",
                columns: new[] { "HistorialClinicoId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesClinicos_PacienteId",
                schema: "Clinical",
                table: "HistorialesClinicos",
                column: "PacienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosDoctor_ConsultorioId",
                schema: "Scheduling",
                table: "HorariosDoctor",
                column: "ConsultorioId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosDoctor_DoctorId",
                schema: "Scheduling",
                table: "HorariosDoctor",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_ClinicaId_Apellidos_Nombres",
                schema: "Personal",
                table: "Pacientes",
                columns: new[] { "ClinicaId", "Apellidos", "Nombres" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_FacturaId",
                schema: "Billing",
                table: "Pagos",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_FechaHora",
                schema: "Billing",
                table: "Pagos",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedimientosCatalogo_ClinicaId_Codigo",
                schema: "Clinical",
                table: "ProcedimientosCatalogo",
                columns: new[] { "ClinicaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcedimientosRealizados_HistorialClinicoEntradaId",
                schema: "Clinical",
                table: "ProcedimientosRealizados",
                column: "HistorialClinicoEntradaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedimientosRealizados_ProcedimientoCatalogoId",
                schema: "Clinical",
                table: "ProcedimientosRealizados",
                column: "ProcedimientoCatalogoId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                schema: "Security",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecuenciasComprobante_ClinicaId_TipoComprobante",
                schema: "Billing",
                table: "SecuenciasComprobante",
                columns: new[] { "ClinicaId", "TipoComprobante" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClinicaRoles_AsignadoPorUsuarioId",
                schema: "Security",
                table: "UsuarioClinicaRoles",
                column: "AsignadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClinicaRoles_ClinicaId",
                schema: "Security",
                table: "UsuarioClinicaRoles",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClinicaRoles_UsuarioId",
                schema: "Security",
                table: "UsuarioClinicaRoles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClinicaRoles_UsuarioId_ClinicaId_RolId",
                schema: "Security",
                table: "UsuarioClinicaRoles",
                columns: new[] { "UsuarioId", "ClinicaId", "RolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                schema: "Security",
                table: "Usuarios",
                column: "Email",
                unique: true);

migrationBuilder.Sql("CREATE UNIQUE INDEX UQ_Pacientes_Documento_Clinica ON Personal.Pacientes (ClinicaId, Documento) WHERE Documento IS NOT NULL;");

            migrationBuilder.Sql("ALTER TABLE Security.UsuarioClinicaRoles ADD CONSTRAINT FK_UCR_Rol FOREIGN KEY (RolId) REFERENCES Security.Roles(Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivosClinicos",
                schema: "Clinical");

            migrationBuilder.DropTable(
                name: "Auditoria",
                schema: "Audit");

            migrationBuilder.DropTable(
                name: "Citas",
                schema: "Scheduling");

            migrationBuilder.DropTable(
                name: "Empleados",
                schema: "Personal");

            migrationBuilder.DropTable(
                name: "Equipamiento",
                schema: "Scheduling");

            migrationBuilder.DropTable(
                name: "FacturaDetalles",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "HorariosDoctor",
                schema: "Scheduling");

            migrationBuilder.DropTable(
                name: "Pagos",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "SecuenciasComprobante",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "UsuarioClinicaRoles",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "Pacientes",
                schema: "Personal");

            migrationBuilder.DropTable(
                name: "Consultorios",
                schema: "Scheduling");

            migrationBuilder.DropTable(
                name: "ProcedimientosRealizados",
                schema: "Clinical");

            migrationBuilder.DropTable(
                name: "Doctores",
                schema: "Personal");

            migrationBuilder.DropTable(
                name: "Facturas",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "Clinicas",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "HistorialClinicoEntradas",
                schema: "Clinical");

            migrationBuilder.DropTable(
                name: "ProcedimientosCatalogo",
                schema: "Clinical");

            migrationBuilder.DropTable(
                name: "HistorialesClinicos",
                schema: "Clinical");
        }
    }
}
