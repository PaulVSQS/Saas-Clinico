/* ============================================================================================
   SCRIPT DE CREACIÓN — SaaS Multi-Tenant para Clínicas Médicas/Odontológicas
   ============================================================================================
   Motor:            SQL Server 2019+ (compatible con SQL Server 2022 / Azure SQL)
   Cómo usarlo:      Ejecuta el script COMPLETO de una sola vez en SSMS o Azure Data Studio
                      (F5). Está ordenado para que las dependencias de llaves foráneas
                      siempre existan antes de ser referenciadas — no lo reordenes.
   Ejecución parcial: Cada sección está delimitada con un banner
                          -- ============ N. NOMBRE DE LA SECCIÓN ============
                      Puedes seleccionar desde un banner hasta el siguiente banner y
                      presionar F5 para correr SOLO esa sección (útil para depurar o
                      re-ejecutar una parte puntual). Respeta el orden numérico si vas
                      a correr secciones sueltas la primera vez.
   Idempotencia:     El script usa IF NOT EXISTS / IF OBJECT_ID en los puntos críticos
                      (BD, esquemas) para poder re-ejecutarse sin explotar si ya corriste
                      parte de él antes. Las tablas usan CREATE TABLE simple: si necesitas
                      re-crear una tabla individual, usa el bloque de la sección 12
                      (DROP opcional) — NO está pensado para producción, es solo un helper
                      de desarrollo.
   ============================================================================================ */


/* ============================================================================================
   SECCIÓN 1 — CREACIÓN DE LA BASE DE DATOS
   ============================================================================================
   Qué hace: crea la base de datos si no existe todavía. No la recrea si ya existe (para no
   borrar datos por accidente si vuelves a correr el script completo).
   ============================================================================================ */

/*IF DB_ID(N'ClinicaSaaS') IS NULL
BEGIN
    CREATE DATABASE ClinicaSaaS
    COLLATE Modern_Spanish_CI_AS;   -- collation en español, insensible a mayúsculas/acentos para búsquedas de pacientes
END
GO*/

-- A partir de aquí, todo el script corre DENTRO de esta base de datos.
USE ClinicaSaaS;
GO

-- Recomendación de nivel de aislamiento por defecto para reducir bloqueos de lectura
-- en un sistema con mucha concurrencia (recepción, doctores y facturación al mismo tiempo).
ALTER DATABASE ClinicaSaaS SET READ_COMMITTED_SNAPSHOT ON;
GO


/* ============================================================================================
   SECCIÓN 2 — CREACIÓN DE ESQUEMAS (separación física de dominios)
   ============================================================================================
   Qué hace: crea los 6 esquemas que separan físicamente datos de identidad/seguridad,
   datos personales, datos clínicos, agenda/recursos, datos financieros y auditoría.
   Un esquema por bounded context evita que un JOIN accidental mezcle, por ejemplo,
   una tabla clínica con una financiera, y permite otorgar permisos de SQL Server
   granulares por esquema más adelante (ej. el rol Auditor solo lee Billing y Audit).
   ============================================================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Security')   EXEC('CREATE SCHEMA Security');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Personal')   EXEC('CREATE SCHEMA Personal');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Clinical')   EXEC('CREATE SCHEMA Clinical');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Scheduling') EXEC('CREATE SCHEMA Scheduling');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Billing')    EXEC('CREATE SCHEMA Billing');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Audit')      EXEC('CREATE SCHEMA Audit');
GO

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identidad, tenants (clínicas), usuarios y asignación de roles por clínica',
     @level0type = N'SCHEMA', @level0name = N'Security';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Datos personales identificables: pacientes, doctores, empleados',
     @level0type = N'SCHEMA', @level0name = N'Personal';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Historial clínico versionado, archivos clínicos y procedimientos médicos',
     @level0type = N'SCHEMA', @level0name = N'Clinical';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Consultorios, equipamiento, horarios de doctores y citas',
     @level0type = N'SCHEMA', @level0name = N'Scheduling';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Facturación fiscal (DGII) e interna, detalle de factura y pagos/abonos',
     @level0type = N'SCHEMA', @level0name = N'Billing';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Bitácora de auditoría universal de todas las acciones del sistema',
     @level0type = N'SCHEMA', @level0name = N'Audit';
GO


/* ============================================================================================
   SECCIÓN 3 — ESQUEMA Security: catálogos base, tenants y usuarios
   ============================================================================================ */

-- --------------------------------------------------------------------------------------------
-- 3.1 Security.Roles — catálogo cerrado de roles del sistema (se llena con INSERT fijo, no lo
--     edita la aplicación). Los roles ligados a clínica son todos menos SuperAdminSaaS, que se
--     maneja aparte con el flag Usuarios.EsSuperAdminSaaS (ver sección 3.3).
-- --------------------------------------------------------------------------------------------
CREATE TABLE Security.Roles (
    Id      INT             NOT NULL PRIMARY KEY,
    Nombre  VARCHAR(50)     NOT NULL UNIQUE
);
GO

INSERT INTO Security.Roles (Id, Nombre) VALUES
    (1, 'AdminClinica'),
    (2, 'Recepcion'),
    (3, 'Doctor'),
    (4, 'Empleado'),
    (5, 'Auditor');
GO

-- --------------------------------------------------------------------------------------------
-- 3.2 Security.Clinicas — tabla raíz de todo el sistema multi-tenant (el "tenant").
--     Toda tabla de negocio del resto del script cuelga, directa o indirectamente, de aquí.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Security.Clinicas (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Clinicas_Id DEFAULT NEWID(),
    RazonSocial         NVARCHAR(200)   NOT NULL,
    NombreComercial     NVARCHAR(200)   NOT NULL,
    RNC                 VARCHAR(20)     NOT NULL,
    Direccion           NVARCHAR(300)   NULL,
    Telefono            VARCHAR(30)     NULL,
    Email               VARCHAR(150)    NULL,
    PlanSuscripcion     VARCHAR(50)     NOT NULL CONSTRAINT DF_Clinicas_Plan DEFAULT 'Basico',
    Activo              BIT             NOT NULL CONSTRAINT DF_Clinicas_Activo DEFAULT 1,
    FechaCreacion       DATETIME2(3)    NOT NULL CONSTRAINT DF_Clinicas_FechaCreacion DEFAULT SYSUTCDATETIME(),
    EstaEliminado       BIT             NOT NULL CONSTRAINT DF_Clinicas_EstaEliminado DEFAULT 0,
    FechaEliminacion    DATETIME2(3)    NULL,
    RowVersion          ROWVERSION,
    CONSTRAINT PK_Clinicas PRIMARY KEY (Id),
    CONSTRAINT UQ_Clinicas_RNC UNIQUE (RNC)
);
GO

