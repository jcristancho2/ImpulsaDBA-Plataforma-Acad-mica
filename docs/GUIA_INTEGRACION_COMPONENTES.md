# Guía de Integración de Componentes

## 📋 Proceso para Integrar un Componente y Renderizarlo desde el Sidebar en el Dashboard

Esta guía explica paso a paso cómo integrar un componente nuevo que se renderiza dentro del Dashboard cuando se hace clic en un botón del sidebar.

---

## 🎯 **Paso 1: Preparar el Componente**

- [ ] Revisar que el componente esté completo y funcional
- [ ] Verificar que no tenga dependencias externas no documentadas
- [ ] Asegurar que el código esté limpio y comentado si es necesario

---

## 📁 **Paso 2: Copiar el Componente a la Carpeta Shared**

El componente debe ubicarse en `Components/Shared/` ya que se renderizará dentro del Dashboard.

- [ ] Copiar el archivo `.razor` del componente a `ImpulsaDBA/Components/Shared/`
- [ ] Si el componente tiene un archivo `.razor.css` (estilos), copiarlo también
- [ ] Si tiene archivos JavaScript (`.js`), copiarlos a `wwwroot/js/`

**Estructura esperada:**
```
ImpulsaDBA/
  Components/
    Shared/
      PlantillaNotas.razor          ← Tu componente aquí
      PlantillaNotas.razor.css      ← Estilos (opcional)
  wwwroot/
    js/
      plantilla-notas.js            ← JavaScript (si aplica)
```

---

## 🔧 **Paso 3: Ajustar el Namespace del Componente**

Abrir el archivo del componente y asegurar que tenga el namespace correcto:

```razor
@namespace ImpulsaDBA.Components.Shared
@using ImpulsaDBA.Models
@using ImpulsaDBA.Services

<!-- Contenido del componente -->
<div class="plantilla-notas">
    <!-- Tu código aquí -->
</div>
```

---

## 🔌 **Paso 4: Verificar Dependencias y Servicios**

- [ ] Revisar qué servicios inyecta el componente (ej: `@inject CalendarioService`)
- [ ] Confirmar que los servicios existan en el proyecto
- [ ] Si el componente necesita nuevos servicios, registrarlos en `Program.cs`:

**Archivo:** `ImpulsaDBA/Program.cs`
```csharp
// Agregar después de los otros servicios
builder.Services.AddScoped<MiNuevoService>();
```

---

## 📦 **Paso 5: Verificar Modelos y DTOs**

- [ ] Revisar qué modelos usa el componente (ej: `@using ImpulsaDBA.Models`)
- [ ] Si el componente requiere nuevos modelos, copiarlos a `ImpulsaDBA/Models/`
- [ ] Verificar que los modelos sean compatibles con los existentes

---

## 🎨 **Paso 6: Integrar Estilos CSS**

- [ ] Si el componente tiene estilos propios, usar archivo `.razor.css` (estilos scoped)
- [ ] Verificar que no haya conflictos de nombres de clases CSS con otros componentes

---

## 🔗 **Paso 7: Agregar Botón en el Sidebar**

Modificar el archivo `ImpulsaDBA/Components/Layout/DashboardLayout.razor` para agregar el botón en el sidebar.

**Ubicación:** Dentro de la sección `<nav class="sidebar-nav">`, agregar el nuevo botón:

```razor
<nav class="sidebar-nav">
    <a href="/dashboard/inicio" class="nav-item @(GetActiveClass("/dashboard/inicio"))">
        <i class="bi bi-house-fill nav-icon"></i>
        <span class="nav-text">Inicio</span>
    </a>
    <a href="/dashboard/asignaturas" class="nav-item @(GetActiveClass("/dashboard/asignaturas"))">
        <i class="bi bi-book-fill nav-icon"></i>
        <span class="nav-text">Mis Asignaturas</span>
    </a>
    <a href="/dashboard/calendario" class="nav-item @(GetActiveClass("/dashboard/calendario"))">
        <i class="bi bi-calendar3 nav-icon"></i>
        <span class="nav-text">Calendario</span>
    </a>
    
    <!-- NUEVO: Botón para Plantilla de Notas -->
    <button class="nav-item @(vistaActual == "plantilla-notas" ? "active" : "")" 
            @onclick="() => MostrarVista("plantilla-notas")">
        <i class="bi bi-file-earmark-text nav-icon"></i>
        <span class="nav-text">Plantilla de Notas</span>
    </button>
</nav>
```

