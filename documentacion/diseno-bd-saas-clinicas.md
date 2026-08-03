# Diseño de Base de Datos — SaaS Multi-Tenant para Clínicas Médicas/Odontológicas

**Stack objetivo:** .NET 9 · ASP.NET Core · EF Core 9 · SQL Server · Blazor Server · MudBlazor · Clean Architecture / DDD
**Alcance de este documento:** exclusivamente modelo de datos (SQL Server) — sin UI, sin endpoints, sin autenticación.

---

## 1. Decisiones arquitectónicas de base (con justificación)

| Decisión | Elección | Justificación |
|---|---|---|
| Estrategia multi-tenant | **Base de datos compartida, esquema compartido, discriminador `ClinicaId`** (según lo solicitado) | Menor costo operativo que BD-por-tenant a la escala inicial; permite reporting cross-tenant para el SuperAdmin sin federar consultas. Se compensa el riesgo de fuga de datos entre clínicas con **Global Query Filters de EF Core + Row-Level Security (RLS) nativa de SQL Server** como defensa en profundidad (doble candado: aplicación y motor de base de datos). |
| Separación de dominios | **Esquemas de SQL Server** (`Security`, `Personal`, `Clinical`, `Scheduling`, `Billing`, `Audit`) en vez de un solo `dbo` | Un esquema por bounded context hace físicamente imposible un JOIN accidental que mezcle datos clínicos y financieros en una vista, y permite otorgar permisos de SQL granulares (p. ej. el rol `Auditor` solo tiene `SELECT` en `Billing` y `Audit`, nunca en `Clinical`). |
| Claves primarias | **`UNIQUEIDENTIFIER` (GUID v7 recomendado si el driver lo soporta, si no `NEWID()`)** en casi todas las tablas de negocio; `BIGINT IDENTITY` solo en `Audit.Auditoria` (tabla de solo-inserción de altísimo volumen) | GUIDs evitan que un usuario adivine IDs secuenciales de pacientes/facturas de otra clínica (seguridad por oscuridad adicional) y simplifican la generación de IDs en el cliente/Blazor Server antes del `SaveChanges`. `BIGINT` en auditoría por rendimiento de índices ante millones de filas. |
| Borrado | **Soft delete universal** vía `EstaEliminado BIT`, `FechaEliminacion`, `EliminadoPorUsuarioId` + Global Query Filter en EF Core | Cumple el requisito de "nada se elimina físicamente" y mantiene la integridad referencial histórica (una factura nunca pierde su paciente aunque el paciente sea "eliminado"). |
| Historial clínico | **Append-only / versionado, nunca UPDATE sobre una entrada ya creada** | Un expediente clínico alterado retroactivamente sin dejar rastro es inaceptable legal y médicamente. Cada cambio es una fila nueva enlazada a la anterior. |
| Archivos clínicos | **Relacionados a la versión del historial clínico, no al paciente** | Cumple el requisito explícito; además da trazabilidad exacta de en qué consulta/versión se generó cada imagen o PDF. |
| Auditoría | **Tabla central única `Audit.Auditoria`** poblada por interceptor de `SaveChanges` de EF Core (no triggers de SQL) | Un interceptor tiene acceso al `UsuarioId`, IP y dispositivo del contexto HTTP/Blazor Circuit, algo que un trigger de SQL no puede conocer. Los triggers quedan como opción complementaria solo si se necesita blindaje contra escritura directa a la BD fuera de la app. |
| Moneda | `DECIMAL(18,2)` en todos los montos, nunca `FLOAT`/`MONEY` puro sin escala explícita | Precisión exacta obligatoria en sistemas financieros; `MONEY` de SQL Server tiene limitaciones de redondeo conocidas. |
| Concurrencia | `ROWVERSION` (`Timestamp`) en tablas financieras y de historial clínico | Evita que dos abonos simultáneos o dos ediciones de historial se pisen silenciosamente; EF Core lo mapea nativamente como token de concurrencia optimista. |

---

## 2. Esquema `Security` — Identidad, tenants y permisos

