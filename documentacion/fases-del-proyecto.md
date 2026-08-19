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

**Etapa actual:** Listos para iniciar **FASE 6 — Módulos del Negocio**.

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

✅ **FASE 4 — Infraestructura (Completada):**
- Se definieron en `Application/Common/Interfaces` las abstracciones técnicas que consumirán los casos de uso de Fase 6, sin que Application dependa de EF Core ni de ASP.NET Core:
  - `IRepositorio<TEntity>` — contrato genérico (`ObtenerPorIdAsync`, `Consultar` como `IQueryable`, `AgregarAsync`) válido para los 13 Aggregate Roots, respetando los Global Query Filters de tenant/soft-delete ya configurados en el DbContext desde Fase 3. Deliberadamente sin `Actualizar`/`Eliminar`: el tracking de EF Core cubre las modificaciones y el borrado lógico ya es un método de dominio.
  - `IUnitOfWork` — confirma en una sola transacción todos los cambios hechos a través de uno o varios repositorios durante el mismo caso de uso.
  - `IFileStorageService` (+ el record `ResultadoAlmacenamiento`) — abstracción de almacenamiento de archivos binarios para `ArchivoClinico` (Fase 2), pensada para poder cambiar la implementación (disco local → Azure Blob/S3) sin tocar Application ni Web.
  - `IDateTimeProvider` — abstracción del reloj del sistema, para permitir controlar la hora en pruebas unitarias futuras.
- Se implementó en `Persistence`:
  - `RepositorioBase<TEntity>` — única implementación de `IRepositorio<TEntity>` para todos los agregados (no una clase por cada uno), y `UnitOfWork` (delega en `ClinicaSaaSDbContext.SaveChangesAsync`, donde ya corren los 4 interceptores de Fase 3).
  - `PersistenceServiceCollectionExtensions.AddPersistence()` — se centralizó ahí todo el registro de DI que antes vivía directo en `Program.cs` (DbContext + los 4 interceptores + `IUnitOfWork` + `IRepositorio<>` genérico).
- Se implementó en `Infrastructure` (antes vacío, solo el `.csproj`):
  - `HttpUserContext` — implementación real conjunta de `ICurrentUserContext` **e** `ITenantContext`, leyendo `IHttpContextAccessor`/`ClaimsPrincipal`. Captura los valores una única vez en el constructor (registrado Scoped, una instancia por circuito) para evitar el problema conocido de Blazor Server donde `HttpContext` puede quedar `null` después de la conexión inicial. Hoy, sin ASP.NET Core Identity todavía (Fase 5), siempre devuelve `UsuarioId`/`ClinicaId` nulos — comportamiento esperado; la lectura de claims (`ClaimTypes.NameIdentifier` y un claim custom `"clinica_id"`) ya queda lista para cuando Fase 5 empiece a emitirlos.
  - `SystemDateTimeProvider` — implementación real de `IDateTimeProvider` sobre `DateTime.UtcNow`.
  - `LocalFileStorageService` — implementación real de `IFileStorageService` sobre disco local (ruta configurable vía `Storage:RutaLocal` en `appsettings.json`, con fallback a `App_Data/archivos-clinicos`), calculando SHA-256 y generando nombres físicos únicos por archivo (nunca el nombre original).
  - `InfrastructureServiceCollectionExtensions.AddInfrastructure()` — registra `IHttpContextAccessor`, `HttpUserContext` (resuelto como la misma instancia hacia ambas interfaces), `IDateTimeProvider` e `IFileStorageService`.
  - Se agregó `<FrameworkReference Include="Microsoft.AspNetCore.App" />` al `.csproj` de Infrastructure (necesario para `IHttpContextAccessor`, sin convertirlo en un proyecto Web).