**Nota:** Cambia `"plantilla-notas"` por el identificador que quieras usar para tu componente.

---

## 🎛️ **Paso 8: Agregar Estado en DashboardLayout para Controlar la Vista**

En el mismo archivo `DashboardLayout.razor`, agregar una variable de estado y un método para cambiar la vista:

**En la sección `@code` del DashboardLayout:**

```csharp
@code {
    // ... código existente ...
    
    // NUEVO: Variable para controlar qué vista mostrar
    private string vistaActual = "dashboard";
    
    // NUEVO: Método para cambiar la vista
    private void MostrarVista(string vista)
    {
        vistaActual = vista;
        StateHasChanged();
    }
    
    // NUEVO: Método para obtener la clase activa del botón
    private string GetVistaActiveClass(string vista)
    {
        return vistaActual == vista ? "active" : "";
    }
}
```

**Actualizar el botón del sidebar para usar el nuevo método:**

```razor
<button class="nav-item @GetVistaActiveClass("plantilla-notas")" 
        @onclick="() => MostrarVista("plantilla-notas")">
    <i class="bi bi-file-earmark-text nav-icon"></i>
    <span class="nav-text">Plantilla de Notas</span>
</button>
```

**Importante:** Necesitamos pasar el estado al Dashboard. Usaremos un `CascadingValue`:

**En DashboardLayout.razor, modificar la sección del contenido:**

```razor
<!-- Content -->
<main class="dashboard-content">
    <CascadingValue Value="vistaActual" Name="VistaActual">
        <CascadingValue Value="this" Name="DashboardLayout">
            @Body
        </CascadingValue>
    </CascadingValue>
</main>
```

Y agregar un método público para cambiar la vista desde el Dashboard:

```csharp
public void CambiarVista(string vista)
{
    vistaActual = vista;
    StateHasChanged();
}
```

---

## 📄 **Paso 9: Modificar el Dashboard para Renderizar el Componente Condicionalmente**

Modificar el archivo `ImpulsaDBA/Components/Pages/Dashboard.razor`:

**1. Agregar el using del componente:**
```razor
@using ImpulsaDBA.Components.Shared
```

**2. Agregar parámetros para recibir el estado del layout:**
```razor
@code {
    [CascadingParameter(Name = "VistaActual")]
    private string? VistaActual { get; set; }
    
    [CascadingParameter(Name = "DashboardLayout")]
    private DashboardLayout? Layout { get; set; }
    
    // ... código existente ...
}
```

**3. Modificar el contenido del Dashboard para renderizar condicionalmente:**

```razor
<div class="dashboard-page">
    @if (VistaActual == "plantilla-notas")
    {
        <!-- Renderizar el componente de Plantilla de Notas -->
        <PlantillaNotas />
    }
    else
    {
        <!-- Contenido original del Dashboard -->
        @if (isLoading)
        {
            <div class="loading-container">
                <div class="spinner"></div>
                <p>Cargando asignaturas...</p>
            </div>
        }
        else if (asignaturas.Count == 0)
        {
            <div class="empty-state">
                <i class="bi bi-book-x" style="font-size: 4rem; color: var(--color-text-secondary);"></i>
                <h2>No tienes asignaturas asignadas</h2>
                <p>No se encontraron asignaturas asignadas para tu perfil.</p>
            </div>
        }
        else
        {
            <div class="page-header">
                <div class="page-header-left">
                    <h1>Mis Asignaturas</h1>
                    <div class="page-subtitle">
                        <span>Periodo académico actual: Periodo @periodoActual - @anioActual</span>
                        <span class="asignaturas-count">@asignaturas.Count Asignatura(s) asignada(s)</span>
                    </div>
                </div>
                <div class="page-header-right">
                    <Ayudas />
                </div>
            </div>

            <div class="asignaturas-grid">
                @foreach (var asignatura in asignaturas)
                {
                    <CardAsignatura Asignatura="asignatura" OnClick="OnAsignaturaClick" />
                }
            </div>
        }
    }
</div>
```