```sql
CREATE SCHEMA Security;
GO

-- Tenants
CREATE TABLE Security.Clinicas (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    RazonSocial         NVARCHAR(200)   NOT NULL,
    NombreComercial     NVARCHAR(200)   NOT NULL,
    RNC                 VARCHAR(20)     NOT NULL,          -- identificación fiscal DGII
    Direccion           NVARCHAR(300)   NULL,
    Telefono            VARCHAR(30)     NULL,
    Email               VARCHAR(150)    NULL,
    PlanSuscripcion     VARCHAR(50)     NOT NULL DEFAULT 'Basico', -- gestión SaaS
    Activo              BIT             NOT NULL DEFAULT 1,
    FechaCreacion       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    FechaEliminacion    DATETIME2(3)    NULL,
    RowVersion          ROWVERSION,
    CONSTRAINT UQ_Clinicas_RNC UNIQUE (RNC)
);

-- Usuario global (una sola cuenta de login puede pertenecer a varias clínicas)
CREATE TABLE Security.Usuarios (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    Email               VARCHAR(150)    NOT NULL,
    PasswordHash        VARBINARY(256)  NOT NULL,          -- gestión real de hashing fuera de alcance (no auth)
    NombreCompleto      NVARCHAR(200)   NOT NULL,
    Telefono            VARCHAR(30)     NULL,
    EsSuperAdminSaaS    BIT             NOT NULL DEFAULT 0, -- control global, no ligado a clínica
    Activo              BIT             NOT NULL DEFAULT 1,
    FechaCreacion       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    RowVersion          ROWVERSION,
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email)
);

-- Catálogo cerrado de roles (Admin de Clínica, Secretaria, Doctor, Empleado, Auditor)
CREATE TABLE Security.Roles (
    Id      INT             NOT NULL PRIMARY KEY,
    Nombre  VARCHAR(50)     NOT NULL UNIQUE     -- 'AdminClinica','Recepcion','Doctor','Empleado','Auditor'
);

-- Asignación de rol POR CLÍNICA — aquí vive el aislamiento de permisos
CREATE TABLE Security.UsuarioClinicaRoles (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UsuarioId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    RolId               INT             NOT NULL REFERENCES Security.Roles(Id),
    FechaAsignacion     DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    AsignadoPorUsuarioId UNIQUEIDENTIFIER NULL REFERENCES Security.Usuarios(Id),
    Activo              BIT             NOT NULL DEFAULT 1,
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    CONSTRAINT UQ_UsuarioClinicaRol UNIQUE (UsuarioId, ClinicaId, RolId)
);
CREATE INDEX IX_UsuarioClinicaRoles_ClinicaId ON Security.UsuarioClinicaRoles(ClinicaId) INCLUDE (UsuarioId, RolId);
CREATE INDEX IX_UsuarioClinicaRoles_UsuarioId ON Security.UsuarioClinicaRoles(UsuarioId);
```

**Por qué `Usuarios` es global y no está duplicado por clínica:** un doctor que trabaja en dos clínicas (caso real y explícitamente permitido en el dominio) debe iniciar sesión una sola vez y ver un selector de clínica activa. Duplicar usuarios por clínica obligaría a contraseñas distintas y rompería el requisito "un usuario puede tener varios roles, ligados a una clínica específica".

**Por qué `EsSuperAdminSaaS` es un flag en `Usuarios` y no un rol en `UsuarioClinicaRoles`:** el SuperAdmin no pertenece a ninguna clínica — controla la plataforma completa. Modelarlo como rol-por-clínica violaría su propia naturaleza (tendría que "pertenecer" artificialmente a una clínica ficticia).

---

## 3. Esquema `Personal` — Datos personales identificables

```sql
CREATE SCHEMA Personal;
GO

CREATE TABLE Personal.Pacientes (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    Nombres             NVARCHAR(150)   NOT NULL,
    Apellidos           NVARCHAR(150)   NOT NULL,
    Documento           VARCHAR(20)     NULL,           -- cédula/pasaporte
    TipoDocumento       VARCHAR(20)     NULL,           -- 'Cedula','Pasaporte','ActaNacimiento' (menores)
    FechaNacimiento     DATE            NULL,
    Sexo                CHAR(1)         NULL,           -- 'M','F'
    Telefono            VARCHAR(30)     NULL,
    Email               VARCHAR(150)    NULL,
    Direccion           NVARCHAR(300)   NULL,
    ContactoEmergenciaNombre    NVARCHAR(150) NULL,
    ContactoEmergenciaTelefono  VARCHAR(30)   NULL,
    FechaCreacion       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CreadoPorUsuarioId  UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    FechaEliminacion    DATETIME2(3)    NULL,
    RowVersion          ROWVERSION,
    CONSTRAINT UQ_Paciente_Documento_Clinica UNIQUE (ClinicaId, Documento)
);
CREATE INDEX IX_Pacientes_Clinica ON Personal.Pacientes(ClinicaId) WHERE EstaEliminado = 0;
CREATE INDEX IX_Pacientes_Nombre ON Personal.Pacientes(ClinicaId, Apellidos, Nombres);

-- Perfil profesional del doctor, ligado 1:1 a un Usuario, pero con datos por clínica
CREATE TABLE Personal.Doctores (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UsuarioId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    Especialidad        NVARCHAR(150)   NOT NULL,
    NumeroExequatur     VARCHAR(50)     NULL,           -- licencia médica nacional
    Activo              BIT             NOT NULL DEFAULT 1,
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Doctor_Usuario_Clinica UNIQUE (UsuarioId, ClinicaId)
);
CREATE INDEX IX_Doctores_Clinica ON Personal.Doctores(ClinicaId) WHERE EstaEliminado = 0;

CREATE TABLE Personal.Empleados (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UsuarioId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    Cargo               NVARCHAR(100)   NOT NULL,       -- 'Recepcion','Contabilidad','Auxiliar', etc.
    FechaIngreso        DATE            NULL,
    Activo              BIT             NOT NULL DEFAULT 1,
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Empleado_Usuario_Clinica UNIQUE (UsuarioId, ClinicaId)
);
```