- Se refactorizó `Program.cs`: ya no arma el `DbContext` ni los stubs de diseño a mano — ahora solo llama `builder.Services.AddInfrastructure()` y `builder.Services.AddPersistence(builder.Configuration)`.
- Se actualizó el comentario de `DesignTimeContextStubs` (Persistence): esas implementaciones mínimas quedan reservadas exclusivamente para `ClinicaSaaSDbContextFactory` (herramientas `dotnet ef`), ya no las usa la app en caliente.
- Se agregó la sección `"Storage": { "RutaLocal": "App_Data/archivos-clinicos" }` a `appsettings.json`, y `App_Data/` al `.gitignore` (los archivos clínicos subidos localmente nunca deben versionarse).
- Se verificó `dotnet build` limpio en los 7 proyectos de la solución (0 errores; solo advertencias preexistentes de Fase 2 y una de seguridad de AutoMapper, ninguna nueva de Fase 4), y se corrió la app (`dotnet run`) confirmando arranque sin excepciones y carga correcta en el navegador — validando que todo el cableado de Dependency Injection de Fase 4 (`AddInfrastructure` + `AddPersistence`) quedó bien conectado de punta a punta.
- Todo el trabajo de la fase fue versionado y subido a la rama `Dev`.

✅ **FASE 5 — Seguridad (Completada):**
- **Decisión de diseño clave:** no se usó ASP.NET Core Identity completo (con sus propias tablas `AspNetUsers`) — el `Usuario` del dominio (Fase 2) ya tiene su propio `PasswordHash`, y traer las tablas de Identity encima habría creado dos fuentes de verdad de usuarios. Se construyó la autenticación/autorización real sobre el propio `Usuario`/`UsuarioClinicaRol`, reutilizando únicamente `PasswordHasher<TUser>` (la clase de hashing PBKDF2 de Identity Core, sin ningún store) para no reinventar la parte criptográfica.
- Se agregó en `Application`:
  - `IPasswordHasher` (+ `ResultadoVerificacionPassword`) e `IAutenticacionService` (+ los DTOs `UsuarioAutenticado`/`MembresiaClinica`).
  - `AutenticacionService` — valida credenciales contra `Usuario`, detecta usuario inactivo, soporta rehash automático si el algoritmo cambiara de versión algún día, y devuelve las membresías activas del usuario en todas sus clínicas (aprovechando que el filtro global de tenant de `UsuarioClinicaRol` se "abre" cuando todavía no hay `ClinicaId` activo, definido desde Fase 3).
  - `ApplicationServiceCollectionExtensions.AddApplication()`.
  - Se extendió `IRepositorio<TEntity>` (Fase 4) con `PrimeroOPredeterminadoAsync`/`ListarAsync` — necesarios para que Application pueda consultar de forma async por criterio propio sin depender de EF Core directamente. Implementados en `RepositorioBase<TEntity>` (Persistence).
- Se implementó en `Infrastructure`:
  - `Pbkdf2PasswordHasher` — envuelve `PasswordHasher<Usuario>` de ASP.NET Core Identity.
  - `ClinicaSaaSAuthorizationPolicies` — genera automáticamente una Policy de autorización por cada valor del enum `RolClinica` (Fase 2), más una Policy `SuperAdminSaaS`; un SuperAdmin SaaS siempre pasa cualquier Policy de rol.
  - `SecurityServiceCollectionExtensions.AddClinicaSaaSSecurity()` — autenticación por cookie (elegida sobre JWT por ser la opción correcta para Blazor Server), estado de autenticación en cascada para `AuthorizeView`/`AuthorizeRouteView`, y registro de las Policies.
  - Se registró `IPasswordHasher` en `InfrastructureServiceCollectionExtensions.AddInfrastructure()` (Fase 4), junto al resto de capacidades técnicas.
- Se implementó en `Web`:
  - `AuthEndpoints` (Minimal API) — `/Account/Login` y `/Account/Logout`, deliberadamente **fuera** de Blazor: `HttpContext.SignInAsync`/`SignOutAsync` no pueden llamarse de forma confiable desde dentro de un circuito interactivo de Blazor Server. Es el patrón oficial de .NET 8+: la página `/login` se renderiza **estática** (sin `@rendermode`) con un `<form>` HTML normal que hace POST directo a estos endpoints, protegido con `<AntiforgeryToken />` validado server-side.
  - Los claims emitidos al iniciar sesión incluyen `NameIdentifier`, `Email`, `Name`, el claim custom `superadmin_saas` (si aplica), y — para la primera membresía activa del usuario — `clinica_id` y el `Role`. Un selector de clínica para usuarios con membresías en varias clínicas queda fuera del alcance de esta fase y se resuelve en una fase posterior sin romper la sesión mientras tanto (usa la primera por defecto).
  - `Routes.razor` ahora usa `AuthorizeRouteView` (con `RedirectToLogin` para usuarios no autenticados, y mensaje de acceso denegado para autenticados sin permiso) en vez de `RouteView` simple. `NavMenu.razor` muestra el nombre del usuario/opción de cerrar sesión, o el enlace de iniciar sesión, según corresponda — sin tocar ninguno de los enlaces existentes (Home/Counter/Weather).
  - `/mi-cuenta` (protegida con `[Authorize]`) — página de verificación end-to-end: muestra `ICurrentUserContext`/`ITenantContext` (Fase 4) y los claims reales de la sesión, confirmando que ambas fases quedaron bien conectadas.
  - `DevDataSeeder` — **solo en Development**: crea un único Usuario SuperAdmin de prueba (`admin@clinicasaas.dev`) la primera vez que se corre la app localmente y no existe ningún Usuario todavía, para poder probar el login sin esperar al módulo real de alta de usuarios (Fase 6). Queda documentado para eliminarse en cuanto ese módulo exista.
  - No se corrigió ninguna migración de EF Core — Fase 5 no modificó el esquema de base de datos, solo la capa de aplicación/seguridad.