**4. Agregar botón para volver al Dashboard (opcional, dentro del componente):**

Si quieres que el componente tenga un botón para volver al dashboard, puedes pasarle un callback:

```razor
@if (VistaActual == "plantilla-notas")
{
    <PlantillaNotas OnVolver="VolverAlDashboard" />
}
```

Y agregar el método:

```csharp
private void VolverAlDashboard()
{
    if (Layout != null)
    {
        Layout.CambiarVista("dashboard");
    }
}
```

---

## ✅ **Paso 10: Verificar Referencias a la API (si aplica)**

Si el componente hace llamadas a la API:
- [ ] Verificar que los endpoints existan en `ImpulsaDBA.API/Controllers/`
- [ ] Si necesita nuevos endpoints, crearlos en el proyecto API
- [ ] Confirmar que `HttpClient` esté configurado correctamente en `Program.cs`

---

## 🧪 **Paso 11: Probar la Integración**

- [ ] Compilar el proyecto sin errores:
  ```bash
  dotnet build
  ```
- [ ] Ejecutar la aplicación:
  ```bash
  dotnet run
  ```
- [ ] Verificar que:
  - El botón aparece en el sidebar
  - Al hacer clic, el componente se renderiza dentro del Dashboard
  - El botón se marca como activo cuando el componente está visible
  - Los servicios inyectados funcionan correctamente
  - Los estilos se aplican correctamente
  - No hay errores en la consola del navegador

---

## 🔄 **Paso 12: Agregar Funcionalidad de Navegación (Opcional)**

Si quieres que el componente también sea accesible por URL, puedes agregar una ruta:

**En el componente PlantillaNotas.razor:**
```razor
@page "/dashboard/plantilla-notas"
```

Y actualizar el botón del sidebar para usar navegación:
```razor
<a href="/dashboard/plantilla-notas" class="nav-item @(GetActiveClass("/dashboard/plantilla-notas"))">
    <i class="bi bi-file-earmark-text nav-icon"></i>
    <span class="nav-text">Plantilla de Notas</span>
</a>
```

Esto permite que el componente sea accesible tanto desde el botón como desde la URL directamente.

---

## 📝 **Paso 13: Resolver Conflictos (si existen)**

Si hay conflictos:
- [ ] **Nombres de clases CSS**: Renombrar clases conflictivas
- [ ] **Nombres de variables/parámetros**: Ajustar nombres para evitar conflictos
- [ ] **Dependencias**: Verificar versiones de paquetes NuGet

---

## 📚 **Paso 14: Documentar (Opcional)**

- [ ] Agregar comentarios en el código si es necesario
- [ ] Documentar parámetros del componente si los tiene
- [ ] Actualizar `FUNCIONAMIENTO_APLICACION.md` si el componente es importante

---

## 💾 **Paso 15: Commit y Versionado**

- [ ] Hacer commit de los cambios:
  ```bash
  git add .
  git commit -m "feat: Integrar componente Plantilla de Notas con renderizado desde sidebar"
  ```

---

## 🔍 Checklist Rápido

Antes de considerar la integración completa, verifica:

- ✅ El componente compila sin errores
- ✅ El componente está en `Components/Shared/`
- ✅ El namespace está correctamente configurado
- ✅ Los servicios necesarios están registrados en `Program.cs`
- ✅ Los modelos/DTOs están disponibles
- ✅ El botón aparece en el sidebar (`DashboardLayout.razor`)
- ✅ El estado de vista está implementado en `DashboardLayout`
- ✅ El Dashboard renderiza condicionalmente el componente
- ✅ Los estilos se aplican correctamente
- ✅ La funcionalidad básica funciona
- ✅ No hay errores en la consola del navegador
- ✅ El botón se marca como activo cuando el componente está visible

---

## 📝 Ejemplo Práctico: Integrando "Plantilla de Notas"

### Resumen de Cambios

1. **Componente**: `Components/Shared/PlantillaNotas.razor`
2. **Namespace**: `@namespace ImpulsaDBA.Components.Shared`
3. **Servicios**: Usa `AsignaturaService` (ya existe)
4. **Modelos**: Usa `Usuario` y `Asignatura` (ya existen)
5. **Estilos**: `PlantillaNotas.razor.css` (scoped)

### Código del Componente

**Archivo:** `ImpulsaDBA/Components/Shared/PlantillaNotas.razor`
```razor
@namespace ImpulsaDBA.Components.Shared
@using ImpulsaDBA.Models
@using ImpulsaDBA.Services
@inject AsignaturaService AsignaturaService

<div class="plantilla-notas">
    <div class="plantilla-header">
        <h2>Plantilla de Notas</h2>
        <button class="btn-volver" @onclick="VolverAlDashboard">
            <i class="bi bi-arrow-left"></i> Volver
        </button>
    </div>
    
    <div class="plantilla-content">
        <!-- Contenido del componente aquí -->
    </div>
</div>

@code {
    [Parameter]
    public EventCallback OnVolver { get; set; }
    
    private async Task VolverAlDashboard()
    {
        await OnVolver.InvokeAsync();
    }
}
```

### Modificaciones en DashboardLayout.razor

**Agregar botón en el sidebar:**
```razor
<nav class="sidebar-nav">
    <!-- ... botones existentes ... -->
    
    <button class="nav-item @GetVistaActiveClass("plantilla-notas")" 
            @onclick="() => MostrarVista("plantilla-notas")">
        <i class="bi bi-file-earmark-text nav-icon"></i>
        <span class="nav-text">Plantilla de Notas</span>
    </button>
</nav>
```

**Agregar estado y métodos:**
```csharp
@code {
    private string vistaActual = "dashboard";
    
    private void MostrarVista(string vista)
    {
        vistaActual = vista;
        StateHasChanged();
    }
    
    private string GetVistaActiveClass(string vista)
    {
        return vistaActual == vista ? "active" : "";
    }
    
    public void CambiarVista(string vista)
    {
        vistaActual = vista;
        StateHasChanged();
    }
}
```

### Modificaciones en Dashboard.razor

**Renderizado condicional:**
```razor
@using ImpulsaDBA.Components.Shared

<div class="dashboard-page">
    @if (VistaActual == "plantilla-notas")
    {
        <PlantillaNotas OnVolver="VolverAlDashboard" />
    }
    else
    {
        <!-- Contenido original del Dashboard -->
        <!-- ... -->
    }
</div>

@code {
    [CascadingParameter(Name = "VistaActual")]
    private string? VistaActual { get; set; }
    
    [CascadingParameter(Name = "DashboardLayout")]
    private DashboardLayout? Layout { get; set; }
    
    private void VolverAlDashboard()
    {
        Layout?.CambiarVista("dashboard");
    }
    
    // ... resto del código existente ...
}
```

---

## ⚠️ Errores Comunes y Soluciones

### Error: "The type or namespace name 'X' could not be found"
**Solución**: 
- Verificar que el namespace esté en `_Imports.razor` o agregarlo directamente en el componente
- Asegurar que el componente tenga `@namespace ImpulsaDBA.Components.Shared`