**Por qué el nombre del paciente vive aquí y NO en `Clinical`:** el requisito es explícito ("un paciente tiene datos personales independientes de su historial clínico"). Esto también facilita cumplir eventuales solicitudes de portabilidad/rectificación de datos personales (tipo GDPR/Ley 172-13 de RD sobre protección de datos) sin tocar el expediente médico.

**Doctor/Empleado como tabla separada de `Usuarios`:** `Usuarios` es autenticación e identidad; `Doctores`/`Empleados` es *perfil profesional por clínica*. Esto permite que el mismo `UsuarioId` tenga un registro de `Doctores` en la Clínica A y un registro de `Empleados` en la Clínica B (caso real: un odontólogo que además administra otra sucursal).

---

## 4. Esquema `Clinical` — Historial clínico, procedimientos y archivos

```sql
CREATE SCHEMA Clinical;
GO

-- Contenedor raíz: 1 por paciente
CREATE TABLE Clinical.HistorialesClinicos (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    PacienteId          UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Pacientes(Id),
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    FechaCreacion       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Historial_Paciente UNIQUE (PacienteId)
);

-- Entradas versionadas — INSERT-ONLY, jamás UPDATE
CREATE TABLE Clinical.HistorialClinicoEntradas (
    Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    HistorialClinicoId      UNIQUEIDENTIFIER NOT NULL REFERENCES Clinical.HistorialesClinicos(Id),
    Version                 INT             NOT NULL,           -- 1,2,3... incremental por historial
    EntradaAnteriorId       UNIQUEIDENTIFIER NULL REFERENCES Clinical.HistorialClinicoEntradas(Id),
    DoctorId                UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Doctores(Id),
    CitaId                  UNIQUEIDENTIFIER NULL,             -- FK lógica a Scheduling.Citas (ver más abajo)
    MotivoConsulta          NVARCHAR(500)   NULL,
    Diagnostico             NVARCHAR(MAX)   NULL,
    Tratamiento              NVARCHAR(MAX)   NULL,
    Notas                   NVARCHAR(MAX)   NULL,
    SignosVitalesJson        NVARCHAR(MAX)   NULL,               -- estructura flexible (presión, temp, etc.)
    FechaRegistro            DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    EsVersionActual          BIT             NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Historial_Version UNIQUE (HistorialClinicoId, Version)
);
CREATE INDEX IX_HistorialEntradas_Historial ON Clinical.HistorialClinicoEntradas(HistorialClinicoId, EsVersionActual);
CREATE INDEX IX_HistorialEntradas_Doctor ON Clinical.HistorialClinicoEntradas(DoctorId);

-- Archivos clínicos — ligados a la ENTRADA del historial, nunca directo al paciente
CREATE TABLE Clinical.ArchivosClinicos (
    Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    HistorialClinicoEntradaId UNIQUEIDENTIFIER NOT NULL REFERENCES Clinical.HistorialClinicoEntradas(Id),
    TipoArchivo             VARCHAR(30)     NOT NULL,           -- 'Radiografia','FotoIntraoral','PDF','Laboratorio'
    NombreOriginal          NVARCHAR(260)   NOT NULL,
    RutaAlmacenamiento      NVARCHAR(500)   NOT NULL,           -- ruta en blob storage (Azure Blob/S3), no el binario
    HashSHA256              CHAR(64)        NOT NULL,           -- integridad, evidencia de no-alteración
    TamanoBytes             BIGINT          NOT NULL,
    SubidoPorUsuarioId      UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    FechaSubida              DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    EstaEliminado            BIT             NOT NULL DEFAULT 0,
    FechaEliminacion         DATETIME2(3)    NULL
);
CREATE INDEX IX_ArchivosClinicos_Entrada ON Clinical.ArchivosClinicos(HistorialClinicoEntradaId) WHERE EstaEliminado = 0;

-- Catálogo de procedimientos (precio base), por clínica
CREATE TABLE Clinical.ProcedimientosCatalogo (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    Codigo              VARCHAR(30)     NOT NULL,               -- código interno de la clínica
    Nombre              NVARCHAR(200)   NOT NULL,
    Descripcion         NVARCHAR(500)   NULL,
    PrecioBase          DECIMAL(18,2)   NOT NULL,
    Activo              BIT             NOT NULL DEFAULT 1,
    EstaEliminado       BIT             NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Procedimiento_Codigo_Clinica UNIQUE (ClinicaId, Codigo)
);

-- Procedimientos realmente ejecutados, enlazados a la entrada del historial (evidencia clínica)
CREATE TABLE Clinical.ProcedimientosRealizados (
    Id                          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    HistorialClinicoEntradaId   UNIQUEIDENTIFIER NOT NULL REFERENCES Clinical.HistorialClinicoEntradas(Id),
    ProcedimientoCatalogoId     UNIQUEIDENTIFIER NOT NULL REFERENCES Clinical.ProcedimientosCatalogo(Id),
    DoctorId                    UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Doctores(Id),
    PiezaDental                 VARCHAR(10)     NULL,           -- notación FDI, aplica solo a odontología
    PrecioAplicado               DECIMAL(18,2)   NOT NULL,       -- snapshot del precio al momento (puede diferir del catálogo)
    Fecha                        DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    Notas                        NVARCHAR(500)   NULL
);
CREATE INDEX IX_ProcRealizados_Entrada ON Clinical.ProcedimientosRealizados(HistorialClinicoEntradaId);
```

