# 🎓 ImpulsaDBA - Plataforma Académica

![Lenguaje](https://img.shields.io/badge/Lenguaje-ASP.NET%20Core%208.0-512BD4?style=for-the-badge&logo=dotnet)
![Frontend](https://img.shields.io/badge/Frontend-Blazor%20WebAssembly-512BD4?style=for-the-badge&logo=dotnet)
![Base de Datos](https://img.shields.io/badge/Base%20de%20Datos-SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server)
![Estado](https://img.shields.io/badge/Estado-Desarrollo-%2328A745?style=for-the-badge)


Plataforma académica desarrollada con **Blazor WebAssembly** y **ASP.NET Core Web API** para la gestión de actividades académicas, calendarios, asignaturas y recursos educativos.

---

## 🎯 Descripción

**ImpulsaDBA** es una plataforma académica completa que permite a los docentes:

- ✅ Gestionar actividades académicas (videos, evaluaciones, asignaciones, materiales de apoyo, etc.)
- ✅ Visualizar y organizar actividades en un calendario interactivo
- ✅ Administrar asignaturas y grupos de estudiantes
- ✅ Recuperar contraseñas de forma segura
- ✅ Acceder a ayudas y recursos educativos

La aplicación está construida con una arquitectura moderna separando el frontend (Blazor WebAssembly) del backend (ASP.NET Core Web API), comunicándose mediante HTTP REST.

---

## 🏗️ Arquitectura

El proyecto sigue una arquitectura en capas:

```
┌─────────────────────────────────────────┐
│   ImpulsaDBA.Client (Blazor WASM)      │
│   - Componentes UI                    │
│   - Servicios HTTP                     │
│   - Autenticación                      │
└──────────────┬──────────────────────────┘
               │ HTTP REST
┌──────────────▼──────────────────────────┐
│   ImpulsaDBA.API (ASP.NET Core)        │
│   - Controllers                         │
│   - Services (Lógica de Negocio)       │
│   - DatabaseService                    │
└──────────────┬──────────────────────────┘
               │ SQL
┌──────────────▼──────────────────────────┐
│   SQL Server Database                   │
│   - col.persona                         │
│   - tab.recurso                         │
│   - tab.asignacion_academica_recurso    │
│   - bas.ayuda                           │
└─────────────────────────────────────────┘
```

### Componentes Principales

- **Frontend (Blazor WebAssembly)**: Interfaz de usuario reactiva y PWA
- **Backend (ASP.NET Core API)**: API REST 
- **Base de Datos**: SQL Server con múltiples esquemas (col, tab, bas, seg, aca)
- **Autenticación**: Sistema de autenticación personalizado con BCrypt para hash de contraseñas

---

## 📦 Requisitos Previos

Antes de instalar, asegúrate de tener instalado:

- **.NET 8.0 SDK** o superior
  - Descarga: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verifica la instalación: `dotnet --version`

- **SQL Server** (2022)
  - Puede ser SQL Server local, remoto o en una máquina virtual
  - Asegúrate de tener credenciales de acceso

- **Git** (opcional, para clonar el repositorio)

- **Editor de código** (recomendado: Visual Studio Code, Visual Studio, o Rider)

---

## 🚀 Instalación

### Paso 1: Clonar o Descargar el Proyecto

```bash
# Si tienes Git
git clone <https://github.com/jcristancho2/ImpulsaDBA-Plataforma-Acad-mica>
cd ImpulsaDBA-Plataforma-Acad-mica

# O descarga el proyecto como ZIP y extrae
```

### Paso 2: Restaurar Dependencias

```bash
# Restaurar paquetes NuGet para todos los proyectos
dotnet restore
```

Esto descargará automáticamente todas las dependencias necesarias

### Paso 3: Verificar la Instalación

```bash
# Verificar que los proyectos se pueden compilar
dotnet build
```

Si todo está correcto, deberías ver:
```
Build succeeded.
```

---

## ⚙️ Configuración

### 1. Configuración de Base de Datos

La aplicación necesita una cadena de conexión a SQL Server. Tienes dos opciones:

#### Opción A: Usando `appsettings.json` (Recomendado para desarrollo)

Edita el archivo `ImpulsaDBA.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=TU_BASE_DE_DATOS;User Id=TU_USUARIO;Password=TU_CONTRASEÑA;Encrypt=True;TrustServerCertificate=True;"
  }
}
```




#### Opción B: Usando Variables de Entorno (Recomendado para producción)

Crea un archivo `.env` en la raíz del proyecto (o en `ImpulsaDBA.API/`):

```env
DB_SERVER=tu_servidor
DB_NAME=tu_base_de_datos
DB_USER=tu_usuario
DB_PASSWORD=tu_contraseña
DB_PORT=1433
```

**Nota:** El archivo `.env` debe estar en `.gitignore` para no exponer credenciales.

### 2. Configuración del Cliente

Edita `ImpulsaDBA.Client/appsettings.json` para configurar la URL de la API:

```json
{
  "ApiBaseUrl": "https://localhost:5001",
  "BlazorClientUrl": "https://localhost:5079"
}
```

### 3. Configuración para SQL Server Remoto (Máquina Virtual)

Si tu SQL Server está en una máquina virtual (Parallels Desktop, VMware, etc.):

1. **Obtén la IP de la VM:**
   - En Windows, ejecuta: `ipconfig`
   - Busca la dirección IPv4 (ej: `10.211.55.3`)

2. **Configura la cadena de conexión:**
   ```json
   "DefaultConnection": "Server=...,puerto;Database=...;User Id=...;Password=tu_password;Encrypt=True;TrustServerCertificate=True;"
   ```

3. **Habilita conexiones remotas en SQL Server:**
   - Abre SQL Server Configuration Manager
   - Habilita TCP/IP en Protocolos para SQL Server
   - Reinicia el servicio SQL Server
   - Configura el firewall para permitir el puerto 1433

4. **Verifica conectividad**
   


### Prioridad de Configuración

1. Primero se intenta leer desde `appsettings.json` (`ConnectionStrings.DefaultConnection`)
2. Si no está disponible, se construye desde variables de entorno
3. Si ninguna está configurada, se usan valores por defecto (localhost)

---

## ▶️ Ejecución

### Opción 1: Script Automático (Recomendado)

```bash
# Dar permisos de ejecución (solo la primera vez)
chmod +x ejecutar.sh

# Ejecutar ambos proyectos
./ejecutar.sh
```

El script ejecutará automáticamente:
- ✅ API en `https://localhost:5001`
- ✅ Cliente en `https://localhost:5079`

**Para detener:** Presiona `Ctrl+C`

### Opción 2: Ejecutar Manualmente

#### Terminal 1 - API Backend

```bash
cd ImpulsaDBA.API
dotnet run
```

**URLs de la API:**
- HTTP: `http://localhost:5001`

#### Terminal 2 - Cliente Blazor

```bash
cd ImpulsaDBA.Client
dotnet run
```

**URLs del Cliente:**
- HTTP: `http://localhost:5079`

### ⚠️ Orden de Ejecución

**IMPORTANTE:** Siempre ejecuta primero la API y luego el Cliente.

1. **Primero:** API Backend (`ImpulsaDBA.API`)
2. **Segundo:** Cliente Blazor (`ImpulsaDBA.Client`)

El Cliente necesita que la API esté corriendo para funcionar correctamente.

### 🔗 URLs de Acceso

Una vez que ambos servicios estén ejecutándose:

- **Cliente (Aplicación Principal):** `https://localhost:5079`
- **API Backend:** `https://localhost:5001`


### 🛑 Detener los Servicios

- Si usaste el script: Presiona `Ctrl+C`
- Si ejecutaste manualmente: Presiona `Ctrl+C` en cada terminal

---


## 📁 Estructura del Proyecto

```
ImpulsaDBA-Plataforma-Acad-mica/
│
├── ImpulsaDBA.API/                    # Backend - ASP.NET Core Web API
│   ├── Application/
│   │   └── Services/                 # Lógica de negocio
│   │       ├── AuthService.cs        # Autenticación y recuperación de contraseña
│   │       ├── CalendarioService.cs  # Gestión de actividades y calendario
│   │       ├── AsignaturaService.cs  # Gestión de asignaturas
│   │       └── AyudaService.cs       # Gestión de ayudas y recursos
│   │
│   ├── Controllers/                  # Controladores REST API
│   │   ├── AuthController.cs         # Endpoints de autenticación
│   │   ├── CalendarioController.cs   # Endpoints de calendario
│   │   ├── AsignaturasController.cs  # Endpoints de asignaturas
│   │   ├── AyudasController.cs      # Endpoints de ayudas
│   │   └── HealthController.cs       # Health checks
│   │
│   ├── Infrastructure/
│   │   ├── Database/
│   │   │   └── DatabaseService.cs    # Servicio de acceso a datos
│   │   └── Repositories/
│   │       └── UsuarioRepository.cs # Repositorio de usuarios
│   │
│   ├── Domain/
│   │   └── Entities/                # Entidades del dominio
│   │
│   ├── Program.cs                    # Configuración de la aplicación
│   └── appsettings.json             # Configuración (cadena de conexión)
│
├── ImpulsaDBA.Client/                # Frontend - Blazor WebAssembly
│   ├── Components/
│   │   ├── Calendario/              # Componentes de calendario
│   │   │   ├── CalendarioComponent.razor
│   │   │   └── CreacionActividades/  # Modales de creación de actividades
│   │   │       ├── VideoDeEnganche.razor
│   │   │       ├── Evaluacion.razor
│   │   │       ├── Asignaciones.razor
│   │   │       ├── MaterialApoyo.razor
│   │   │       ├── PreguntasProblematizadoras.razor
│   │   │       ├── RecursoInteractivo.razor
│   │   │       └── ClaseVirtual.razor
│   │   │
│   │   ├── Layout/                  # Layouts de la aplicación
│   │   │   ├── DashboardLayout.razor
│   │   │   ├── MainLayout.razor
│   │   │   └── NavMenu.razor
│   │   │
│   │   └── Shared/                 # Componentes compartidos
│   │       ├── Ayudas.razor
│   │       ├── CardAsignatura.razor
│   │       ├── ModalActividad.razor
│   │       └── ModalOlvideContrasena.razor
│   │
│   ├── Pages/                       # Páginas principales
│   │   ├── Login.razor             # Página de inicio de sesión
│   │   ├── Dashboard.razor         # Dashboard principal
│   │   ├── Inicio.razor            # Página de inicio
│   │   ├── MisMaterias.razor        # Vista de asignaturas
│   │   └── Calendario.razor        # Vista de calendario
│   │
│   ├── Services/                    # Servicios del cliente
│   │   ├── AuthService.cs          # Servicio de autenticación
│   │   ├── CalendarioService.cs    # Servicio de calendario
│   │   ├── AsignaturaService.cs    # Servicio de asignaturas
│   │   └── AyudaService.cs         # Servicio de ayudas
│   │
│   ├── Auth/
│   │   └── CustomAuthStateProvider.cs  # Proveedor de autenticación
│   │
│   ├── Program.cs                   # Configuración del cliente
│   └── wwwroot/                     # Archivos estáticos (CSS, JS, imágenes)
│
├── ImpulsaDBA.Shared/               # Proyecto compartido
│   ├── DTOs/                       # Data Transfer Objects
│   ├── Requests/                   # Modelos de request
│   ├── Responses/                  # Modelos de response
│   └── Enums/                     # Enumeraciones
│
├── ejecutar.sh                     # Script de ejecución automática
└── README.md                       # Este archivo
```

---

## 📚 Documentación de Componentes

### Backend (API)

#### Controllers

- **`AuthController`**: Maneja autenticación y recuperación de contraseña
  - `POST /api/auth/login` - Iniciar sesión
  - `POST /api/auth/validar-informacion-recuperacion` - Validar información para recuperar contraseña
  - `POST /api/auth/cambiar-contrasena` - Cambiar contraseña

- **`CalendarioController`**: Gestión de actividades y calendario
  - `GET /api/calendario/asignacion/{id}/fecha` - Obtener actividades por fecha
  - `GET /api/calendario/asignacion/{id}/mes` - Obtener actividades por mes
  - `POST /api/calendario/crear` - Crear nueva actividad
  - `PUT /api/calendario/actualizar` - Actualizar actividad
  - `DELETE /api/calendario/eliminar/{id}` - Eliminar actividad

- **`AsignaturasController`**: Gestión de asignaturas
  - `GET /api/asignaturas/profesor/{id}` - Obtener asignaturas por profesor
  - `GET /api/asignaturas/grupo/{id}/estudiantes` - Obtener cantidad de estudiantes

- **`AyudasController`**: Gestión de ayudas
  - `GET /api/ayudas/componente/{id}` - Obtener ayudas por componente

#### Services

- **`AuthService`**: Lógica de autenticación y validación de credenciales
  - Valida usuarios por email, cédula o celular
  - Verifica contraseñas con BCrypt o número de documento (legacy)
  - Gestiona recuperación de contraseña

- **`CalendarioService`**: Lógica de negocio para actividades
  - Crea, actualiza y elimina actividades
  - Obtiene actividades por fecha, mes o asignación
  - Valida permisos de edición/eliminación

- **`AsignaturaService`**: Lógica de negocio para asignaturas
  - Obtiene asignaturas por profesor
  - Calcula estadísticas de actividades

- **`DatabaseService`**: Servicio de acceso a datos
  - Ejecuta consultas SQL de forma segura
  - Maneja conexiones a SQL Server

### Frontend (Client)

#### Páginas Principales

- **`Login.razor`**: Página de inicio de sesión
  - Autenticación con email/cédula/celular
  - Recuperación de contraseña

- **`Dashboard.razor`**: Dashboard principal con navegación

- **`Inicio.razor`**: Página de inicio con resumen de actividades

- **`MisMaterias.razor`**: Vista de asignaturas del profesor

- **`Calendario.razor`**: Vista de calendario con actividades

#### Componentes Clave

- **`CalendarioComponent.razor`**: Componente principal del calendario
  - Vista mensual, semanal y diaria
  - Etiquetas de actividades con colores por tipo
  - Interacción con actividades (editar, eliminar)

- **Modales de Creación de Actividades**:
  - `VideoDeEnganche.razor` - Videos de enganche
  - `Evaluacion.razor` - Evaluaciones
  - `Asignaciones.razor` - Asignaciones
  - `MaterialApoyo.razor` - Material de apoyo
  - `PreguntasProblematizadoras.razor` - Preguntas problematizadoras
  - `RecursoInteractivo.razor` - Recursos interactivos
  - `ClaseVirtual.razor` - Clases virtuales

- **`ModalOlvideContrasena.razor`**: Modal para recuperar contraseña

- **`Ayudas.razor`**: Componente de ayudas con PDFs y videos

---

## 💻 Uso de la Aplicación

### Iniciar Sesión

1. Abre la aplicación en `https://localhost:5079`
2. Ingresa tu usuario (puede ser email, cédula o celular)
3. Ingresa tu contraseña
4. Haz clic en "Iniciar Sesión"

### Usuarios de Prueba

Para realizar pruebas de la aplicación, puedes utilizar los siguientes usuarios:

| Cédula | Correo | Celular | Contraseña |
|--------|--------|---------|------------|
| 1066840516 | ANZOLA.ANDREA@gmail.com | 3225983969 | Tecnoeduca10. |
| 1141315626 | REYES.LUNA@gmail.com | 3197710352 | Tecnoeduca10. |

**Nota:** Puedes iniciar sesión usando cualquiera de los datos de identificación (cédula, correo o celular) junto con la contraseña correspondiente.

### Recuperar Contraseña

1. En la página de login, haz clic en "¿Olvidó su contraseña?"
2. Ingresa tu email, celular y número de documento
3. Si la información es correcta, podrás ingresar una nueva contraseña
4. La nueva contraseña se hashea con BCrypt y se almacena de forma segura

### Crear una Actividad

1. Navega a "Calendario"
2. Selecciona una asignatura y grupo
3. Haz clic en "Agregar Nueva Actividad"
4. Selecciona el tipo de actividad
5. Completa el formulario correspondiente
6. Guarda la actividad

### Editar/Eliminar Actividad

1. En el calendario, haz clic en una etiqueta de actividad
2. Si eres el creador, verás opciones para editar o eliminar
3. Solo el docente que creó la actividad puede editarla o eliminarla

### Ver Actividades

- **Vista Mensual**: Muestra todas las actividades del mes
- **Vista Semanal**: Muestra actividades de la semana
- **Vista Diaria**: Muestra actividades de un día específico con detalles

---

## 🛠️ Tecnologías

### Backend
- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - Framework para API REST
- **Microsoft.Data.SqlClient** - Cliente SQL Server
- **Dapper** - Micro-ORM para acceso a datos
- **BCrypt.Net-Next** - Hash de contraseñas

### Frontend
- **Blazor WebAssembly** - Framework de UI
- **Blazored.LocalStorage** - Almacenamiento local
- **SweetAlert2** - Alertas y diálogos
- **Bootstrap 5** - Framework CSS
- **Bootstrap Icons** - Iconos

### Base de Datos
- **SQL Server** - Base de datos relacional

---

## 🔧 Troubleshooting

### Error: "Connection string 'DefaultConnection' not found"

**Solución:** Asegúrate de que `ImpulsaDBA.API/appsettings.json` tenga la cadena de conexión configurada.

### Error: "Cannot connect to SQL Server"

**Soluciones:**
1. Verifica que SQL Server esté ejecutándose
2. Verifica la cadena de conexión (servidor, base de datos, credenciales)
3. Si es remoto, verifica que el puerto 1433 esté abierto
4. Verifica que TCP/IP esté habilitado en SQL Server Configuration Manager

### Error: "CORS policy"

**Solución:** Verifica que la URL del cliente esté en la lista de orígenes permitidos en `Program.cs` del API.

### Error: "Certificate validation failed"

**Solución:** En desarrollo, acepta la excepción del certificado SSL. En producción, configura certificados válidos.

### La aplicación no carga

**Soluciones:**
1. Verifica que la API esté ejecutándose antes que el cliente
2. Verifica que los puertos no estén ocupados
3. Revisa la consola del navegador para errores
4. Verifica que `ApiBaseUrl` en `appsettings.json` del cliente sea correcta

### No puedo iniciar sesión

**Soluciones:**
1. Verifica que el usuario exista en la base de datos
2. Si cambiaste la contraseña, asegúrate de usar la nueva contraseña
3. Si no has cambiado la contraseña, usa tu número de documento como contraseña
4. Revisa los logs del servidor para ver errores específicos

---

## 📝 Notas Adicionales

- La aplicación es una **PWA (Progressive Web App)**, lo que permite instalarla como una aplicación nativa
- Las contraseñas se hashean con **BCrypt** para mayor seguridad
- El sistema soporta autenticación legacy (número de documento) y nueva (contraseña hasheada)
- Solo el docente que creó una actividad puede editarla o eliminarla
- Las actividades pueden marcarse como visibles o invisibles para estudiantes

---

## 📄 Licencia

Este proyecto es propiedad de TecnoEduca Colombia.

---

## 👥 Contribuidores

- Equipo de desarrollo TecnoEduca Colombia

---


