# Hoja de Ruta y Fases del Proyecto: SaaS Clínico

Este documento detalla las 9 fases de construcción del sistema SaaS para Clínicas Médicas y Odontológicas. Cada fase tiene un objetivo claro y entregables específicos, asegurando una evolución limpia, mantenible y escalable.

## Stack Tecnológico Oficial

- **.NET 9**
- **ASP.NET Core**
- **Blazor Server**
- **Entity Framework Core**
- **SQL Server**
- **MudBlazor**
- **Clean Architecture**
- **Multi-tenant**
- **Auditoría completa**

---

## FASES DEL PROYECTO

### FASE 1 — Fundación de la Arquitectura
**Objetivo:** Crear una solución profesional y dejar lista toda la infraestructura base del proyecto.
- **Incluye:** Crear la solución, todos los proyectos, configurar referencias, instalar NuGet, configurar Clean Architecture, Program.cs base, estructura de carpetas, appsettings, configurar EF Core, SQL Server y Dependency Injection, y verificar compilación al 100%.
- **NO incluye:** Entidades, Servicios, CRUD, UI, Lógica ni Identity.

### FASE 2 — Dominio
**Objetivo:** Nace el sistema. Representar el núcleo del negocio (no la base de datos).
- **Incluye:** Entidades, Value Objects, Enums, Interfaces, Eventos de dominio (si aportan valor), Navegaciones, Relaciones, Agregados y Clases base comunes.

### FASE 3 — Persistencia
**Objetivo:** Implementación de Entity Framework Core contra la base de datos diseñada.
- **Incluye:** DbContext, Fluent API, Configuraciones por entidad (`IEntityTypeConfiguration<T>`), Índices, Restricciones, Seeds y la Primera migración.

### FASE 4 — Infraestructura
**Objetivo:** Implementación de capacidades técnicas.
- **Incluye:** Repositorios, Servicios de almacenamiento, Auditoría, Usuario actual, Tenant actual, Fecha/hora y Abstracciones.

### FASE 5 — Seguridad
**Objetivo:** Autenticación y Autorización.
- **Incluye:** ASP.NET Core Identity, Roles, Permisos, Policies, Claims, aislamiento Multi-tenant y Protección de datos.

### FASE 6 — Módulos del Negocio
**Objetivo:** Desarrollo iterativo de la lógica en el siguiente orden estricto:
1. Clínica
2. Usuarios
3. Empleados
4. Doctores
5. Consultorios
6. Horarios
7. Pacientes
8. Citas
9. Historia clínica
10. Archivos médicos
11. Procedimientos
12. Facturación
13. Pagos
14. Reportes

### FASE 7 — Integraciones
**Objetivo:** Conectar el SaaS con el mundo exterior.
- **Incluye:** DGII (Comprobantes fiscales), Correos, SignalR, Background Jobs, Notificaciones y Caché distribuida.

### FASE 8 — UI
**Objetivo:** Construcción de la interfaz de usuario.
- **Incluye:** MudBlazor, Layout, Dashboard, Componentes, Responsive, Tema, Animaciones y UX general.

### FASE 9 — Optimización
**Objetivo:** Preparación para producción y fiabilidad.
- **Incluye:** Logs, Health Checks, Performance, Caché avanzada, Seguridad avanzada y Monitoreo continuo.

---

## ESTADO ACTUAL DEL PROYECTO

**Etapa actual:** Listos para iniciar **FASE 4 — Infraestructura**.

### Hitos completados

✅ **FASE 1 — Fundación de la Arquitectura (Completada):**
- Se creó la solución `ClinicaSaaS.sln` estructurada limpiamente en proyectos (SharedKernel, Domain, Application, Persistence, Infrastructure, Web, UnitTests, IntegrationTests).
- Se establecieron correctamente todas las referencias de Clean Architecture.
- Se instalaron paquetes de NuGet esenciales (EF Core, MediatR, AutoMapper, FluentValidation, MudBlazor).
- Se sentaron las bases en el `SharedKernel` con clases fundamentales (`Result`, `IEntity`, `IAuditableEntity`, `ISoftDeletable`, `ITenantEntity`, `EntityBase`, `AuditableEntity`, `SoftDeleteEntity`).
- Se reestructuró el entorno físico en tres áreas maestras: `Db/` (para base de datos), `documentacion/` (para guías y planes) y `Solucion/` (para el código fuente).
- La solución compila correctamente sin errores.
- Los cambios fundacionales fueron versionados y subidos a la rama `Dev`.