**Por qué `PrecioAplicado` se copia en vez de referenciar siempre el precio del catálogo:** si el precio base sube el mes próximo, una factura o procedimiento de hace 6 meses no puede recalcularse retroactivamente — es evidencia legal/financiera de un momento específico (snapshot pattern).

**Por qué `CitaId` en `HistorialClinicoEntradas` es una FK "lógica" sin `REFERENCES`:** `Scheduling.Citas` se define en la sección siguiente; en SQL real se agrega la constraint después de crear ambos esquemas para evitar dependencia circular en el orden de creación. En EF Core esto no es problema (el `DbContext` resuelve el grafo completo).

---

## 5. Esquema `Scheduling` — Consultorios, equipamiento, horarios y citas

```sql
CREATE SCHEMA Scheduling;
GO

CREATE TABLE Scheduling.Consultorios (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId   UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    Nombre      NVARCHAR(100)   NOT NULL,          -- 'Consultorio 1', 'Sala de Rayos X'
    Piso        VARCHAR(20)     NULL,
    Activo      BIT             NOT NULL DEFAULT 1,
    EstaEliminado BIT           NOT NULL DEFAULT 0
);

CREATE TABLE Scheduling.Equipamiento (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    ConsultorioId        UNIQUEIDENTIFIER NULL REFERENCES Scheduling.Consultorios(Id), -- NULL = equipo móvil/compartido
    Nombre               NVARCHAR(150)   NOT NULL,
    NumeroSerie           VARCHAR(100)    NULL,
    FechaAdquisicion      DATE            NULL,
    ProximoMantenimiento  DATE            NULL,
    Activo                BIT             NOT NULL DEFAULT 1,
    EstaEliminado         BIT             NOT NULL DEFAULT 0
);

-- El horario NUNCA está embebido en Doctores — es su propia entidad, con soporte para
-- fijo, rotativo o por rango de fechas, y consultorio opcional (si rota, puede ser NULL = "por definir")
CREATE TABLE Scheduling.HorariosDoctor (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    DoctorId        UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Doctores(Id),
    ClinicaId       UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    ConsultorioId    UNIQUEIDENTIFIER NULL REFERENCES Scheduling.Consultorios(Id),
    TipoHorario      VARCHAR(20)     NOT NULL,      -- 'Fijo','Rotativo','PorRango'
    DiaSemana        TINYINT         NULL,           -- 0=Domingo..6=Sábado; NULL si es 'PorRango'
    HoraInicio        TIME            NOT NULL,
    HoraFin           TIME            NOT NULL,
    FechaInicioVigencia DATE          NOT NULL,       -- desde cuándo aplica este bloque
    FechaFinVigencia    DATE          NULL,           -- NULL = indefinido
    Activo             BIT             NOT NULL DEFAULT 1,
    EstaEliminado       BIT             NOT NULL DEFAULT 0
);
CREATE INDEX IX_HorariosDoctor_Doctor ON Scheduling.HorariosDoctor(DoctorId, FechaInicioVigencia, FechaFinVigencia);
CREATE INDEX IX_HorariosDoctor_Consultorio ON Scheduling.HorariosDoctor(ConsultorioId);

CREATE TABLE Scheduling.Citas (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    PacienteId          UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Pacientes(Id),
    DoctorId            UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Doctores(Id),
    ConsultorioId       UNIQUEIDENTIFIER NULL REFERENCES Scheduling.Consultorios(Id),
    FechaHoraInicio      DATETIME2(3)    NOT NULL,
    FechaHoraFin          DATETIME2(3)    NOT NULL,
    Estado                VARCHAR(20)     NOT NULL DEFAULT 'Programada', -- Programada/Confirmada/EnProceso/Completada/Cancelada/NoAsistio
    MotivoConsulta         NVARCHAR(300)   NULL,
    CreadoPorUsuarioId     UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    FechaCreacion           DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    EstaEliminado           BIT             NOT NULL DEFAULT 0,
    RowVersion               ROWVERSION
);
CREATE INDEX IX_Citas_Doctor_Fecha ON Scheduling.Citas(DoctorId, FechaHoraInicio) WHERE EstaEliminado = 0;
CREATE INDEX IX_Citas_Paciente ON Scheduling.Citas(PacienteId) WHERE EstaEliminado = 0;
CREATE INDEX IX_Citas_Clinica_Fecha ON Scheduling.Citas(ClinicaId, FechaHoraInicio) WHERE EstaEliminado = 0;
```