- **Bug detectado y corregido durante la verificación:** `AutenticacionService` comparaba `u.Email.Valor == valor` dentro del predicado async — EF Core no puede traducir un acceso a un miembro interno de una propiedad mapeada con `HasConversion` (a diferencia de `OwnsOne`). Se corrigió comparando el Value Object completo (`u.Email == emailBuscado`), que EF sí traduce aplicando el converter configurado en `UsuarioConfiguration`.
- Se verificó `dotnet build` limpio en los 9 proyectos de la solución, y se probó el flujo completo en caliente: `DevDataSeeder` creando el usuario (confirmado en SSMS y en el log de auditoría automático), `/mi-cuenta` bloqueando el acceso sin sesión, login exitoso con las credenciales de prueba, `/mi-cuenta` mostrando el `UsuarioId` y los claims reales tras autenticarse, y logout devolviendo al estado sin sesión.
- Todo el trabajo de la fase fue versionado y subido a la rama `Dev` (`feat: fase 5 - seguridad`).


### Prompt Record
**User Prompt:**
> Módulo 5 de Fase 6 (Módulos del Negocio): Consultorios — implementar IConsultorioService + ConsultorioService, registrar en DI, y crear las páginas Blazor Index/Nuevo/Editar con protección AdminClinica, más el enlace en NavMenu.

**Action Taken / Agent Summary:**
- Cloné la rama `Dev` del repositorio y confirmé el último commit (`fase 6 - modulo 4: doctores`) para verificar que correspondía implementar el Módulo 5 según el orden estricto del roadmap.
- Revisé exhaustivamente los archivos de plantilla: IDoctorService/DoctorService, IEmpleadoService/EmpleadoService, IClinicaService, las páginas Blazor de Doctores/Empleados/Clínicas, la entidad de dominio `Consultorio` (ya existente desde Fase 2/3, sin `ActualizarDatos`), `ConsultorioConfiguration`, `IRepositorio`, `IUnitOfWork`, `ITenantContext` y `NavMenu`.
- Agregué `Consultorio.ActualizarDatos(nombre, piso)` al dominio, mismo patrón que `Doctor.ActualizarDatos`/`Empleado.ActualizarDatos`.
- Creé `Application/Consultorios/IConsultorioService.cs` con DTOs (`ConsultorioDto`, `RegistrarConsultorioRequest`, `ActualizarDatosConsultorioRequest`) e interface completa; documenté a Equipamiento como fuera de alcance de este módulo (no listado en el roadmap de Fase 6).
- Creé `Application/Consultorios/ConsultorioService.cs` implementando todos los casos de uso, con aislamiento de tenant en escritura idéntico a DoctorService/EmpleadoService, y join en memoria con `IClinicaService` para el nombre de clínica (sin `IUsuarioService`, ya que Consultorio no está ligado a un Usuario).
- Actualicé `ApplicationServiceCollectionExtensions.cs` registrando `IConsultorioService → ConsultorioService`.
- Creé `Web/Components/Pages/Consultorios/Index.razor`, `Nuevo.razor`, `Editar.razor` replicando el patrón de Doctores (`AuthorizeView` AdminClinica, modelos sellados locales, manejo de errores inline), usando la convención de carpetas `Pages/<Módulo>/` (mayoritaria: Clínicas/Usuarios/Doctores) en vez de la excepción de Empleados.
- Actualicé `NavMenu.razor` agregando el enlace "Consultorios" dentro del bloque `AuthorizeView` existente de AdminClinica, junto a Empleados y Doctores.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de confirmar compilación de tu lado tras pegar los archivos.


