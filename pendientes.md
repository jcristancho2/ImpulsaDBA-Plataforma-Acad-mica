## Día 4: Calendario y Modal Básico (7 horas)

### 📆 Componente Calendario (4h)

- [ ]  Crear `Shared/CalendarioComponent.razor`
- [ ]  Implementar vista mensual
    - [ ]  Header con mes/año
    - [ ]  Botones anterior/siguiente mes
    - [ ]  Grid 7x6 (semanas x días)
    - [ ]  Nombres de días (L-D)
- [ ]  Crear `Services/CalendarioService.cs`
    - [ ]  Método ObtenerDiasFestivos()
    - [ ]  Método ObtenerActividadesPorFecha(fecha, materiaId)
- [ ]  Implementar lógica de renderizado:
    - [ ]  Días del mes actual
    - [ ]  Días del mes anterior/siguiente (gris)
    - [ ]  Día actual (resaltado)
    - [ ]  Días festivos (color especial)
    - [ ]  Días con actividades (indicador)
- [ ]  Actualización automática al cambiar mes
- [ ]  Filtrar por materia seleccionada
- [ ]  Evento click en día
- [ ]  CSS para calendario responsivo
- [ ]  Probar navegación entre meses

### 🎯 Modal de Actividades - Parte 1 (3h)

- [ ]  Crear `Shared/ModalActividad.razor`
- [ ]  Implementar overlay oscuro
- [ ]  Diseñar estructura modal (2 secciones)
- [ ]  **Sección 1 - Lista desplegable:**
    - [ ]  Crear lista con 14 tipos de actividades:
        - [ ]  Preguntas problematizadoras
        - [ ]  Lección interactiva
        - [ ]  Tarea
        - [ ]  Trabajo
        - [ ]  Taller
        - [ ]  Investigación
        - [ ]  Proyecto
        - [ ]  Actividad práctica
        - [ ]  Juego interactivo
        - [ ]  Presentaciones
        - [ ]  Documento
        - [ ]  Recursos de lectura
        - [ ]  Clase virtual
        - [ ]  Encuentro
        - [ ]  Resumen clase
        - [ ]  Evaluación
    - [ ]  Implementar efecto hover/desplegable
    - [ ]  Iconos por tipo de actividad
- [ ]  Implementar validación:
    - [ ]  No permitir crear para el mismo día
    - [ ]  Solo día siguiente en adelante
    - [ ]  Mostrar mensaje de restricción
- [ ]  Evento selección de tipo
- [ ]  CSS para animaciones
- [ ]  Botón cerrar modal

**✅ ENTREGABLE DÍA 4:** Calendario funcional con modal de selección