### Error: "Cannot resolve symbol 'ServiceName'"
**Solución**: 
- Verificar que el servicio esté registrado en `Program.cs`
- Asegurar que el servicio esté inyectado con `@inject NombreService NombreService`

### Error: "CascadingParameter 'VistaActual' was not supplied"
**Solución**: 
- Verificar que en `DashboardLayout.razor` se haya agregado el `CascadingValue`:
  ```razor
  <CascadingValue Value="vistaActual" Name="VistaActual">
      @Body
  </CascadingValue>
  ```
- Asegurar que el Dashboard tenga el parámetro `[CascadingParameter(Name = "VistaActual")]`

### Error: "The name 'Layout' does not exist in the current context"
**Solución**: 
- Verificar que se haya agregado el `CascadingParameter` para `DashboardLayout`:
  ```csharp
  [CascadingParameter(Name = "DashboardLayout")]
  private DashboardLayout? Layout { get; set; }
  ```
- Asegurar que en `DashboardLayout.razor` se haya envuelto `@Body` con el `CascadingValue`

### Error: "El componente no se renderiza al hacer clic en el botón"
**Solución**: 
- Verificar que el método `MostrarVista()` llame a `StateHasChanged()`
- Asegurar que el valor de `vistaActual` coincida exactamente con el string usado en el `@if` del Dashboard
- Verificar que el botón tenga el evento `@onclick` correctamente configurado

### Error: "El botón no se marca como activo"
**Solución**: 
- Verificar que el método `GetVistaActiveClass()` retorne "active" cuando la vista coincide
- Asegurar que la clase CSS "active" esté definida en los estilos del sidebar

### Error: "CSS class not found"
**Solución**: 
- Verificar que el archivo `.razor.css` esté en la misma carpeta que el `.razor`
- Asegurar que el nombre del archivo CSS coincida exactamente con el nombre del componente

### Error: "Route already exists"
**Solución**: 
- Solo aplica si agregaste una ruta `@page` al componente
- Cambiar la ruta del componente o eliminar la ruta duplicada
- Verificar que no haya conflictos con rutas existentes en `Routes.razor`

---

## 📚 Recursos Adicionales

- **Estructura del proyecto**: Ver `FUNCIONAMIENTO_APLICACION.md`
- **Servicios disponibles**: Ver `ImpulsaDBA/Services/`
- **Modelos disponibles**: Ver `ImpulsaDBA/Models/`
- **Configuración**: Ver `ImpulsaDBA/Program.cs`
- **Layout del Dashboard**: Ver `ImpulsaDBA/Components/Layout/DashboardLayout.razor`
- **Página Dashboard**: Ver `ImpulsaDBA/Components/Pages/Dashboard.razor`
- **Componentes compartidos**: Ver `ImpulsaDBA/Components/Shared/`

## 🔗 Flujo de Renderizado

```
Usuario hace clic en botón del Sidebar
         ↓
DashboardLayout.MostrarVista("plantilla-notas")
         ↓
vistaActual = "plantilla-notas"
         ↓
StateHasChanged() actualiza el CascadingValue
         ↓
Dashboard.razor recibe VistaActual = "plantilla-notas"
         ↓
Renderiza condicionalmente: @if (VistaActual == "plantilla-notas")
         ↓
Se muestra el componente PlantillaNotas
```

## 💡 Tips Adicionales

1. **Múltiples Componentes**: Si necesitas agregar más componentes, simplemente agrega más botones en el sidebar y más condiciones `@if` en el Dashboard.

2. **Persistencia de Estado**: Si quieres que el estado se mantenga al recargar la página, puedes guardar `vistaActual` en `LocalStorage`.

3. **Animaciones**: Puedes agregar transiciones CSS cuando cambia la vista usando clases de animación.

4. **Parámetros al Componente**: Puedes pasar parámetros al componente desde el Dashboard:
   ```razor
   <PlantillaNotas AsignaturaId="@asignaturaSeleccionada" />
   ```