### Prompt Record
**User Prompt:**
> Módulo 6 de Fase 6 (Módulos del Negocio): Horarios — implementar sobre el repo ya actualizado
> con el Módulo 5 (Consultorios) integrado y confirmado funcionando, sin errores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`1f725f4`) y confirmé que Consultorios (Módulo 5)
  quedó integrado correctamente.
- Revisé el dominio existente de `HorarioDoctor` (Fase 2) y descubrí que NO es un Aggregate Root
  — es una Entity interna del agregado `Doctor`, con `Doctor.AgregarHorario`/
  `Doctor.DesactivarHorario` ya implementados y validando solapamiento entre bloques.
- Confirmé que la Persistencia (`HorarioDoctorConfiguration`, dentro de `DoctorConfiguration.cs`)
  y la tabla `Scheduling.HorariosDoctor` ya existían desde Fase 3 — no hizo falta ninguna
  migración nueva para este módulo.
- Creé `Application/Horarios/IHorarioService.cs` y `HorarioService.cs`, operando exclusivamente
  a través de `IRepositorio<Doctor>` (nunca un repositorio propio para `HorarioDoctor`, respetando
  el límite del agregado). Solo expuse `AgregarAsync`/`DesactivarAsync`/`ListarPorDoctorAsync`
  porque son los únicos casos de uso que el dominio ofrece.
- Agregué una validación de Application (no de dominio): que el `ConsultorioId` de un nuevo
  bloque pertenezca a la misma clínica del doctor.
- Registré `IHorarioService → HorarioService` en `ApplicationServiceCollectionExtensions.cs`.
- Creé las páginas Blazor `Web/Components/Pages/Horarios/Index.razor` (lista de doctores),
  `Detalle.razor` (bloques de horario de un doctor + desactivar) y `Nuevo.razor` (formulario para
  agregar un bloque), navegación `"por doctor"` reflejando la estructura del dominio.
- Actualicé `NavMenu.razor` agregando el enlace "Horarios" en el bloque de AdminClinica.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos.

### Prompt Record
**User Prompt:**
> Módulo 7 de Fase 6 (Módulos del Negocio): Pacientes — implementar siguiendo el mismo criterio
> que los módulos anteriores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`b2630ff Fix de horarios persistentes`) y confirmé que
  el Módulo 6 (Horarios) sigue funcionando correctamente.
- Revisé el dominio y persistencia de `Paciente` (Fase 2/3, ya existentes) y confirmé una
  diferencia clave con Doctor/Empleado/Consultorio: `Paciente` no tiene `Activo`/`Desactivar`/
  `Reactivar`, solo `EstaEliminado` (soft-delete directo) — así lo modeló el diseño de BD desde el
  principio, no lo cambié.
- Creé `Application/Pacientes/IPacienteService.cs` con DTOs y requests separando explícitamente
  `ActualizarDatosPersonales` de `ActualizarDatosDeContacto`, reflejando la separación que ya
  tenía el dominio.
- Creé `Application/Pacientes/PacienteService.cs`. A diferencia de Clínicas/Doctores/Consultorios
  (listas cortas que se listan enteras), `ListarAsync` acepta un filtro de búsqueda que se traduce
  a una `Expression<Func<Paciente, bool>>` pasada a `IRepositorio<Paciente>.ListarAsync(predicado)`
  — se ejecuta como `WHERE` en SQL Server, sin que Application dependa de EF Core.
- Registré `IPacienteService → PacienteService` en `ApplicationServiceCollectionExtensions.cs`.
- Creé `Web/Components/Pages/Pacientes/Index.razor` (con buscador con debounce de 300ms),
  `Nuevo.razor` (solo nombres/apellidos, redirige a Editar para completar el resto) y
  `Editar.razor` (dos formularios independientes — Datos personales y Contacto — reflejando los
  dos métodos separados del dominio).
- Actualicé `NavMenu.razor` agregando el enlace "Pacientes" en el bloque de AdminClinica.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos.