**Por qué el horario es tabla independiente:** el requisito lo exige explícitamente, y modelarlo así permite validar solapamientos (`FechaHoraInicio`/`FechaHoraFin` de una cita contra `HorariosDoctor`) sin tocar la entidad `Doctores`, y soporta múltiples bloques por doctor (mañana en Consultorio 1, tarde en Consultorio 3, distintos días).

**Restricción de solapamiento de citas:** SQL Server no tiene `EXCLUDE` nativo (a diferencia de PostgreSQL), así que la validación de "un doctor no puede tener dos citas simultáneas" debe hacerse en la capa de aplicación (Domain Service) antes del `SaveChanges`, idealmente dentro de una transacción con `SERIALIZABLE` o un índice único filtrado si se decide discretizar los slots.

---

## 6. Esquema `Billing` — Facturación, procedimientos, pagos y DGII

> Nota regulatoria (verificada, no asumida): República Dominicana está migrando de NCF tradicional a **e-CF (Comprobante Fiscal Electrónico)** bajo la Ley 32-23, con calendario escalonado por tamaño de contribuyente (grandes ya obligados; pequeños/micro con plazo extendido hasta el 15 de noviembre de 2026). El modelo de abajo se diseña **e-CF-first** (firma digital, envío y validación en línea a la DGII) con NCF de papel como fallback de contingencia, para no quedar obsoleto ante la fecha límite.

```sql
CREATE SCHEMA Billing;
GO

-- Rangos/secuencias de comprobantes autorizados por DGII, por clínica y tipo
CREATE TABLE Billing.SecuenciasComprobante (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId           UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    TipoComprobante      VARCHAR(5)      NOT NULL,      -- 'E31','E32','E34'... (tipos de e-CF) o 'B01','B02' (NCF legado)
    EsElectronico         BIT             NOT NULL DEFAULT 1,
    SecuenciaActual        BIGINT          NOT NULL DEFAULT 0,
    SecuenciaDesde          BIGINT          NOT NULL,
    SecuenciaHasta          BIGINT          NOT NULL,
    FechaVencimiento         DATE           NULL,
    Activo                    BIT            NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Secuencia_Clinica_Tipo UNIQUE (ClinicaId, TipoComprobante)
);

CREATE TABLE Billing.Facturas (
    Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClinicaId               UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    PacienteId               UNIQUEIDENTIFIER NOT NULL REFERENCES Personal.Pacientes(Id),
    TipoFactura               VARCHAR(10)     NOT NULL,      -- 'Fiscal','Interna'
    NumeroComprobante          VARCHAR(20)     NULL,           -- e-NCF/NCF asignado (NULL si es Interna)
    EstadoDGII                 VARCHAR(20)     NULL,           -- 'Pendiente','Enviado','Aceptado','Rechazado','Contingencia' (solo si Fiscal)
    FechaEmision                DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    Subtotal                     DECIMAL(18,2)   NOT NULL,
    Descuento                     DECIMAL(18,2)   NOT NULL DEFAULT 0,
    Impuestos                     DECIMAL(18,2)   NOT NULL DEFAULT 0,  -- ITBIS
    Total                          DECIMAL(18,2)   NOT NULL,
    Estado                          VARCHAR(20)     NOT NULL DEFAULT 'Pendiente', -- Pendiente/Parcial/Pagada/Anulada
    EstaAnulada                     BIT             NOT NULL DEFAULT 0,
    MotivoAnulacion                  NVARCHAR(300)   NULL,
    CreadoPorUsuarioId                UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    EstaEliminado                      BIT             NOT NULL DEFAULT 0,
    RowVersion                          ROWVERSION,
    CONSTRAINT CK_Factura_NumeroComprobante CHECK (
        (TipoFactura = 'Interna' AND NumeroComprobante IS NULL) OR (TipoFactura = 'Fiscal')
    )
);
CREATE INDEX IX_Facturas_Clinica_Fecha ON Billing.Facturas(ClinicaId, FechaEmision) WHERE EstaEliminado = 0;
CREATE INDEX IX_Facturas_Paciente ON Billing.Facturas(PacienteId) WHERE EstaEliminado = 0;
CREATE UNIQUE INDEX UQ_Facturas_Comprobante ON Billing.Facturas(ClinicaId, NumeroComprobante) WHERE NumeroComprobante IS NOT NULL;

CREATE TABLE Billing.FacturaDetalles (
    Id                          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    FacturaId                   UNIQUEIDENTIFIER NOT NULL REFERENCES Billing.Facturas(Id),
    ProcedimientoRealizadoId     UNIQUEIDENTIFIER NULL REFERENCES Clinical.ProcedimientosRealizados(Id), -- trazabilidad clínica opcional
    Descripcion                   NVARCHAR(300)   NOT NULL, -- snapshot textual, independiente de si el procedimiento cambia de nombre luego
    Cantidad                       INT             NOT NULL DEFAULT 1,
    PrecioUnitario                 DECIMAL(18,2)   NOT NULL,
    Subtotal                        DECIMAL(18,2)   NOT NULL
);
CREATE INDEX IX_FacturaDetalles_Factura ON Billing.FacturaDetalles(FacturaId);

-- Abonos / pagos parciales — historial completo, nunca se sobreescribe un pago
CREATE TABLE Billing.Pagos (
    Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    FacturaId                UNIQUEIDENTIFIER NOT NULL REFERENCES Billing.Facturas(Id),
    ClinicaId                 UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Clinicas(Id),
    Monto                      DECIMAL(18,2)   NOT NULL,
    FechaHora                   DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    MetodoPago                   VARCHAR(20)     NOT NULL,   -- 'Efectivo','Tarjeta','Transferencia','Cheque'
    Referencia                    VARCHAR(100)    NULL,       -- número de confirmación/autorización
    RegistradoPorUsuarioId         UNIQUEIDENTIFIER NOT NULL REFERENCES Security.Usuarios(Id),
    EstaAnulado                     BIT             NOT NULL DEFAULT 0,
    MotivoAnulacion                  NVARCHAR(300)   NULL,
    EstaEliminado                     BIT             NOT NULL DEFAULT 0,
    RowVersion                         ROWVERSION
);
CREATE INDEX IX_Pagos_Factura ON Billing.Pagos(FacturaId) WHERE EstaEliminado = 0;
CREATE INDEX IX_Pagos_Clinica_Fecha ON Billing.Pagos(ClinicaId, FechaHora);
```