✅ **FASE 2 — Dominio (Completada):**
- Se analizó el diseño de base de datos aprobado (6 esquemas: Security, Personal, Clinical, Scheduling, Billing, Audit) para modelar el dominio real del negocio, sin convertir tablas en clases de forma automática.
- Se identificaron y construyeron **12 Aggregate Roots**: `Clinica`, `Usuario`, `UsuarioClinicaRol`, `Paciente`, `Doctor`, `Empleado`, `HistorialClinico`, `ProcedimientoCatalogo`, `Consultorio`, `Equipamiento`, `Cita`, `Factura`, `SecuenciaComprobante`.
- Se construyeron las **Entities internas** de cada agregado: `HorarioDoctor` (en Doctor), `HistorialClinicoEntrada`, `ArchivoClinico`, `ProcedimientoRealizado` (en HistorialClinico), `FacturaDetalle`, `Pago` (en Factura).
- Se crearon **10 Value Objects** justificados: `Email`, `Rnc`, `DocumentoIdentidad`, `ContactoEmergencia`, `BloqueHorario`, `PeriodoVigencia`, `HashArchivo`, `SignosVitales`, `Dinero`, `CodigoComprobanteFiscal`.
- Se definieron **11 Enums** para todos los estados cerrados del negocio (`RolClinica`, `PlanSuscripcion`, `Genero`, `TipoDocumentoIdentidad`, `TipoHorario`, `TipoArchivoClinico`, `EstadoCita`, `TipoFactura`, `EstadoDGII`, `EstadoFactura`, `MetodoPago`).
- Se agregó al `SharedKernel` lo estrictamente necesario para soportar el dominio: `Guard` (precondiciones de programador) y `DomainException` (violaciones de invariantes verdaderamente excepcionales), siguiendo el patrón `Result<T>` ya existente para toda la validación de negocio esperada.
- Se implementaron reglas de negocio clave: `HistorialClinico` es append-only (nunca se edita una entrada, solo se agrega una nueva versión enlazada a la anterior), `Factura` con máquina de estados y snapshots de precio, `Cita` con transiciones de estado validadas, y una interfaz de dominio (`IValidadorDisponibilidadDoctor`) para la validación de solapamiento de citas que requiere consultar otros agregados.
- Se revisó y corrigió el código a mano (colisiones de nombre entre propiedades y tipos, métodos `Eliminar` públicos faltantes en 8 de los agregados con soft-delete) antes de integrarlo.
- Se detectó y resolvió un incidente de pérdida local de `Solucion/` (por quedar la carpeta parada en la rama `documentacion` durante una limpieza de Antigravity); el código se recuperó intacto desde la rama `Dev`.
- Se agregó `.gitignore` a `Solucion/` y se dejaron de trackear `bin/`, `obj/` y `.vs/` (991 archivos limpiados del repositorio).
- El dominio completo (45 archivos) fue copiado sobre la solución recuperada, la solución compiló correctamente (`dotnet build` limpio), y todo fue versionado y subido a la rama `Dev`.