### Prompt Record
**User Prompt:**
> Módulo 8 de Fase 6 (Módulos del Negocio): Citas — implementar siguiendo el mismo criterio que
> los módulos anteriores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`ee481bf Modulo de pacientes integrado`).
- Revisé el dominio de `Cita` (Fase 2/3, ya existente): máquina de estados completa
  (`TransicionesValidas`) y el Domain Service `IValidadorDisponibilidadDoctor`, cuya
  implementación **no existía todavía** — el propio comentario del archivo indicaba que iba en
  Persistence.
- Creé `Persistence/Services/ValidadorDisponibilidadDoctor.cs` con la fórmula estándar de
  solapamiento de intervalos contra `Scheduling.Citas`, excluyendo citas Canceladas/NoAsistio, y
  lo registré en `PersistenceServiceCollectionExtensions.cs`.
- Creé `Application/Citas/ICitaService.cs`/`CitaService.cs` con un método por cada transición del
  agregado (`Confirmar`, `IniciarAtencion`, `Completar`, `Cancelar`, `MarcarNoAsistio`,
  `Reprogramar`, `Eliminar`), cada uno espejo directo del dominio — sin inventar transiciones
  nuevas. Antes de `Programar`/`Reprogramar`, valida consultorio (misma clínica) y disponibilidad
  del doctor vía el Domain Service recién implementado.
- Registré `ICitaService → CitaService` en `ApplicationServiceCollectionExtensions.cs`.
- Creé `Web/Components/Pages/Citas/Index.razor` (agenda: selecciona doctor + fecha, ve sus citas
  de ese día, con botones de acción que reflejan exactamente `TransicionesValidas`), `Nueva.razor`
  (formulario de alta, con `[SupplyParameterFromQuery]` para prellenar doctor/fecha al llegar
  desde la agenda) y `Reprogramar.razor`.
- Actualicé `NavMenu.razor` agregando el enlace "Citas" en el bloque de AdminClinica.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos.


### Prompt Record
**User Prompt:**
> Módulo 9 de Fase 6 (Módulos del Negocio): Historia Clínica — implementar siguiendo el mismo
> criterio que los módulos anteriores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`1f4ec9b fase 6 modulo 8 citas listo funcionando`).
- Revisé el dominio y persistencia de `HistorialClinico` (Fase 2/3, ya existentes): agregado
  separado de Paciente (1:1 forzado por BD), con `HistorialClinicoEntrada` como Entity interna
  append-only — nunca se edita una entrada, corregir un dato es agregar una nueva enlazada vía
  `EntradaAnteriorId`.
- Identifiqué el mismo problema que Doctor/Horarios (Módulo 6): `Entradas` es una colección hija
  que `RepositorioBase.ObtenerPorIdAsync` no carga por defecto. Creé
  `IRepositorioHistorialClinico`/`RepositorioHistorialClinico` con `Include(h => h.Entradas)`, más
  un método `ObtenerPorPacienteIdAsync` porque la UI navega desde el paciente, nunca desde el Id
  del historial directamente.
- Creé `Application/HistorialesClinicos/IHistorialClinicoService.cs`/`HistorialClinicoService.cs`.
  `AgregarEntradaAsync` crea el `HistorialClinico` de forma perezosa si el paciente no tiene uno
  todavía (relación 1:1 sin paso de setup manual), valida que el doctor pertenezca a la clínica
  del paciente, y si se enlaza una Cita, que sea de ese mismo paciente (reutilizando
  `ICitaService.ListarPorPacienteAsync`, Módulo 8).
- Registré el repositorio en `PersistenceServiceCollectionExtensions.cs` y el servicio en
  `ApplicationServiceCollectionExtensions.cs`.
- Creé `Web/Components/Pages/HistorialesClinicos/Index.razor` (buscador de pacientes, mismo
  patrón de debounce que Pacientes/Índex) y `Detalle.razor` (formulario para agregar una entrada
  nueva — nunca editar — más el timeline de entradas existentes en orden descendente por versión).
- Actualicé `NavMenu.razor` agregando el enlace "Historia Clínica" en el bloque de AdminClinica.
- **Deliberadamente no expuse** `AgregarArchivoAEntradaActual` ni
  `RegistrarProcedimientoEnEntradaActual` — ya existen en el dominio pero pertenecen a los
  Módulos 10 y 11.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos.