**Por qué `EstadoDGII` es `NULL` para facturas internas:** una factura interna nunca se transmite a la DGII, así que forzar un estado de un flujo que no aplica confundiría a la capa de aplicación; el `CHECK` constraint documenta la regla de negocio directamente en el esquema.

**Por qué los pagos se anulan (`EstaAnulado`) en vez de eliminarse o editarse:** un abono mal registrado se corrige creando un pago de reversión o marcándolo anulado con motivo, nunca borrando la fila — es la única forma de que un contador/auditor reconstruya qué pasó y quién lo hizo.

**Por qué `FacturaDetalles.Descripcion` es un snapshot de texto y no solo el FK al procedimiento:** si el catálogo cambia el nombre del procedimiento el año próximo, una factura fiscal ya emitida y reportada a la DGII no puede "cambiar" retroactivamente de descripción.

---

## 7. Esquema `Audit` — Auditoría universal

```sql
CREATE SCHEMA Audit;
GO

CREATE TABLE Audit.Auditoria (
    Id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ClinicaId            UNIQUEIDENTIFIER NULL,             -- NULL para acciones de SuperAdmin a nivel plataforma
    UsuarioId             UNIQUEIDENTIFIER NULL REFERENCES Security.Usuarios(Id), -- NULL si fue un proceso del sistema
    Accion                 VARCHAR(20)     NOT NULL,          -- 'Create','Update','Delete','Login','Anular', etc.
    EntidadNombre           VARCHAR(150)    NOT NULL,          -- p.ej. 'Billing.Facturas'
    EntidadId                VARCHAR(64)     NOT NULL,          -- Id del registro afectado (string por flexibilidad de PK)
    DatosAnteriores           NVARCHAR(MAX)   NULL,             -- JSON serializado del estado previo
    DatosNuevos                NVARCHAR(MAX)   NULL,             -- JSON serializado del estado nuevo
    FechaHora                    DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    DireccionIP                   VARCHAR(45)     NULL,             -- soporta IPv6
    Dispositivo                    NVARCHAR(300)   NULL              -- User-Agent / identificador de dispositivo
);
CREATE INDEX IX_Auditoria_Clinica_Fecha ON Audit.Auditoria(ClinicaId, FechaHora DESC);
CREATE INDEX IX_Auditoria_Entidad ON Audit.Auditoria(EntidadNombre, EntidadId, FechaHora DESC);
CREATE INDEX IX_Auditoria_Usuario ON Audit.Auditoria(UsuarioId, FechaHora DESC);
```

**Por qué `EntidadId` es `VARCHAR(64)` y no `UNIQUEIDENTIFIER`:** la tabla de auditoría debe poder registrar cualquier entidad del sistema, incluida `Audit.Auditoria` sobre sí misma en teoría, o entidades futuras con PK compuesta serializada como texto — desacoplar el tipo de dato del esquema le da longevidad al diseño.