✅ **FASE 3 — Persistencia (Completada):**
- Se implementó `ClinicaSaaSDbContext` con los `DbSet<T>` de los 12 Aggregate Roots del dominio, más el registro de `AuditLogEntry` para el esquema de auditoría.
- Se crearon las **Configuraciones Fluent API** (`IEntityTypeConfiguration<T>`) de cada entidad, organizadas por esquema de negocio: `Security`, `Personal`, `Clinical`, `Scheduling`, `Billing` y `Audit` — incluyendo el mapeo de Value Objects con `OwnsOne` (`Email`, `Rnc`, `DocumentoIdentidad`, `Dinero`, `SignosVitales`, etc.).
- Se implementaron **4 interceptores de `SaveChanges`**:
  - `AuditableEntitySaveChangesInterceptor` y `AuditoriaSaveChangesInterceptor` — para el registro automático de auditoría (creación, modificación, quién y cuándo).
  - `TenantSessionContextConnectionInterceptor` — setea `SESSION_CONTEXT('ClinicaId')` en cada conexión, base para el aislamiento multi-tenant (y para la futura política de `RLS` en SQL Server).
  - `FacturaTotalesSaveChangesInterceptor` — sincroniza los totales calculados del dominio (`Factura.Subtotal`/`Total`) hacia las columnas físicas persistidas, justo antes de guardar.
- Se creó `ClinicaSaaSDbContextFactory` (`IDesignTimeDbContextFactory<ClinicaSaaSDbContext>`) para que las herramientas de EF Core (`dotnet ef migrations add`, `dotnet ef database update`) puedan construir el `DbContext` en tiempo de diseño sin levantar todo el host de Blazor Server ni depender de `ICurrentUserContext`/`ITenantContext` reales (que se implementan en Fase 4).
- Se generó y aplicó la primera migración, `InicialEsquemaCompleto`, con **3 ajustes escritos a mano en SQL crudo** (`migrationBuilder.Sql(...)`) que EF Core no pudo resolver automáticamente:
  1. `UQ_Paciente_Documento_Clinica` — índice único compuesto entre `ClinicaId` y una propiedad de un Value Object mapeado con `OwnsOne`.
  2. `FK_UCR_Rol` — la FK de `Security.UsuarioClinicaRoles.RolId` hacia `Security.Roles.Id`, ya que `RolId` en el dominio es un enum convertido a `int`.
  3. Sincronización de `Factura.Subtotal`/`Total` y `FacturaDetalle.Subtotal` entre el dominio (propiedades calculadas) y la base de datos (columnas físicas para reportería), resuelta vía interceptor.
- Se sembró (`seed`) el catálogo fijo de los **5 roles** del sistema en `Security.Roles`.
- Se decidió y documentó que el script SQL manual original (`Db/01-crear-base-datos-saas-clinicas.sql`) queda **descartado como fuente de verdad**: el esquema real ahora se genera únicamente desde el código C# (`ClinicaSaaSDbContext` + `Configurations` + `Migrations`), evitando dos fuentes de verdad desincronizadas.
- Se corrigió un problema de configuración detectado al validar contra SQL Server Management Studio: la cadena de conexión en `appsettings.json` apuntaba a `(localdb)\MSSQLLocalDB` (una instancia distinta a la que administra el usuario en SSMS), causando que la base de datos migrada no fuera visible. Se actualizó a la instancia real `LAPTOP-Q920ARC9\SQLEXPRESSS`, tanto en `appsettings.json` como en el fallback de `ClinicaSaaSDbContextFactory`, y se re-aplicó la migración correctamente.
- Se verificó manualmente en SSMS, contra la base `ClinicaSaaS` ya creada: los 6 esquemas de negocio presentes, **21 tablas** en total (13 Aggregate Roots, 6 entidades internas de agregado y 2 tablas de soporte — `Auditoria` y `Roles`), el seed de los 5 roles en `Security.Roles`, y el registro correcto de la migración en `__EFMigrationsHistory`.
- Todo el trabajo de la fase fue versionado y subido a la rama `Dev`, incluyendo el fix de connection string como commit separado (`fix: apuntar connection string a instancia SQLEXPRESSS`).

**Pendiente detectado (no bloqueante, a resolver en fases posteriores):** `appsettings.json` no está en `.gitignore`, por lo que la cadena de conexión con el nombre de servidor de esta máquina (`LAPTOP-Q920ARC9\SQLEXPRESSS`) queda versionada tal cual. Antes de trabajar en equipo o en otra máquina, conviene moverla a `appsettings.Development.json` (ya ignorado) o a User Secrets.