### Prompt Record
**User Prompt:**
> Módulo 10 de Fase 6 (Módulos del Negocio): Archivos Médicos — implementar siguiendo el mismo
> criterio que los módulos anteriores, con especial cuidado porque es un módulo sensible
> (archivos médicos de pacientes).

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`1f4ec9b`, última referencia confirmada — el Módulo 9
  todavía no aparecía pusheado al remoto, así que este paquete asume que se integró tal cual se
  entregó).
- Revisé el dominio y persistencia de `ArchivoClinico` (Fase 2/3, ya existentes): Entity interna
  de `HistorialClinicoEntrada`, con `HashArchivo` (VO validando formato SHA-256) y
  `TipoArchivoClinico`. Confirmé que `IFileStorageService` (Fase 4,
  `Infrastructure/Storage/LocalFileStorageService.cs`) ya estaba completo — calcula el hash,
  genera nombre físico único, guarda fuera de `wwwroot` — no hizo falta escribir nada de
  almacenamiento.
- Encontré un hueco real del dominio: no existía forma de eliminar un archivo ya subido. Agregué
  `HistorialClinicoEntrada.EliminarArchivo` (internal) y `HistorialClinico.EliminarArchivo`
  (público, busca en todas las entradas).
- Extendí `RepositorioHistorialClinico` (Módulo 9) con `.ThenInclude(e => e.Archivos)` — sin esto
  hubiéramos repetido el mismo bug de "desaparece al recargar" que ya arreglamos dos veces.
- Creé `Application/HistorialesClinicos/IArchivoClinicoService.cs`/`ArchivoClinicoService.cs`.
  Respeté al pie de la letra el contrato documentado de `IFileStorageService`: el borrado normal
  de un archivo es solo soft-delete de dominio (el binario se conserva); `EliminarAsync` de
  `IFileStorageService` solo se invoca como compensación si el binario se sube con éxito pero el
  resto de la operación falla (dominio rechaza, o `GuardarCambiosAsync` lanza).
- Creé `Web/ArchivosClinicosEndpoints.cs` — Minimal API fuera de Blazor, mismo patrón que
  `AuthEndpoints.cs` (Blazor Server no puede empujar una descarga de archivo por el circuito
  SignalR), protegida con la misma Policy y nunca aceptando una ruta de disco arbitraria del
  cliente.
- Extendí `Web/Components/Pages/HistorialesClinicos/Detalle.razor` (Módulo 9) con la sección de
  Archivos clínicos: subida vía `InputFile` (límite 15 MB, solo .jpg/.jpeg/.png/.pdf), listado con
  descarga y eliminación.
- Registré todo en `PersistenceServiceCollectionExtensions.cs` (repositorio), `ApplicationServiceCollectionExtensions.cs`
  (servicio) y `Program.cs` (endpoint).
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos. Dado que este módulo toca almacenamiento de archivos
  reales, además del build te recomiendo probar en caliente: subir un archivo, verificar que
  aparece en `App_Data/archivos-clinicos/` (o la ruta que tengas configurada en
  `Storage:RutaLocal`), descargarlo, y eliminarlo comprobando que el binario sigue en disco.

### Prompt Record
**User Prompt:**
> Módulo 11 de Fase 6 (Módulos del Negocio): Procedimientos — implementar siguiendo el mismo
> criterio que los módulos anteriores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`b56f31e Modulo 10 integrado`).
- Revisé el dominio y persistencia de `ProcedimientoCatalogo`/`ProcedimientoRealizado` (Fase 2/3,
  ya existentes): dos agregados distintos — el catálogo de precios de la clínica (independiente)
  y el registro de que un procedimiento se ejecutó de verdad (Entity interna de
  `HistorialClinicoEntrada`, con `PrecioAplicado` como snapshot deliberado del precio).
- Agregué `ProcedimientoCatalogo.ActualizarDatos` — faltaba corregir código/nombre/descripción
  (separado de `CambiarPrecio`, que ya existía con su propia guarda de "no repreciar inactivo").
- Extendí `RepositorioHistorialClinico` (Módulos 9 y 10) con
  `.ThenInclude(e => e.ProcedimientosRealizados)` — exactamente lo que dejé anotado como pendiente
  en el SIP del Módulo 10.
- Decidí **no** agregar una forma de eliminar un `ProcedimientoRealizado`: a diferencia de
  `ArchivoClinico` (Módulo 10), su tabla no tiene columnas de soft-delete — es evidencia clínica y
  financiera permanente por diseño, no un descuido. Agregar borrado requeriría tocar el esquema,
  fuera del alcance de este módulo.