**Recomendación operativa a 10 años:** particionar `Audit.Auditoria` por rango de `FechaHora` (partición mensual o trimestral vía `PARTITION FUNCTION`/`PARTITION SCHEME` de SQL Server) desde el día uno. Es una tabla de solo-inserción que crecerá sin límite; particionar evita que en el año 5 una consulta de auditoría escanee cientos de millones de filas, y permite archivar particiones viejas a almacenamiento más barato sin downtime.

---

## 8. Patrón de Soft Delete — consistencia entre tablas

Todas las tablas de negocio comparten estas columnas (implementadas como interfaz común en EF Core, ver sección 10):

```
EstaEliminado       BIT           NOT NULL DEFAULT 0
FechaEliminacion    DATETIME2(3)  NULL
EliminadoPorUsuarioId UNIQUEIDENTIFIER NULL REFERENCES Security.Usuarios(Id)
```

En este documento se omitió `EliminadoPorUsuarioId` de algunas tablas por espacio, pero **debe agregarse a las 15 tablas de negocio** listadas arriba para que la auditoría de "quién eliminó qué" no dependa exclusivamente de `Audit.Auditoria` (defensa en profundidad: si la tabla de auditoría fallara, la columna igual documenta el autor del soft delete).

---

## 9. Índices — resumen consolidado y criterio de diseño

| Tabla | Índice | Motivo |
|---|---|---|
| Todas las tablas con `ClinicaId` | `(ClinicaId, ...)` como primera columna de índices compuestos | El 100% de las consultas de la aplicación filtran primero por clínica (multi-tenant); poner `ClinicaId` primero maximiza el uso del índice y habilita particionamiento futuro por clínica si el volumen lo exige. |
| Tablas con soft delete y consultas frecuentes | Índices **filtrados** `WHERE EstaEliminado = 0` | SQL Server excluye filas eliminadas del índice físicamente, reduciendo tamaño y mejorando velocidad — la inmensa mayoría de las consultas de UI solo quiere registros activos. |
| `Pacientes.Documento` | Único compuesto con `ClinicaId` | Dos clínicas distintas pueden tener pacientes con la misma cédula sin conflicto; dentro de una misma clínica, la cédula debe ser única. |
| `Facturas.NumeroComprobante` | Único filtrado (`WHERE NumeroComprobante IS NOT NULL`) | Un e-NCF nunca puede repetirse dentro de una clínica, pero muchas facturas internas tendrán el campo `NULL` simultáneamente. |
| `Audit.Auditoria` | Compuesto por entidad y por clínica+fecha | Los dos patrones de consulta reales de un auditor son "historial de este registro" y "todo lo que pasó en esta clínica en tal rango de fechas". |
| `HorariosDoctor` | `(DoctorId, FechaInicioVigencia, FechaFinVigencia)` | Soporta la consulta más común: "¿qué horarios tiene este doctor vigentes en tal fecha?". |

---

## 10. Consideraciones para Entity Framework Core

1. **Multi-tenancy con Global Query Filters.** Cada `DbSet` de una entidad con `ClinicaId` debe tener `modelBuilder.Entity<T>().HasQueryFilter(e => e.ClinicaId == _tenantContext.ClinicaActualId)`. Esto se combina automáticamente con el filtro de soft delete (`&& !e.EstaEliminado`) mediante una interfaz `ITenantEntity` + `ISoftDeletable` y un método de extensión que aplica ambos filtros a todas las entidades que las implementen, iterando `modelBuilder.Model.GetEntityTypes()` en `OnModelCreating`. Evita repetir la expresión lambda en cada entidad.

2. **Row-Level Security como segunda capa.** Los Global Query Filters son responsabilidad de la aplicación: un bug o un `IgnoreQueryFilters()` mal usado podría filtrar datos entre clínicas. Se recomienda además una **política de seguridad de SQL Server (`CREATE SECURITY POLICY`)** ligada a `SESSION_CONTEXT('ClinicaId')`, seteado por EF Core al abrir cada conexión (interceptor `IDbConnectionInterceptor`). Esto hace que incluso una consulta ad-hoc o un bug de la aplicación no pueda leer datos de otra clínica a nivel de motor de base de datos.

3. **Interceptor de auditoría, no triggers de SQL.** Implementar `SaveChangesInterceptor` que, antes de `SavingChangesAsync`, inspeccione `ChangeTracker.Entries()` y genere una fila en `Audit.Auditoria` por cada entidad `Added`/`Modified`/`Deleted` (el "Deleted" real nunca ocurre por el patrón soft-delete, así que en la práctica será casi siempre `Modified` con `EstaEliminado` pasando de `0` a `1`). El interceptor tiene acceso al `IHttpContextAccessor`/`AuthenticationStateProvider` de Blazor Server para capturar `UsuarioId`, IP y dispositivo — algo que un trigger de SQL no puede hacer.

4. **Nunca DbSet.Remove().** Se recomienda **eliminar el método `Remove` del vocabulario del equipo**: exponer únicamente un método de dominio `Eliminar(usuarioId)` en las entidades que setea `EstaEliminado`, `FechaEliminacion`, `EliminadoPorUsuarioId`, y dejar que EF Core lo trate como un `UPDATE` normal. Esto es más seguro que confiar en que cada desarrollador recuerde nunca usar `Remove`.