-- --------------------------------------------------------------------------------------------
-- 3.3 Security.Usuarios — identidad GLOBAL (una sola cuenta de login puede pertenecer a
--     varias clínicas con distintos roles en cada una, ver 3.4). EsSuperAdminSaaS es el
--     control de plataforma completo, no ligado a ninguna clínica.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Security.Usuarios (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Usuarios_Id DEFAULT NEWID(),
    Email               VARCHAR(150)    NOT NULL,
    PasswordHash        VARBINARY(256)  NOT NULL,
    NombreCompleto      NVARCHAR(200)   NOT NULL,
    Telefono            VARCHAR(30)     NULL,
    EsSuperAdminSaaS    BIT             NOT NULL CONSTRAINT DF_Usuarios_SuperAdmin DEFAULT 0,
    Activo              BIT             NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT 1,
    FechaCreacion       DATETIME2(3)    NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT SYSUTCDATETIME(),
    EstaEliminado       BIT             NOT NULL CONSTRAINT DF_Usuarios_EstaEliminado DEFAULT 0,
    RowVersion          ROWVERSION,
    CONSTRAINT PK_Usuarios PRIMARY KEY (Id),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email)
);
GO

-- --------------------------------------------------------------------------------------------
-- 3.4 Security.UsuarioClinicaRoles — aquí vive el aislamiento de permisos: un usuario tiene
--     0..N filas, una por cada combinación (clínica, rol) que le corresponde.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Security.UsuarioClinicaRoles (
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UCR_Id DEFAULT NEWID(),
    UsuarioId               UNIQUEIDENTIFIER NOT NULL,
    ClinicaId               UNIQUEIDENTIFIER NOT NULL,
    RolId                   INT             NOT NULL,
    FechaAsignacion          DATETIME2(3)    NOT NULL CONSTRAINT DF_UCR_Fecha DEFAULT SYSUTCDATETIME(),
    AsignadoPorUsuarioId     UNIQUEIDENTIFIER NULL,
    Activo                    BIT             NOT NULL CONSTRAINT DF_UCR_Activo DEFAULT 1,
    EstaEliminado              BIT             NOT NULL CONSTRAINT DF_UCR_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_UsuarioClinicaRoles PRIMARY KEY (Id),
    CONSTRAINT FK_UCR_Usuario FOREIGN KEY (UsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT FK_UCR_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_UCR_Rol FOREIGN KEY (RolId) REFERENCES Security.Roles(Id),
    CONSTRAINT FK_UCR_AsignadoPor FOREIGN KEY (AsignadoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT UQ_UsuarioClinicaRol UNIQUE (UsuarioId, ClinicaId, RolId)
);
GO

-- Índices de apoyo a los dos patrones de consulta más comunes: "roles de este usuario"
-- y "quién tiene acceso a esta clínica".
CREATE INDEX IX_UCR_ClinicaId ON Security.UsuarioClinicaRoles(ClinicaId) INCLUDE (UsuarioId, RolId);
CREATE INDEX IX_UCR_UsuarioId ON Security.UsuarioClinicaRoles(UsuarioId);
GO


/* ============================================================================================
   SECCIÓN 4 — ESQUEMA Personal: pacientes, doctores y empleados
   ============================================================================================ */

-- --------------------------------------------------------------------------------------------
-- 4.1 Personal.Pacientes — datos personales del paciente, INDEPENDIENTES de su historial
--     clínico (que vive en el esquema Clinical). El documento es único por clínica, no
--     globalmente: dos clínicas distintas pueden tener pacientes con la misma cédula.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Personal.Pacientes (
    Id                          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Pacientes_Id DEFAULT NEWID(),
    ClinicaId                   UNIQUEIDENTIFIER NOT NULL,
    Nombres                      NVARCHAR(150)   NOT NULL,
    Apellidos                     NVARCHAR(150)   NOT NULL,
    Documento                      VARCHAR(20)     NULL,
    TipoDocumento                   VARCHAR(20)     NULL,
    FechaNacimiento                  DATE            NULL,
    Sexo                              CHAR(1)         NULL,
    Telefono                          VARCHAR(30)     NULL,
    Email                              VARCHAR(150)    NULL,
    Direccion                          NVARCHAR(300)   NULL,
    ContactoEmergenciaNombre            NVARCHAR(150)   NULL,
    ContactoEmergenciaTelefono           VARCHAR(30)     NULL,
    FechaCreacion                         DATETIME2(3)    NOT NULL CONSTRAINT DF_Pacientes_FechaCreacion DEFAULT SYSUTCDATETIME(),
    CreadoPorUsuarioId                     UNIQUEIDENTIFIER NOT NULL,
    EstaEliminado                           BIT             NOT NULL CONSTRAINT DF_Pacientes_EstaEliminado DEFAULT 0,
    FechaEliminacion                         DATETIME2(3)    NULL,
    EliminadoPorUsuarioId                     UNIQUEIDENTIFIER NULL,
    RowVersion                                 ROWVERSION,
    CONSTRAINT PK_Pacientes PRIMARY KEY (Id),
    CONSTRAINT FK_Pacientes_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_Pacientes_CreadoPor FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT FK_Pacientes_EliminadoPor FOREIGN KEY (EliminadoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT UQ_Paciente_Documento_Clinica UNIQUE (ClinicaId, Documento),
    CONSTRAINT CK_Pacientes_Sexo CHECK (Sexo IN ('M', 'F') OR Sexo IS NULL),
    CONSTRAINT CK_Pacientes_TipoDocumento CHECK (TipoDocumento IN ('Cedula', 'Pasaporte', 'ActaNacimiento') OR TipoDocumento IS NULL)
);
GO

-- Índice filtrado: SQL Server excluye del índice las filas con EstaEliminado = 1, así que
-- el 99% de las búsquedas de la UI (solo pacientes activos) usan un índice más pequeño y rápido.
CREATE INDEX IX_Pacientes_Clinica ON Personal.Pacientes(ClinicaId) WHERE EstaEliminado = 0;
CREATE INDEX IX_Pacientes_Nombre ON Personal.Pacientes(ClinicaId, Apellidos, Nombres);
GO

-- --------------------------------------------------------------------------------------------
-- 4.2 Personal.Doctores — perfil PROFESIONAL de un Usuario, específico por clínica (el mismo
--     UsuarioId puede tener un registro de Doctor en la Clínica A y otro en la Clínica B).
-- --------------------------------------------------------------------------------------------
CREATE TABLE Personal.Doctores (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Doctores_Id DEFAULT NEWID(),
    UsuarioId           UNIQUEIDENTIFIER NOT NULL,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL,
    Especialidad        NVARCHAR(150)   NOT NULL,
    NumeroExequatur     VARCHAR(50)     NULL,
    Activo              BIT             NOT NULL CONSTRAINT DF_Doctores_Activo DEFAULT 1,
    EstaEliminado       BIT             NOT NULL CONSTRAINT DF_Doctores_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_Doctores PRIMARY KEY (Id),
    CONSTRAINT FK_Doctores_Usuario FOREIGN KEY (UsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT FK_Doctores_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT UQ_Doctor_Usuario_Clinica UNIQUE (UsuarioId, ClinicaId)
);
GO

CREATE INDEX IX_Doctores_Clinica ON Personal.Doctores(ClinicaId) WHERE EstaEliminado = 0;
GO

-- --------------------------------------------------------------------------------------------
-- 4.3 Personal.Empleados — mismo patrón que Doctores, para personal no-médico (recepción,
--     contabilidad, auxiliares).
-- --------------------------------------------------------------------------------------------
CREATE TABLE Personal.Empleados (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Empleados_Id DEFAULT NEWID(),
    UsuarioId           UNIQUEIDENTIFIER NOT NULL,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL,
    Cargo               NVARCHAR(100)   NOT NULL,
    FechaIngreso        DATE            NULL,
    Activo              BIT             NOT NULL CONSTRAINT DF_Empleados_Activo DEFAULT 1,
    EstaEliminado       BIT             NOT NULL CONSTRAINT DF_Empleados_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_Empleados PRIMARY KEY (Id),
    CONSTRAINT FK_Empleados_Usuario FOREIGN KEY (UsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT FK_Empleados_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT UQ_Empleado_Usuario_Clinica UNIQUE (UsuarioId, ClinicaId)
);
GO


/* ============================================================================================
   SECCIÓN 5 — ESQUEMA Clinical (parte 1): contenedor de historial y catálogo de procedimientos
   ============================================================================================
   Nota de orden: HistorialClinicoEntradas (la parte versionada) se crea más abajo, en la
   SECCIÓN 7, porque necesita referenciar Scheduling.Citas, que todavía no existe en este punto.
   ============================================================================================ */

-- --------------------------------------------------------------------------------------------
-- 5.1 Clinical.HistorialesClinicos — contenedor raíz, exactamente 1 fila por paciente.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Clinical.HistorialesClinicos (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Historiales_Id DEFAULT NEWID(),
    PacienteId          UNIQUEIDENTIFIER NOT NULL,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL,
    FechaCreacion       DATETIME2(3)    NOT NULL CONSTRAINT DF_Historiales_FechaCreacion DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_HistorialesClinicos PRIMARY KEY (Id),
    CONSTRAINT FK_Historiales_Paciente FOREIGN KEY (PacienteId) REFERENCES Personal.Pacientes(Id),
    CONSTRAINT FK_Historiales_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT UQ_Historial_Paciente UNIQUE (PacienteId)
);
GO

-- --------------------------------------------------------------------------------------------
-- 5.2 Clinical.ProcedimientosCatalogo — catálogo de procedimientos y su precio base, por
--     clínica (cada clínica define sus propios procedimientos y precios).
-- --------------------------------------------------------------------------------------------
CREATE TABLE Clinical.ProcedimientosCatalogo (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProcCatalogo_Id DEFAULT NEWID(),
    ClinicaId           UNIQUEIDENTIFIER NOT NULL,
    Codigo              VARCHAR(30)     NOT NULL,
    Nombre              NVARCHAR(200)   NOT NULL,
    Descripcion         NVARCHAR(500)   NULL,
    PrecioBase          DECIMAL(18,2)   NOT NULL,
    Activo              BIT             NOT NULL CONSTRAINT DF_ProcCatalogo_Activo DEFAULT 1,
    EstaEliminado       BIT             NOT NULL CONSTRAINT DF_ProcCatalogo_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_ProcedimientosCatalogo PRIMARY KEY (Id),
    CONSTRAINT FK_ProcCatalogo_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT UQ_Procedimiento_Codigo_Clinica UNIQUE (ClinicaId, Codigo),
    CONSTRAINT CK_ProcCatalogo_PrecioBase CHECK (PrecioBase >= 0)
);
GO


/* ============================================================================================
   SECCIÓN 6 — ESQUEMA Scheduling: consultorios, equipamiento, horarios y citas
   ============================================================================================ */

-- --------------------------------------------------------------------------------------------
-- 6.1 Scheduling.Consultorios
-- --------------------------------------------------------------------------------------------
CREATE TABLE Scheduling.Consultorios (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Consultorios_Id DEFAULT NEWID(),
    ClinicaId       UNIQUEIDENTIFIER NOT NULL,
    Nombre          NVARCHAR(100)   NOT NULL,
    Piso            VARCHAR(20)     NULL,
    Activo          BIT             NOT NULL CONSTRAINT DF_Consultorios_Activo DEFAULT 1,
    EstaEliminado   BIT             NOT NULL CONSTRAINT DF_Consultorios_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_Consultorios PRIMARY KEY (Id),
    CONSTRAINT FK_Consultorios_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id)
);
GO

-- --------------------------------------------------------------------------------------------
-- 6.2 Scheduling.Equipamiento — ConsultorioId es NULL cuando el equipo es móvil/compartido
--     entre varios consultorios.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Scheduling.Equipamiento (
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Equipamiento_Id DEFAULT NEWID(),
    ClinicaId               UNIQUEIDENTIFIER NOT NULL,
    ConsultorioId            UNIQUEIDENTIFIER NULL,
    Nombre                    NVARCHAR(150)   NOT NULL,
    NumeroSerie                VARCHAR(100)    NULL,
    FechaAdquisicion             DATE            NULL,
    ProximoMantenimiento          DATE            NULL,
    Activo                         BIT             NOT NULL CONSTRAINT DF_Equipamiento_Activo DEFAULT 1,
    EstaEliminado                  BIT             NOT NULL CONSTRAINT DF_Equipamiento_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_Equipamiento PRIMARY KEY (Id),
    CONSTRAINT FK_Equipamiento_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_Equipamiento_Consultorio FOREIGN KEY (ConsultorioId) REFERENCES Scheduling.Consultorios(Id)
);
GO

-- --------------------------------------------------------------------------------------------
-- 6.3 Scheduling.HorariosDoctor — el horario NUNCA está embebido en Doctores. Es su propia
--     entidad para soportar horario fijo, rotativo o por rango de fechas, con consultorio
--     opcional (NULL = "rota, por definir").
-- --------------------------------------------------------------------------------------------
CREATE TABLE Scheduling.HorariosDoctor (
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Horarios_Id DEFAULT NEWID(),
    DoctorId                UNIQUEIDENTIFIER NOT NULL,
    ClinicaId               UNIQUEIDENTIFIER NOT NULL,
    ConsultorioId            UNIQUEIDENTIFIER NULL,
    TipoHorario               VARCHAR(20)     NOT NULL,
    DiaSemana                  TINYINT         NULL,
    HoraInicio                   TIME            NOT NULL,
    HoraFin                        TIME            NOT NULL,
    FechaInicioVigencia              DATE          NOT NULL,
    FechaFinVigencia                   DATE          NULL,
    Activo                               BIT             NOT NULL CONSTRAINT DF_Horarios_Activo DEFAULT 1,
    EstaEliminado                         BIT             NOT NULL CONSTRAINT DF_Horarios_EstaEliminado DEFAULT 0,
    CONSTRAINT PK_HorariosDoctor PRIMARY KEY (Id),
    CONSTRAINT FK_Horarios_Doctor FOREIGN KEY (DoctorId) REFERENCES Personal.Doctores(Id),
    CONSTRAINT FK_Horarios_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_Horarios_Consultorio FOREIGN KEY (ConsultorioId) REFERENCES Scheduling.Consultorios(Id),
    CONSTRAINT CK_Horarios_Tipo CHECK (TipoHorario IN ('Fijo', 'Rotativo', 'PorRango')),
    CONSTRAINT CK_Horarios_DiaSemana CHECK (DiaSemana BETWEEN 0 AND 6 OR DiaSemana IS NULL),
    CONSTRAINT CK_Horarios_Rango CHECK (HoraFin > HoraInicio)
);
GO

CREATE INDEX IX_HorariosDoctor_Doctor ON Scheduling.HorariosDoctor(DoctorId, FechaInicioVigencia, FechaFinVigencia);
CREATE INDEX IX_HorariosDoctor_Consultorio ON Scheduling.HorariosDoctor(ConsultorioId);
GO

-- --------------------------------------------------------------------------------------------
-- 6.4 Scheduling.Citas
-- --------------------------------------------------------------------------------------------
CREATE TABLE Scheduling.Citas (
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Citas_Id DEFAULT NEWID(),
    ClinicaId               UNIQUEIDENTIFIER NOT NULL,
    PacienteId               UNIQUEIDENTIFIER NOT NULL,
    DoctorId                  UNIQUEIDENTIFIER NOT NULL,
    ConsultorioId               UNIQUEIDENTIFIER NULL,
    FechaHoraInicio                DATETIME2(3)    NOT NULL,
    FechaHoraFin                     DATETIME2(3)    NOT NULL,
    Estado                             VARCHAR(20)     NOT NULL CONSTRAINT DF_Citas_Estado DEFAULT 'Programada',
    MotivoConsulta                       NVARCHAR(300)   NULL,
    CreadoPorUsuarioId                     UNIQUEIDENTIFIER NOT NULL,
    FechaCreacion                            DATETIME2(3)    NOT NULL CONSTRAINT DF_Citas_FechaCreacion DEFAULT SYSUTCDATETIME(),
    EstaEliminado                             BIT             NOT NULL CONSTRAINT DF_Citas_EstaEliminado DEFAULT 0,
    RowVersion                                 ROWVERSION,
    CONSTRAINT PK_Citas PRIMARY KEY (Id),
    CONSTRAINT FK_Citas_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_Citas_Paciente FOREIGN KEY (PacienteId) REFERENCES Personal.Pacientes(Id),
    CONSTRAINT FK_Citas_Doctor FOREIGN KEY (DoctorId) REFERENCES Personal.Doctores(Id),
    CONSTRAINT FK_Citas_Consultorio FOREIGN KEY (ConsultorioId) REFERENCES Scheduling.Consultorios(Id),
    CONSTRAINT FK_Citas_CreadoPor FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT CK_Citas_Estado CHECK (Estado IN ('Programada', 'Confirmada', 'EnProceso', 'Completada', 'Cancelada', 'NoAsistio')),
    CONSTRAINT CK_Citas_Rango CHECK (FechaHoraFin > FechaHoraInicio)
);
GO

CREATE INDEX IX_Citas_Doctor_Fecha ON Scheduling.Citas(DoctorId, FechaHoraInicio) WHERE EstaEliminado = 0;
CREATE INDEX IX_Citas_Paciente ON Scheduling.Citas(PacienteId) WHERE EstaEliminado = 0;
CREATE INDEX IX_Citas_Clinica_Fecha ON Scheduling.Citas(ClinicaId, FechaHoraInicio) WHERE EstaEliminado = 0;
GO
-- Nota importante: SQL Server no tiene constraints de tipo EXCLUDE (a diferencia de
-- PostgreSQL), así que la regla "un doctor no puede tener dos citas que se solapen" se
-- valida en la capa de aplicación (Domain Service) antes del SaveChanges, no aquí en la BD.


/* ============================================================================================
   SECCIÓN 7 — ESQUEMA Clinical (parte 2): historial versionado, archivos y procedimientos
   realizados. Va después de Scheduling porque HistorialClinicoEntradas referencia Citas.
   ============================================================================================ */

-- --------------------------------------------------------------------------------------------
-- 7.1 Clinical.HistorialClinicoEntradas — INSERT-ONLY. Nunca se hace UPDATE sobre una fila
--     ya creada; cada edición del historial crea una fila nueva enlazada a la anterior vía
--     EntradaAnteriorId. Esto es lo que hace el historial "versionado" a nivel de esquema.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Clinical.HistorialClinicoEntradas (
    Id                          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_HCE_Id DEFAULT NEWID(),
    HistorialClinicoId          UNIQUEIDENTIFIER NOT NULL,
    Version                      INT             NOT NULL,
    EntradaAnteriorId             UNIQUEIDENTIFIER NULL,
    DoctorId                        UNIQUEIDENTIFIER NOT NULL,
    CitaId                            UNIQUEIDENTIFIER NULL,
    MotivoConsulta                      NVARCHAR(500)   NULL,
    Diagnostico                           NVARCHAR(MAX)   NULL,
    Tratamiento                             NVARCHAR(MAX)   NULL,
    Notas                                     NVARCHAR(MAX)   NULL,
    SignosVitalesJson                           NVARCHAR(MAX)   NULL,
    FechaRegistro                                 DATETIME2(3)    NOT NULL CONSTRAINT DF_HCE_Fecha DEFAULT SYSUTCDATETIME(),
    EsVersionActual                                 BIT             NOT NULL CONSTRAINT DF_HCE_EsActual DEFAULT 1,
    CONSTRAINT PK_HistorialClinicoEntradas PRIMARY KEY (Id),
    CONSTRAINT FK_HCE_Historial FOREIGN KEY (HistorialClinicoId) REFERENCES Clinical.HistorialesClinicos(Id),
    CONSTRAINT FK_HCE_EntradaAnterior FOREIGN KEY (EntradaAnteriorId) REFERENCES Clinical.HistorialClinicoEntradas(Id),
    CONSTRAINT FK_HCE_Doctor FOREIGN KEY (DoctorId) REFERENCES Personal.Doctores(Id),
    CONSTRAINT FK_HCE_Cita FOREIGN KEY (CitaId) REFERENCES Scheduling.Citas(Id),
    CONSTRAINT UQ_Historial_Version UNIQUE (HistorialClinicoId, Version)
);
GO

CREATE INDEX IX_HCE_Historial ON Clinical.HistorialClinicoEntradas(HistorialClinicoId, EsVersionActual);
CREATE INDEX IX_HCE_Doctor ON Clinical.HistorialClinicoEntradas(DoctorId);
GO

-- --------------------------------------------------------------------------------------------
-- 7.2 Clinical.ArchivosClinicos — ligados a la ENTRADA del historial (evidencia de en qué
--     consulta se generó cada archivo), NUNCA directo al paciente, tal como pide el dominio.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Clinical.ArchivosClinicos (
    Id                              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Archivos_Id DEFAULT NEWID(),
    HistorialClinicoEntradaId       UNIQUEIDENTIFIER NOT NULL,
    TipoArchivo                      VARCHAR(30)     NOT NULL,
    NombreOriginal                     NVARCHAR(260)   NOT NULL,
    RutaAlmacenamiento                   NVARCHAR(500)   NOT NULL,
    HashSHA256                             CHAR(64)        NOT NULL,
    TamanoBytes                              BIGINT          NOT NULL,
    SubidoPorUsuarioId                         UNIQUEIDENTIFIER NOT NULL,
    FechaSubida                                  DATETIME2(3)    NOT NULL CONSTRAINT DF_Archivos_Fecha DEFAULT SYSUTCDATETIME(),
    EstaEliminado                                  BIT             NOT NULL CONSTRAINT DF_Archivos_EstaEliminado DEFAULT 0,
    FechaEliminacion                                 DATETIME2(3)    NULL,
    CONSTRAINT PK_ArchivosClinicos PRIMARY KEY (Id),
    CONSTRAINT FK_Archivos_Entrada FOREIGN KEY (HistorialClinicoEntradaId) REFERENCES Clinical.HistorialClinicoEntradas(Id),
    CONSTRAINT FK_Archivos_SubidoPor FOREIGN KEY (SubidoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT CK_Archivos_Tipo CHECK (TipoArchivo IN ('Radiografia', 'FotoIntraoral', 'PDF', 'Laboratorio', 'Otro')),
    CONSTRAINT CK_Archivos_Tamano CHECK (TamanoBytes > 0)
);
GO

CREATE INDEX IX_Archivos_Entrada ON Clinical.ArchivosClinicos(HistorialClinicoEntradaId) WHERE EstaEliminado = 0;
GO

-- --------------------------------------------------------------------------------------------
-- 7.3 Clinical.ProcedimientosRealizados — procedimientos realmente ejecutados, enlazados a
--     la entrada del historial (evidencia clínica) y al catálogo (precio de referencia).
--     PrecioAplicado es un SNAPSHOT: si el catálogo sube de precio después, este registro
--     no cambia — es evidencia de lo que se cobró en ese momento exacto.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Clinical.ProcedimientosRealizados (
    Id                              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProcReal_Id DEFAULT NEWID(),
    HistorialClinicoEntradaId       UNIQUEIDENTIFIER NOT NULL,
    ProcedimientoCatalogoId          UNIQUEIDENTIFIER NOT NULL,
    DoctorId                           UNIQUEIDENTIFIER NOT NULL,
    PiezaDental                          VARCHAR(10)     NULL,
    PrecioAplicado                         DECIMAL(18,2)   NOT NULL,
    Fecha                                    DATETIME2(3)    NOT NULL CONSTRAINT DF_ProcReal_Fecha DEFAULT SYSUTCDATETIME(),
    Notas                                      NVARCHAR(500)   NULL,
    CONSTRAINT PK_ProcedimientosRealizados PRIMARY KEY (Id),
    CONSTRAINT FK_ProcReal_Entrada FOREIGN KEY (HistorialClinicoEntradaId) REFERENCES Clinical.HistorialClinicoEntradas(Id),
    CONSTRAINT FK_ProcReal_Catalogo FOREIGN KEY (ProcedimientoCatalogoId) REFERENCES Clinical.ProcedimientosCatalogo(Id),
    CONSTRAINT FK_ProcReal_Doctor FOREIGN KEY (DoctorId) REFERENCES Personal.Doctores(Id),
    CONSTRAINT CK_ProcReal_Precio CHECK (PrecioAplicado >= 0)
);
GO

CREATE INDEX IX_ProcRealizados_Entrada ON Clinical.ProcedimientosRealizados(HistorialClinicoEntradaId);
GO


/* ============================================================================================
   SECCIÓN 8 — ESQUEMA Billing: facturación fiscal (DGII) e interna, detalle y pagos
   ============================================================================================
   Nota regulatoria: República Dominicana migra de NCF tradicional a e-CF (Comprobante Fiscal
   Electrónico) bajo la Ley 32-23, con calendario escalonado por tamaño de contribuyente.
   El modelo de abajo soporta ambos: TipoComprobante puede ser un tipo de e-CF ('E31','E32'...)
   o un NCF de papel ('B01','B02'...) mientras dure la transición.
   ============================================================================================ */

-- --------------------------------------------------------------------------------------------
-- 8.1 Billing.SecuenciasComprobante — rangos de comprobantes autorizados por la DGII, por
--     clínica y por tipo de comprobante.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Billing.SecuenciasComprobante (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Secuencias_Id DEFAULT NEWID(),
    ClinicaId           UNIQUEIDENTIFIER NOT NULL,
    TipoComprobante      VARCHAR(5)      NOT NULL,
    EsElectronico         BIT             NOT NULL CONSTRAINT DF_Secuencias_EsElectronico DEFAULT 1,
    SecuenciaActual        BIGINT          NOT NULL CONSTRAINT DF_Secuencias_Actual DEFAULT 0,
    SecuenciaDesde          BIGINT          NOT NULL,
    SecuenciaHasta           BIGINT          NOT NULL,
    FechaVencimiento          DATE            NULL,
    Activo                      BIT             NOT NULL CONSTRAINT DF_Secuencias_Activo DEFAULT 1,
    CONSTRAINT PK_SecuenciasComprobante PRIMARY KEY (Id),
    CONSTRAINT FK_Secuencias_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT UQ_Secuencia_Clinica_Tipo UNIQUE (ClinicaId, TipoComprobante),
    CONSTRAINT CK_Secuencias_Rango CHECK (SecuenciaHasta >= SecuenciaDesde)
);
GO

-- --------------------------------------------------------------------------------------------
-- 8.2 Billing.Facturas — TipoFactura 'Fiscal' se envía a la DGII; 'Interna' nunca.
--     El CHECK constraint obliga a que una factura interna nunca tenga número de comprobante.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Billing.Facturas (
    Id                          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Facturas_Id DEFAULT NEWID(),
    ClinicaId                   UNIQUEIDENTIFIER NOT NULL,
    PacienteId                    UNIQUEIDENTIFIER NOT NULL,
    TipoFactura                     VARCHAR(10)     NOT NULL,
    NumeroComprobante                 VARCHAR(20)     NULL,
    EstadoDGII                          VARCHAR(20)     NULL,
    FechaEmision                          DATETIME2(3)    NOT NULL CONSTRAINT DF_Facturas_Fecha DEFAULT SYSUTCDATETIME(),
    Subtotal                                DECIMAL(18,2)   NOT NULL,
    Descuento                                 DECIMAL(18,2)   NOT NULL CONSTRAINT DF_Facturas_Descuento DEFAULT 0,
    Impuestos                                   DECIMAL(18,2)   NOT NULL CONSTRAINT DF_Facturas_Impuestos DEFAULT 0,
    Total                                         DECIMAL(18,2)   NOT NULL,
    Estado                                          VARCHAR(20)     NOT NULL CONSTRAINT DF_Facturas_Estado DEFAULT 'Pendiente',
    EstaAnulada                                       BIT             NOT NULL CONSTRAINT DF_Facturas_Anulada DEFAULT 0,
    MotivoAnulacion                                     NVARCHAR(300)   NULL,
    CreadoPorUsuarioId                                    UNIQUEIDENTIFIER NOT NULL,
    EstaEliminado                                           BIT             NOT NULL CONSTRAINT DF_Facturas_EstaEliminado DEFAULT 0,
    RowVersion                                                ROWVERSION,
    CONSTRAINT PK_Facturas PRIMARY KEY (Id),
    CONSTRAINT FK_Facturas_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_Facturas_Paciente FOREIGN KEY (PacienteId) REFERENCES Personal.Pacientes(Id),
    CONSTRAINT FK_Facturas_CreadoPor FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT CK_Facturas_Tipo CHECK (TipoFactura IN ('Fiscal', 'Interna')),
    CONSTRAINT CK_Facturas_Estado CHECK (Estado IN ('Pendiente', 'Parcial', 'Pagada', 'Anulada')),
    CONSTRAINT CK_Facturas_EstadoDGII CHECK (EstadoDGII IN ('Pendiente', 'Enviado', 'Aceptado', 'Rechazado', 'Contingencia') OR EstadoDGII IS NULL),
    CONSTRAINT CK_Facturas_NumeroComprobante CHECK (
        (TipoFactura = 'Interna' AND NumeroComprobante IS NULL) OR (TipoFactura = 'Fiscal')
    ),
    CONSTRAINT CK_Facturas_Montos CHECK (Subtotal >= 0 AND Descuento >= 0 AND Impuestos >= 0 AND Total >= 0)
);
GO

CREATE INDEX IX_Facturas_Clinica_Fecha ON Billing.Facturas(ClinicaId, FechaEmision) WHERE EstaEliminado = 0;
CREATE INDEX IX_Facturas_Paciente ON Billing.Facturas(PacienteId) WHERE EstaEliminado = 0;
CREATE UNIQUE INDEX UQ_Facturas_Comprobante ON Billing.Facturas(ClinicaId, NumeroComprobante) WHERE NumeroComprobante IS NOT NULL;
GO

-- --------------------------------------------------------------------------------------------
-- 8.3 Billing.FacturaDetalles — Descripcion es un SNAPSHOT de texto (no depende de que el
--     catálogo mantenga el mismo nombre para siempre: una factura fiscal ya emitida no puede
--     cambiar retroactivamente).
-- --------------------------------------------------------------------------------------------
CREATE TABLE Billing.FacturaDetalles (
    Id                          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_FacturaDetalles_Id DEFAULT NEWID(),
    FacturaId                   UNIQUEIDENTIFIER NOT NULL,
    ProcedimientoRealizadoId     UNIQUEIDENTIFIER NULL,
    Descripcion                    NVARCHAR(300)   NOT NULL,
    Cantidad                         INT             NOT NULL CONSTRAINT DF_FacturaDetalles_Cantidad DEFAULT 1,
    PrecioUnitario                     DECIMAL(18,2)   NOT NULL,
    Subtotal                             DECIMAL(18,2)   NOT NULL,
    CONSTRAINT PK_FacturaDetalles PRIMARY KEY (Id),
    CONSTRAINT FK_FacturaDetalles_Factura FOREIGN KEY (FacturaId) REFERENCES Billing.Facturas(Id),
    CONSTRAINT FK_FacturaDetalles_ProcReal FOREIGN KEY (ProcedimientoRealizadoId) REFERENCES Clinical.ProcedimientosRealizados(Id),
    CONSTRAINT CK_FacturaDetalles_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_FacturaDetalles_Precio CHECK (PrecioUnitario >= 0 AND Subtotal >= 0)
);
GO

CREATE INDEX IX_FacturaDetalles_Factura ON Billing.FacturaDetalles(FacturaId);
GO

-- --------------------------------------------------------------------------------------------
-- 8.4 Billing.Pagos — abonos/pagos parciales. Un pago mal registrado se ANULA (EstaAnulado +
--     MotivoAnulacion), nunca se borra ni se edita el monto — es la única forma de que un
--     auditor reconstruya exactamente qué pasó y quién lo hizo.
-- --------------------------------------------------------------------------------------------
CREATE TABLE Billing.Pagos (
    Id                          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Pagos_Id DEFAULT NEWID(),
    FacturaId                    UNIQUEIDENTIFIER NOT NULL,
    ClinicaId                      UNIQUEIDENTIFIER NOT NULL,
    Monto                            DECIMAL(18,2)   NOT NULL,
    FechaHora                          DATETIME2(3)    NOT NULL CONSTRAINT DF_Pagos_FechaHora DEFAULT SYSUTCDATETIME(),
    MetodoPago                           VARCHAR(20)     NOT NULL,
    Referencia                             VARCHAR(100)    NULL,
    RegistradoPorUsuarioId                   UNIQUEIDENTIFIER NOT NULL,
    EstaAnulado                                BIT             NOT NULL CONSTRAINT DF_Pagos_Anulado DEFAULT 0,
    MotivoAnulacion                              NVARCHAR(300)   NULL,
    EstaEliminado                                  BIT             NOT NULL CONSTRAINT DF_Pagos_EstaEliminado DEFAULT 0,
    RowVersion                                       ROWVERSION,
    CONSTRAINT PK_Pagos PRIMARY KEY (Id),
    CONSTRAINT FK_Pagos_Factura FOREIGN KEY (FacturaId) REFERENCES Billing.Facturas(Id),
    CONSTRAINT FK_Pagos_Clinica FOREIGN KEY (ClinicaId) REFERENCES Security.Clinicas(Id),
    CONSTRAINT FK_Pagos_RegistradoPor FOREIGN KEY (RegistradoPorUsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT CK_Pagos_Metodo CHECK (MetodoPago IN ('Efectivo', 'Tarjeta', 'Transferencia', 'Cheque')),
    CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0)
);
GO

CREATE INDEX IX_Pagos_Factura ON Billing.Pagos(FacturaId) WHERE EstaEliminado = 0;
CREATE INDEX IX_Pagos_Clinica_Fecha ON Billing.Pagos(ClinicaId, FechaHora);
GO


/* ============================================================================================
   SECCIÓN 9 — ESQUEMA Audit: bitácora universal
   ============================================================================================
   Se llena desde un SaveChangesInterceptor de EF Core (no triggers de SQL) porque el
   interceptor tiene acceso al usuario, IP y dispositivo del contexto HTTP/Blazor Circuit.
   ============================================================================================ */

CREATE TABLE Audit.Auditoria (
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    ClinicaId            UNIQUEIDENTIFIER NULL,
    UsuarioId              UNIQUEIDENTIFIER NULL,
    Accion                   VARCHAR(20)     NOT NULL,
    EntidadNombre              VARCHAR(150)    NOT NULL,
    EntidadId                    VARCHAR(64)     NOT NULL,
    DatosAnteriores                 NVARCHAR(MAX)   NULL,
    DatosNuevos                       NVARCHAR(MAX)   NULL,
    FechaHora                           DATETIME2(3)    NOT NULL CONSTRAINT DF_Auditoria_FechaHora DEFAULT SYSUTCDATETIME(),
    DireccionIP                           VARCHAR(45)     NULL,
    Dispositivo                             NVARCHAR(300)   NULL,
    CONSTRAINT PK_Auditoria PRIMARY KEY (Id),
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY (UsuarioId) REFERENCES Security.Usuarios(Id),
    CONSTRAINT CK_Auditoria_Accion CHECK (Accion IN ('Create', 'Update', 'Delete', 'Login', 'Logout', 'Anular', 'Exportar'))
);
GO

CREATE INDEX IX_Auditoria_Clinica_Fecha ON Audit.Auditoria(ClinicaId, FechaHora DESC);
CREATE INDEX IX_Auditoria_Entidad ON Audit.Auditoria(EntidadNombre, EntidadId, FechaHora DESC);
CREATE INDEX IX_Auditoria_Usuario ON Audit.Auditoria(UsuarioId, FechaHora DESC);
GO
-- Recomendación a 10 años: particionar esta tabla por rango de FechaHora (PARTITION FUNCTION
-- + PARTITION SCHEME) desde el día uno — es una tabla de solo-inserción que crece sin límite.


/* ============================================================================================
   SECCIÓN 10 — LLAVES FORÁNEAS ADICIONALES DE SOFT-DELETE (EliminadoPorUsuarioId)
   ============================================================================================
   Qué hace: agrega, en las tablas donde se omitió por espacio, la columna que documenta QUIÉN
   ejecutó el soft delete — como defensa en profundidad además de la tabla Audit.Auditoria.
   ============================================================================================ */

ALTER TABLE Personal.Doctores ADD EliminadoPorUsuarioId UNIQUEIDENTIFIER NULL;
ALTER TABLE Personal.Doctores ADD CONSTRAINT FK_Doctores_EliminadoPor FOREIGN KEY (EliminadoPorUsuarioId) REFERENCES Security.Usuarios(Id);
GO

ALTER TABLE Personal.Empleados ADD EliminadoPorUsuarioId UNIQUEIDENTIFIER NULL;
ALTER TABLE Personal.Empleados ADD CONSTRAINT FK_Empleados_EliminadoPor FOREIGN KEY (EliminadoPorUsuarioId) REFERENCES Security.Usuarios(Id);
GO

ALTER TABLE Scheduling.Citas ADD EliminadoPorUsuarioId UNIQUEIDENTIFIER NULL;
ALTER TABLE Scheduling.Citas ADD CONSTRAINT FK_Citas_EliminadoPor FOREIGN KEY (EliminadoPorUsuarioId) REFERENCES Security.Usuarios(Id);
GO

ALTER TABLE Billing.Facturas ADD EliminadoPorUsuarioId UNIQUEIDENTIFIER NULL;
ALTER TABLE Billing.Facturas ADD CONSTRAINT FK_Facturas_EliminadoPor FOREIGN KEY (EliminadoPorUsuarioId) REFERENCES Security.Usuarios(Id);
GO

ALTER TABLE Billing.Pagos ADD EliminadoPorUsuarioId UNIQUEIDENTIFIER NULL;
ALTER TABLE Billing.Pagos ADD CONSTRAINT FK_Pagos_EliminadoPor FOREIGN KEY (EliminadoPorUsuarioId) REFERENCES Security.Usuarios(Id);
GO


/* ============================================================================================
   SECCIÓN 11 — VERIFICACIÓN FINAL
   ============================================================================================
   Corre esta sección al final para confirmar que todo se creó correctamente antes de
   empezar a insertar datos reales. No modifica nada, solo consulta.
   ============================================================================================ */

-- 11.1 Debe devolver 6 filas (uno por esquema de negocio)
SELECT name AS Esquema
FROM sys.schemas
WHERE name IN ('Security', 'Personal', 'Clinical', 'Scheduling', 'Billing', 'Audit')
ORDER BY name;

-- 11.2 Conteo de tablas por esquema — útil para comparar contra este script (debe dar:
-- Security=4, Personal=3, Clinical=5, Scheduling=4, Billing=4, Audit=1 → total 21 tablas)
SELECT s.name AS Esquema, COUNT(*) AS CantidadTablas
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name IN ('Security', 'Personal', 'Clinical', 'Scheduling', 'Billing', 'Audit')
GROUP BY s.name
ORDER BY s.name;

-- 11.3 Todas las llaves foráneas creadas, para auditar visualmente el grafo de relaciones
SELECT
    fk.name                                        AS NombreFK,
    OBJECT_SCHEMA_NAME(fk.parent_object_id)        AS EsquemaOrigen,
    OBJECT_NAME(fk.parent_object_id)                AS TablaOrigen,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id)      AS EsquemaDestino,
    OBJECT_NAME(fk.referenced_object_id)              AS TablaDestino
FROM sys.foreign_keys fk
ORDER BY EsquemaOrigen, TablaOrigen;

-- 11.4 Confirmar que el catálogo de roles quedó sembrado (debe devolver 5 filas)
SELECT * FROM Security.Roles ORDER BY Id;
GO


/* ============================================================================================
   SECCIÓN 12 (OPCIONAL) — RESET COMPLETO PARA DESARROLLO
   ============================================================================================
   SOLO PARA DESARROLLO LOCAL. Borra TODA la base de datos para volver a correr el script
   desde cero. Está comentado a propósito: selecciona el bloque completo y corre F5 SOLO
   si de verdad quieres destruir todos los datos. NUNCA lo actives así en un ambiente
   compartido o de producción.
   ============================================================================================

   USE master;
   GO
   ALTER DATABASE ClinicaSaaS SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   DROP DATABASE ClinicaSaaS;
   GO

   ============================================================================================ */