- Creé `Application/Procedimientos/` con dos servicios: `IProcedimientoCatalogoService`
  (CRUD del catálogo, mismo patrón que Consultorios) e `IProcedimientoRealizadoService`
  (registrar un procedimiento realizado desde el expediente de un paciente, validando que doctor
  y procedimiento pertenezcan a la clínica del paciente, y que el procedimiento esté activo).
- Registré ambos en `ApplicationServiceCollectionExtensions.cs`.
- Creé `Web/Components/Pages/Procedimientos/Index.razor`/`Nuevo.razor`/`Editar.razor` (catálogo,
  con secciones separadas de Datos y Precio en Editar, igual que Pacientes en el Módulo 7).
- Extendí `Web/Components/Pages/HistorialesClinicos/Detalle.razor` (Módulos 9 y 10) con la sección
  "Procedimientos realizados" — formulario para registrar uno (doctor, procedimiento del catálogo
  activo, pieza dental opcional, precio opcional) más el listado de los ya registrados.
- Actualicé `NavMenu.razor` agregando el enlace "Procedimientos".
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos.

### Prompt Record
**User Prompt:**
> Módulo 12 de Fase 6 (Módulos del Negocio): Facturación — implementar siguiendo el mismo
> criterio que los módulos anteriores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`20f0364 Modulo 11 integrado`).
- Revisé el dominio y persistencia de Facturación (Fase 2/3, ya existentes): `Factura`
  (Aggregate Root, con `FacturaDetalle` y `Pago` como Entities internas) y `SecuenciaComprobante`
  (Aggregate Root independiente para los rangos NCF/e-CF de la DGII). Confirmé que
  `FacturaTotalesSaveChangesInterceptor` (Fase 3) ya sincroniza Subtotal/Total automáticamente —
  no hizo falta tocarlo.
- Identifiqué que `RegistrarPago`/`AnularPago` YA EXISTEN en el mismo agregado `Factura` desde
  Fase 2, pero corresponden al Módulo 13 (Pagos) del roadmap — deliberadamente no los expuse en
  `IFacturaService`, mismo criterio que ya aplicamos con Historia Clínica en el Módulo 9.
- Creé `RepositorioFactura` (Include de Detalles y Pagos, mismo patrón que RepositorioDoctor/
  RepositorioHistorialClinico) y lo registré en `PersistenceServiceCollectionExtensions.cs`.
- Creé `Application/Facturacion/` con dos servicios: `ISecuenciaComprobanteService` (autorizar/
  desactivar secuencias, y consumir el siguiente número) e `IFacturaService` (emitir, agregar
  detalle con trazabilidad opcional a un `ProcedimientoRealizado` del Módulo 11 —validando que no
  se facture dos veces el mismo procedimiento—, aplicar descuento/impuestos, asignar número de
  comprobante, actualizar estado DGII manualmente, anular, eliminar).
- Cuidé la atomicidad de "asignar comprobante": `TomarSiguienteNumeroAsync` NO guarda por su
  cuenta — deja el número consumido trackeado en el mismo DbContext, y `FacturaService` recién
  guarda después de asignarlo a la Factura, así ambas mutaciones se confirman juntas en una sola
  transacción.
- Registré ambos servicios en `ApplicationServiceCollectionExtensions.cs`.
- Creé `Web/Components/Pages/Facturas/` (Index buscador de pacientes, Detalle lista de facturas
  del paciente, Nueva para emitir, FacturaDetalle con todas las acciones de esta factura
  individual) y `Web/Components/Pages/SecuenciasComprobante/` (Index/Nuevo, administración mínima
  de rangos NCF/e-CF).
- Actualicé `NavMenu.razor` agregando los enlaces "Facturas" y "Secuencias de Comprobante".
- Documenté explícitamente que `ActualizarEstadoDGIIAsync` es una transición manual — no hay
  integración real con el webservice de la DGII, eso sería un proyecto de integración aparte.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos. Dado el tamaño de este módulo, además del build te
  recomiendo probar el flujo completo en caliente: emitir una factura, agregar un detalle desde un
  procedimiento realizado (verifica que no puedas facturarlo dos veces), aplicar descuento e
  impuestos, autorizar una secuencia de comprobante y asignarla, y anular la factura.