5. **Versionado del historial clínico como Value Object inmutable.** `HistorialClinicoEntradas` debe exponerse en el dominio como una colección de solo lectura desde `HistorialClinico` (agregado raíz DDD); el único método permitido es `AgregarEntrada(...)`, nunca `EditarEntrada(...)`. Esto hace cumplir a nivel de código C# la regla de "el historial es versionado y cambia en el tiempo" sin depender solo de la disciplina del desarrollador sobre la base de datos.

6. **Agregados DDD sugeridos** (límites de transacción/consistencia):
   - `Clinica` (raíz) — `Security`
   - `Paciente` (raíz) — `Personal`
   - `HistorialClinico` (raíz, contiene `HistorialClinicoEntradas` y estas contienen `ArchivosClinicos`/`ProcedimientosRealizados`) — `Clinical`
   - `Doctor` (raíz, contiene sus `HorariosDoctor`) — `Personal`/`Scheduling`
   - `Cita` (raíz) — `Scheduling`
   - `Factura` (raíz, contiene `FacturaDetalles` y `Pagos`) — `Billing`

   Ningún agregado referencia a otro por navegación de objeto completa, solo por Id (regla DDD estándar) — esto evita que EF Core intente cargar accidentalmente un grafo gigantesco que mezcle `Clinical` y `Billing` en una sola consulta.

7. **`ROWVERSION` y concurrencia optimista.** En `Facturas`, `Pagos` y `HistorialClinicoEntradas` es crítico: dos recepcionistas registrando un abono al mismo tiempo sobre la misma factura deben generar un conflicto de concurrencia detectable (`DbUpdateConcurrencyException`) en vez de que el segundo pago "pise" silenciosamente el estado calculado del primero.

8. **Migraciones por esquema.** Con Clean Architecture, se recomienda un único `DbContext` para simplicidad de multi-tenant (todas las entidades comparten conexión y `SESSION_CONTEXT`), pero organizando las `IEntityTypeConfiguration<T>` en carpetas por esquema (`Persistence/Configurations/Clinical/...`) para que el mapeo refleje la separación de dominios aunque el `DbContext` sea uno solo.

9. **Precisión decimal explícita.** Configurar globalmente en `OnModelCreating`: `configurationBuilder.Properties<decimal>().HavePrecision(18, 2)` para que ningún desarrollador olvide especificarlo por propiedad y EF Core no infiera un `decimal(18,0)` por defecto (error clásico que trunca centavos silenciosamente).

---

## 11. Vista simplificada de relaciones entre esquemas

```
Security.Clinicas (tenant raíz)
   │
   ├── Security.UsuarioClinicaRoles ── Security.Usuarios (global)
   │                                        │
   ├── Personal.Pacientes                   ├── Personal.Doctores
   ├── Personal.Doctores ───────────────────┘
   ├── Personal.Empleados ──────────────────┘
   │
   ├── Clinical.HistorialesClinicos ── Personal.Pacientes
   │        └── HistorialClinicoEntradas ── Personal.Doctores
   │                 ├── ArchivosClinicos
   │                 └── ProcedimientosRealizados ── Clinical.ProcedimientosCatalogo
   │
   ├── Scheduling.Consultorios ── Scheduling.Equipamiento
   ├── Scheduling.HorariosDoctor ── Personal.Doctores + Scheduling.Consultorios
   ├── Scheduling.Citas ── Personal.Pacientes + Personal.Doctores + Scheduling.Consultorios
   │
   ├── Billing.SecuenciasComprobante
   ├── Billing.Facturas ── Personal.Pacientes
   │        ├── FacturaDetalles ── Clinical.ProcedimientosRealizados (opcional)
   │        └── Pagos
   │
   └── Audit.Auditoria (referencia lógica a cualquier entidad vía EntidadNombre + EntidadId)
```

---

## 12. Próximos pasos sugeridos (fuera del alcance de este documento, pero relevantes)

- Definir la política de retención: la ley dominicana de e-CF exige conservar comprobantes 10 años — decidir si eso vive en SQL Server "frío" con partición archivada o se exporta a Blob Storage con solo metadatos en BD.
- Diseñar la política de `RLS` (`CREATE SECURITY POLICY`) con el DBA antes de ir a producción — este documento la recomienda pero no la implementa.
- Definir el proceso real de generación/firma/envío de e-CF a la DGII (fuera de alcance de modelo de datos: es integración externa).
- Revisar con un abogado/contador dominicano la clasificación de contribuyente de cada clínica cliente (grande/mediano/pequeño) porque determina si `EstadoDGII` de `Facturas` debe ser obligatorio desde el día uno o puede quedar en modo NCF de papel temporalmente según el calendario vigente de la DGII.
