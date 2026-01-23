# Funcionamiento de la Aplicación ImpulsaDBA

## 📋 Índice

1. [Arquitectura General](#arquitectura-general)
2. [Flujo de Autenticación](#flujo-de-autenticación)
3. [Estructura de Componentes](#estructura-de-componentes)
4. [Servicios Principales](#servicios-principales)
5. [Funcionalidades Principales](#funcionalidades-principales)
6. [Modelos de Datos](#modelos-de-datos)
7. [Flujo de Datos](#flujo-de-datos)
8. [Configuración y Despliegue](#configuración-y-despliegue)

---

## 🏗️ Arquitectura General

### Tecnología Base
- **Framework**: Blazor Server (.NET 8.0)
- **Base de Datos**: SQL Server
- **Patrón**: MVC con componentes Blazor
- **Autenticación**: Custom Authentication State Provider con LocalStorage

### Características Principales
- Aplicación web de tiempo real con SignalR (Blazor Server)
- Interfaz reactiva sin necesidad de JavaScript
- Conexión directa a base de datos SQL Server
- Autenticación basada en sesiones almacenadas en LocalStorage
- Diseño responsive y moderno

---

## 🔐 Flujo de Autenticación

### 1. Página de Login (`/login`)

**Componente**: `Components/Pages/Login.razor`

**Proceso**:
1. El usuario ingresa sus credenciales:
   - **Usuario**: Puede ser correo electrónico, cédula o número de celular
   - **Contraseña**: Siempre es el número de documento

2. **Validación**:
   ```csharp
   AuthService.ValidarLoginCompleto(usuario, password)
   ```
   - Busca el usuario en la tabla `col.persona` por correo, cédula o celular
   - Valida que la contraseña coincida con el número de documento
   - Obtiene el rol activo desde `seg.rol_persona`

3. **Creación de Sesión**:
   - Si las credenciales son válidas, se crea un objeto `SesionUsuario`
   - Se genera un token único (GUID)
   - La sesión se guarda en `LocalStorage` del navegador

4. **Redirección**:
   - Si el login es exitoso, redirige a `/dashboard`
   - Si hay error, muestra mensaje al usuario

### 2. Custom Authentication State Provider

**Componente**: `Services/CustomAuthStateProvider.cs`

**Funcionalidad**:
- Gestiona el estado de autenticación de la aplicación
- Lee la sesión desde `LocalStorage` al cargar la página
- Proporciona información del usuario autenticado a todos los componentes
- Permite cerrar sesión limpiando `LocalStorage`

### 3. Protección de Rutas

**Componente**: `Routes.razor`

- Las rutas protegidas tienen el atributo `[Authorize]`
- Si el usuario no está autenticado, se redirige a `/login`
- El componente `RedirectToLogin` maneja la redirección automática

---

## 📦 Estructura de Componentes

### Layouts

#### 1. **EmptyLayout** (`Components/Layout/EmptyLayout.razor`)
- Layout mínimo sin navegación
- Usado para páginas de login y errores

#### 2. **DashboardLayout** (`Components/Layout/DashboardLayout.razor`)
- Layout principal de la aplicación
- Incluye:
  - **Sidebar**: Menú lateral colapsable con navegación
  - **Header**: Barra superior con información del usuario y búsqueda
  - **Content Area**: Área principal donde se renderizan las páginas

### Páginas Principales

#### 1. **Login** (`Components/Pages/Login.razor`)
- Página de inicio de sesión
- Validación de formulario con DataAnnotations
- Manejo de errores y estados de carga

#### 2. **Dashboard** (`Components/Pages/Dashboard.razor`)
- Página principal después del login
- Muestra todas las asignaturas asignadas al docente
- Cards con información de cada asignatura:
  - Nombre de la asignatura
  - Grupo
  - Cantidad de estudiantes
  - Estadísticas de actividades (totales, activas, inactivas, pendientes)

#### 3. **Mis Asignaturas** (`Components/Pages/MisMaterias.razor`)
- Vista detallada de todas las asignaturas
- Similar al Dashboard pero con más opciones
- Incluye componente de Ayudas

#### 4. **Calendario** (`Components/Pages/Calendario.razor`)
- Vista principal del calendario académico
- **Filtros**:
  - Por grupo (filtra asignaturas según el grupo seleccionado)
  - Por asignatura (filtra actividades según la asignatura)
- **Funcionalidades**:
  - Crear actividades al hacer clic en un día
  - Editar actividades al hacer clic en las etiquetas
  - Validación: solo permite crear actividades para días futuros (desde mañana)
- **Modales de creación**:
  - Video de Enganche
  - Preguntas Problematizadoras
  - Recurso Interactivo
  - Asignaciones (Tareas, Trabajos, Talleres, etc.)
  - Material de Apoyo
  - Clase Virtual

#### 5. **Inicio** (`Components/Pages/Inicio.razor`)
- Página de bienvenida
- Accesos rápidos a secciones principales
- Componente de Ayudas

### Componentes Compartidos

#### 1. **CalendarioComponent** (`Components/Shared/CalendarioComponent.razor`)
- Componente principal del calendario
- **Vistas disponibles**:
  - Semanal
  - Mensual
  - Anual
- **Características**:
  - Navegación entre meses/semanas
  - Etiquetas de actividades con colores:
    - `--color-pastel-ocean`: Para videos de enganche y asignaciones
    - `--color-pastel-rose`: Para preguntas problematizadoras, material de apoyo y clase virtual
  - Texto truncado a 3 letras por palabra
  - Tooltip con nombre completo al hacer hover
  - Responsive para diferentes tamaños de pantalla

#### 2. **CardAsignatura** (`Components/Shared/CardAsignatura.razor`)
- Tarjeta que muestra información de una asignatura
- Diseño con color personalizado por asignatura
- Muestra estadísticas de actividades con efecto blur

#### 3. **ModalActividad** (`Components/Shared/ModalActividad.razor`)
- Modal principal para seleccionar tipo de actividad
- Al hacer clic en un tipo, se abre el modal específico correspondiente
- Validación de fecha (solo días futuros)

#### 4. **Ayudas** (`Components/Shared/Ayudas.razor`)
- Componente de ayuda contextual
- Dropdown con opciones de PDF y Video
- Aparece en todas las páginas principales a la derecha del header

### Componentes de Creación de Actividades

Todos los modales de creación están en `Components/Pages/CreacionActividades/`:

1. **VideoDeEnganche.razor**: Para videos de enganche
2. **PreguntasProblematizadoras.razor**: Para preguntas problematizadoras
3. **RecursoInteractivo.razor**: Para recursos interactivos
4. **Asignaciones.razor**: Para tareas, trabajos, talleres, investigaciones, proyectos y actividades prácticas
5. **MaterialApoyo.razor**: Para presentaciones, documentos, recursos de lectura y resúmenes de clase
6. **ClaseVirtual.razor**: Para clases virtuales

**Características comunes**:
- Todos soportan modo edición (cuando se hace clic en una actividad existente)
- Validación de fecha (solo días futuros)
- EventCallback `OnActividadGuardada` para notificar al componente padre
- Parámetro `ActividadEditar` para cargar datos existentes

---

## 🔧 Servicios Principales

### 1. **DatabaseService** (`Services/DatabaseService.cs`)

**Responsabilidad**: Gestión de conexiones y consultas a la base de datos SQL Server

**Métodos principales**:
- `ExecuteQueryAsync()`: Ejecuta consultas SELECT y retorna DataTable
- `ExecuteScalarAsync()`: Ejecuta consultas que retornan un solo valor
- `ExecuteNonQueryAsync()`: Ejecuta INSERT, UPDATE, DELETE

**Configuración**:
- Lee la cadena de conexión desde `appsettings.json` o variables de entorno
- Singleton para mantener una única conexión

### 2. **AuthService** (`Services/AuthService.cs`)

**Responsabilidad**: Autenticación y validación de usuarios

**Métodos principales**:
- `ValidarCredencialesUsuario()`: Busca usuario por correo, cédula o celular
- `ValidarCredencialesPassword()`: Valida que la contraseña coincida con el número de documento
- `ValidarLoginCompleto()`: Valida usuario y contraseña en conjunto

**Flujo**:
1. Busca el usuario en `col.persona`
2. Obtiene el rol activo desde `seg.rol_persona`
3. Valida que la contraseña sea el número de documento
4. Retorna objeto `Usuario` con toda la información

### 3. **AsignaturaService** (`Services/AsignaturaService.cs`)

**Responsabilidad**: Gestión de asignaturas y grupos

**Métodos principales**:
- `ObtenerAsignaturasPorProfesor()`: Obtiene todas las asignaturas asignadas a un profesor
- `ObtenerPeriodoActual()`: Obtiene el periodo académico actual
- `ObtenerEstadisticasActividades()`: Calcula estadísticas de actividades por asignación académica

**Consulta principal**:
- Utiliza `plla.View_Asignacion_Academica` para obtener asignaciones
- Une con tablas: `persona`, `grupo`, `sede`, `anio`, `colegio`, `asignatura`
- Asigna colores pastel únicos a cada asignatura

### 4. **CalendarioService** (`Services/CalendarioService.cs`)

**Responsabilidad**: Gestión de actividades del calendario

**Métodos principales**:
- `ObtenerActividadesPorFecha()`: Obtiene actividades para una fecha específica
- `ObtenerActividadesPorMes()`: Obtiene todas las actividades de un mes

**Lógica**:
1. Obtiene año y periodo desde la asignación académica
2. Consulta actividades en `bas.actividad` que coincidan con:
   - El año y periodo de la asignación
   - La fecha especificada (o rango de fechas)
   - Que no estén eliminadas (`eliminado = 0`)

### 5. **CustomAuthStateProvider** (`Services/CustomAuthStateProvider.cs`)

**Responsabilidad**: Gestión del estado de autenticación

**Métodos principales**:
- `GetAuthenticationStateAsync()`: Obtiene el estado actual de autenticación
- `GuardarSesionAsync()`: Guarda la sesión en LocalStorage
- `CerrarSesionAsync()`: Limpia la sesión y redirige al login

**Implementación**:
- Implementa `AuthenticationStateProvider` de Blazor
- Usa `Blazored.LocalStorage` para persistencia
- Crea claims del usuario desde la sesión guardada

### 6. **UsuarioRepository** (`Services/UsuarioRepository.cs`)

**Responsabilidad**: Acceso a datos de usuarios

**Métodos principales**:
- `ObtenerPorId()`: Obtiene un usuario por su ID
- Operaciones CRUD básicas para usuarios

---

## ⚙️ Funcionalidades Principales

### 1. Gestión de Asignaturas

**Flujo**:
1. Al iniciar sesión, se cargan todas las asignaturas del docente
2. Se muestran en cards con información resumida
3. Al hacer clic en una card, se navega al calendario filtrado por esa asignatura
4. Se puede filtrar por grupo para ver solo asignaturas de un grupo específico

**Datos mostrados**:
- Nombre de la asignatura
- Grupo
- Cantidad de estudiantes
- Estadísticas de actividades

### 2. Calendario Académico

**Vistas**:
- **Semanal**: Muestra 7 días con actividades
- **Mensual**: Vista de calendario tradicional
- **Anual**: Vista de todo el año

**Interacciones**:
- **Crear actividad**: Clic en un día → Seleccionar tipo → Llenar formulario → Guardar
- **Editar actividad**: Clic en etiqueta de actividad → Abrir modal con datos → Modificar → Guardar
- **Filtros**: Por grupo y por asignatura

**Validaciones**:
- Solo se pueden crear actividades para días futuros (desde mañana)
- Las fechas se validan tanto en el cliente como en el servidor

### 3. Creación de Actividades

**Tipos de actividades** (según `tab.tipo_recurso`):

1. **Video de Enganche** (ID: 1)
   - Campos: Título, Descripción, Fecha de publicación, URLs de videos

2. **Preguntas Problematizadoras** (ID: 2)
   - Campos: Título, Descripción, Fecha de publicación, Preguntas

3. **Recurso Interactivo** (ID: 3, 10)
   - Campos: Título, Descripción, Fecha de publicación, Thumbnails

4. **Asignaciones** (ID: 4-9)
   - Tareas, Trabajos, Talleres, Investigaciones, Proyectos, Actividades Prácticas
   - Campos: Título, Descripción, Fecha de publicación, Instrucciones

5. **Material de Apoyo** (ID: 11-13, 16)
   - Presentaciones, Documentos, Recursos de Lectura, Resúmenes de Clase
   - Campos: Título, Descripción, Fecha de publicación, Archivos

6. **Clase Virtual** (ID: 14)
   - Campos: Título, Descripción, Fecha y hora, Enlace de videoconferencia

**Proceso de creación**:
1. Usuario hace clic en un día del calendario
2. Se abre modal de selección de tipo
3. Usuario selecciona tipo (se abre modal específico directamente)
4. Usuario llena formulario
5. Al guardar, se crea registro en `bas.actividad`
6. El calendario se recarga automáticamente

### 4. Sistema de Ayudas

**Componente**: `Ayudas.razor`

**Funcionalidad**:
- Dropdown con opciones de ayuda (PDF, Video)
- Aparece en todas las páginas principales
- Posicionado a la derecha del header
- Los enlaces se abren en nueva pestaña

**Estado actual**:
- Preparado para recibir URLs desde base de datos
- Por ahora muestra "No hay ayudas disponibles" si no hay datos

### 5. Filtros y Búsqueda

**Filtro por Grupo**:
- Extrae grupos únicos de las asignaturas del docente
- Al seleccionar un grupo, filtra las asignaturas disponibles
- Actualiza automáticamente el filtro de asignaturas

**Filtro por Asignatura**:
- Muestra solo las asignaturas del grupo seleccionado (si hay filtro de grupo)
- Al seleccionar una asignatura, filtra las actividades del calendario
- Se puede seleccionar "Todas las asignaturas" para ver todas

---

## 📊 Modelos de Datos

### 1. **Usuario** (`Models/Usuario.cs`)
```csharp
- Id: int
- NombreCompleto: string
- Email: string
- Perfil: string (rol del usuario)
- FotoUrl: string
```

### 2. **Asignatura** (`Models/Asignatura.cs`)
```csharp
- Id: int
- IdAsignacionAcademica: int
- Nombre: string
- GrupoId: int
- Grupo: string
- Institucion: string
- ColorHex: string (color único para la card)
- CantidadEstudiantes: int
- Estadisticas: EstadisticasActividades
```

### 3. **ActividadCalendario** (`Models/ActividadCalendario.cs`)
```csharp
- Id: int
- TipoActividad: string
- IdTipoActividad: int
- FechaInicio: DateTime
- FechaFinal: DateTime?
- Nombre: string
```

### 4. **EstadisticasActividades** (`Models/EstadisticasActividades.cs`)
```csharp
- Totales: int
- Activas: int
- Inactivas: int
- Pendientes: int
```

### 5. **TipoActividad** (`Models/TipoActividad.cs`)
```csharp
- Id: int
- Nombre: string
```

---

## 🔄 Flujo de Datos

### Flujo General

```
Usuario → Login → AuthService → DatabaseService → SQL Server
                ↓
         CustomAuthStateProvider → LocalStorage
                ↓
         Dashboard → AsignaturaService → DatabaseService → SQL Server
                ↓
         Calendario → CalendarioService → DatabaseService → SQL Server
                ↓
         Crear/Editar Actividad → DatabaseService → SQL Server
                ↓
         Recargar Calendario
```

### Flujo de Autenticación

```
1. Usuario ingresa credenciales en Login.razor
2. Login.razor llama a AuthService.ValidarLoginCompleto()
3. AuthService consulta DatabaseService
4. DatabaseService ejecuta query en SQL Server
5. Si es válido, se crea SesionUsuario
6. CustomAuthStateProvider guarda sesión en LocalStorage
7. Se redirige a /dashboard
8. DashboardLayout lee sesión desde LocalStorage
9. Se muestra información del usuario autenticado
```

### Flujo de Carga de Asignaturas

```
1. Dashboard.razor se inicializa
2. Obtiene ID del profesor desde AuthenticationState
3. Llama a AsignaturaService.ObtenerAsignaturasPorProfesor()
4. AsignaturaService consulta DatabaseService
5. DatabaseService ejecuta query compleja con JOINs
6. Se mapean resultados a objetos Asignatura
7. Se asignan colores únicos a cada asignatura
8. Se calculan estadísticas de actividades
9. Se renderizan cards en la UI
```

### Flujo de Creación de Actividad

```
1. Usuario hace clic en día del calendario
2. Se abre ModalActividad
3. Usuario selecciona tipo de actividad
4. Se abre modal específico (ej: VideoDeEnganche)
5. Usuario llena formulario
6. Al guardar, se crea FormActividadDto
7. Se envía a CalendarioService (o servicio específico)
8. Se ejecuta INSERT en bas.actividad
9. Se dispara OnActividadGuardada
10. CalendarioComponent recarga actividades
11. Se actualiza la UI
```

---

## 🚀 Configuración y Despliegue

### Configuración de Base de Datos

**Método 1: appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ImpulsaDBA;User Id=sa;Password=...;TrustServerCertificate=true;"
  }
}
```

**Método 2: Variables de Entorno (.env)**
```
DB_SERVER=10.211.55.3
DB_NAME=ImpulsaDBA
DB_USER=sa
DB_PASSWORD=...
DB_PORT=1433
```

### Estructura de Base de Datos

**Tablas principales**:
- `col.persona`: Información de usuarios
- `col.asignacion_academica`: Asignaciones de profesores a grupos
- `aca.grupo`: Grupos académicos
- `bas.actividad`: Actividades del calendario
- `bas.t_actividad`: Tipos de actividades
- `seg.rol_persona`: Roles de usuarios
- `seg.rol`: Catálogo de roles

**Vistas**:
- `plla.View_Asignacion_Academica`: Vista de asignaciones académicas

### Dependencias Principales

```xml
<PackageReference Include="Blazored.LocalStorage" Version="4.3.0" />
<PackageReference Include="CurrieTechnologies.Razor.SweetAlert2" Version="5.6.0" />
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
<PackageReference Include="DotNetEnv" Version="3.1.1" />
```

### Características de Seguridad

1. **Autenticación**: Basada en sesiones almacenadas en LocalStorage
2. **Autorización**: Rutas protegidas con `[Authorize]`
3. **Validación**: Validación de formularios con DataAnnotations
4. **SQL Injection**: Prevenido con parámetros en consultas
5. **HTTPS**: Redirección forzada en producción

---

## 📝 Notas Adicionales

### Convenciones de Nomenclatura

- **Componentes**: PascalCase (ej: `CalendarioComponent.razor`)
- **Servicios**: PascalCase con sufijo "Service" (ej: `AsignaturaService.cs`)
- **Modelos**: PascalCase (ej: `Asignatura.cs`)
- **Métodos**: PascalCase (ej: `ObtenerAsignaturasPorProfesor()`)
- **Variables privadas**: camelCase (ej: `asignaturas`)

### Estilos CSS

- Variables CSS en `wwwroot/css/app.css`
- Colores pastel definidos como variables
- Diseño responsive con media queries
- Estilos específicos en cada componente `.razor`

### Mejoras Futuras Sugeridas

1. Implementar UnitOfWork para transacciones
2. Agregar caché para consultas frecuentes
3. Implementar sistema de notificaciones
4. Agregar exportación de calendario (PDF, Excel)
5. Implementar búsqueda avanzada de actividades
6. Agregar sistema de comentarios en actividades
7. Implementar notificaciones push para actividades próximas

---

## 🔍 Troubleshooting

### Problemas Comunes

1. **Error de conexión a BD**:
   - Verificar cadena de conexión en `appsettings.json` o `.env`
   - Verificar que SQL Server esté ejecutándose
   - Verificar firewall y puertos

2. **Usuario no puede iniciar sesión**:
   - Verificar que el usuario exista en `col.persona`
   - Verificar que tenga rol activo en `seg.rol_persona`
   - Verificar que la contraseña sea el número de documento

3. **Actividades no aparecen en calendario**:
   - Verificar que la asignación académica tenga año y periodo activo
   - Verificar que las actividades no estén marcadas como eliminadas
   - Verificar que las fechas coincidan con el rango del calendario

---

**Última actualización**: Enero 2026
**Versión**: 1.0