### Prompt Record
**User Prompt:**
> Módulo 13 de Fase 6 (Módulos del Negocio): Pagos — implementar siguiendo el mismo criterio que
> los módulos anteriores.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`a70a3d4 Modulo 12 listo`).
- Confirmé que `Pago` ya vive completo dentro del agregado `Factura` desde Fase 2 —
  `RegistrarPago`/`AnularPago` con todas sus invariantes ya resueltas (no pagar de más, no pagar
  una factura anulada o ya pagada, `RecalcularEstadoPorPagos` actualizando el estado
  automáticamente). No hizo falta tocar Domain ni Persistence — `RepositorioFactura` (Módulo 12)
  ya cargaba `Pagos` con `Include`.
- Creé `Application/Facturacion/IPagoService.cs`/`PagoService.cs`, exponiendo únicamente
  `RegistrarAsync`/`AnularAsync` (más los listados) — sin `ActualizarAsync` ni `EliminarAsync` a
  propósito, porque el dominio exige corregir un pago anulándolo y registrando uno nuevo, nunca
  editando el monto.
- Registré el servicio en `ApplicationServiceCollectionExtensions.cs`.
- Extendí `Web/Components/Pages/Facturas/FacturaDetalle.razor` (Módulo 12) con la sección de
  Pagos: listado con estado (activo/anulado) y quién lo registró, formulario para registrar uno
  nuevo (oculto si la factura ya está `Pagada` o anulada), y botón para anular cada pago activo.
- No hubo cambios en `NavMenu.razor` — Pago no tiene navegación propia, igual que
  ProcedimientoRealizado (Módulo 11), vive dentro de la página de su factura.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos. Te recomiendo probar: registrar un pago parcial (verifica
  que la factura pase a "Parcialmente pagada"), completar el saldo (verifica que pase a
  "Pagada" y el formulario de registrar pago desaparezca), anular un pago (verifica que el estado
  de la factura se recalcule hacia atrás), e intentar pagar de más del saldo pendiente (debe
  rechazarlo).

  
### Prompt Record
**User Prompt:**
> Módulo 14 de Fase 6 (Módulos del Negocio): Reportes — implementar siguiendo el mismo criterio
> que los módulos anteriores. Último módulo de la Fase 6.

**Action Taken / Agent Summary:**
- Actualicé la copia local a `origin/Dev` (`6e2fb25`) y confirmé que Pagos (Módulo 13) quedó
  integrado correctamente.
- Confirmé que el roadmap no describe ningún agregado ni tabla propia para "Reportes" — a
  diferencia de los 13 módulos anteriores, este es 100% de solo lectura sobre datos que ya viven
  en Facturación, Citas, Procedimientos y Pacientes. No hubo cambios en Domain ni Persistence.
- Creé `Application/Reportes/IReporteService.cs`/`ReporteService.cs` con 4 reportes: Ingresos
  (sobre `Factura`), Citas (sobre `Cita`), Procedimientos más realizados (sobre
  `ProcedimientoRealizado`, anidado dentro de `HistorialClinico`) y Pacientes nuevos (sobre
  `Paciente.FechaCreacion`, disponible automáticamente vía `AuditableEntity` desde Fase 2).
- Documenté explícitamente una limitación real de rendimiento en el reporte de Procedimientos:
  requiere cargar los agregados `HistorialClinico` completos de toda la clínica para poder
  agregar en memoria, porque `ProcedimientoRealizado` no es consultable como tabla plana —
  mismo patrón "traer y agregar en memoria" que usa el resto de los servicios de Fase 6, dejado
  anotado como candidato a optimización futura, no resuelto aquí.
- Registré el servicio en `ApplicationServiceCollectionExtensions.cs`.
- Creé `Web/Components/Pages/Reportes/Index.razor` — un dashboard con selector de rango de
  fechas y los 4 reportes en tarjetas.
- Actualicé `NavMenu.razor` agregando el enlace "Reportes".
- Con este módulo, **la Fase 6 (Módulos del Negocio) completa sus 14 módulos con base
  funcional** — queda pendiente la segunda pasada de ajustes que ya conversamos, contrastando
  cada módulo contra tu levantamiento de datos.
- No pude correr `dotnet build` en este entorno (sin SDK .NET disponible) — pendiente de tu
  confirmación tras pegar los archivos.