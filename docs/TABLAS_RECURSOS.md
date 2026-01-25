# Tablas Necesarias para Guardar Recursos/Actividades

Este documento describe todas las tablas de la base de datos necesarias para guardar los diferentes tipos de recursos/actividades en el sistema.

## 📋 Tablas Principales (Obligatorias para TODOS los tipos)

### 1. `tab.recurso`
**Propósito:** Tabla principal que almacena la información básica de todos los recursos/actividades.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `titulo` (nvarchar(255), NOT NULL) - Título de la actividad
- `descripcion` (nvarchar(255), NOT NULL) - Descripción breve
- `id_tipo_recurso` (tinyint, NOT NULL) - FK a `tab.tipo_recurso`

**Uso:** Se inserta PRIMERO para obtener el `id_recurso` que se usará en las demás tablas.

---

### 2. `tab.asignacion_academica_recurso`
**Propósito:** Relaciona el recurso con una asignación académica específica y define su visibilidad y fecha.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `presencial` (bit, NOT NULL) - Si es presencial (default: false)
- `visible` (bit, NOT NULL) - Si está visible para estudiantes (default: true)
- `fecha_calendario` (smalldatetime, NULL) - Fecha de publicación en el calendario
- `fecha_creacion_registro` (smalldatetime, NULL) - Fecha de creación
- `id_asignacion_academica` (int, NOT NULL) - FK a `col.asignacion_academica`
- `id_recurso` (int, NULL) - FK a `tab.recurso`
- `id_cuestionario` (int, NULL) - FK a `tab.cuestionario` (para evaluaciones)

**Uso:** Se inserta SEGUNDO, después de crear el recurso. **CRÍTICO:** `visible = 1` y `fecha_calendario IS NOT NULL` para que aparezca en el calendario.

---

### 3. `tab.tipo_recurso`
**Propósito:** Catálogo de tipos de recursos disponibles.

**Campos:**
- `id` (tinyint, IDENTITY) - Clave primaria
- `tipo_recurso` (nvarchar(255), NOT NULL) - Nombre del tipo
- `abreviatura` (nvarchar(50), NOT NULL) - Abreviatura
- `en_uso` (bit, NOT NULL) - Si está activo
- `orden` (tinyint, NOT NULL) - Orden de visualización

**Tipos disponibles:**
1. Video de Enganche
2. Preguntas Problematizadoras
3. Lección Interactiva
4. Tarea
5. Trabajo
6. Taller
7. Investigación
8. Proyecto
9. Actividad Práctica
10. Juego Educativo
11. Presentación (Sliders)
12. Documento (Archivo)
13. Recurso de Lectura
14. Clase Virtual (link)
15. Encuesta
16. Resumen de Clase
17. Evaluación

---

## 📝 Tablas Opcionales (Según el tipo de recurso)

### 4. `tab.hipertexto_recurso`
**Propósito:** Almacena contenido enriquecido (HTML) o texto largo para recursos que lo requieren.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `hipertexto` (nvarchar(max), NULL) - Contenido HTML o texto
- `id_recurso` (int, NOT NULL) - FK a `tab.recurso`

**Se usa para:**
- ✅ **Asignaciones** (Tarea, Trabajo, Taller, etc.) - Contenido del editor Quill
- ✅ **Video de Enganche** - URLs de videos concatenados (separados por `\n`)
- ✅ **Preguntas Problematizadoras** - Preguntas numeradas (formato: "1. Pregunta\n2. Pregunta")
- ✅ **Clase Virtual** - Link de la clase virtual
- ✅ **Evaluación** - Instrucciones adicionales

**Nota:** Solo se inserta si `request.Hipertexto` tiene contenido.

---

### 5. `tab.archivo`
**Propósito:** Almacena información de archivos adjuntos.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `file_name_original` (nvarchar(3000), NOT NULL) - Nombre original del archivo
- `file_name_unico` (nvarchar(3000), NOT NULL) - Nombre único del archivo en el servidor
- `id_tipo_archivo` (tinyint, NOT NULL) - FK a `tab.tipo_archivo`

**Se usa para:**
- ✅ **Material de Apoyo** (Presentación, Documento, Recurso de Lectura, Resumen de Clase)
- ✅ **Asignaciones** (cuando tienen archivos adjuntos)

---

### 6. `tab.archivo_recurso`
**Propósito:** Relaciona archivos con recursos.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `renderizable` (bit, NOT NULL) - Si el archivo se puede renderizar en el navegador
- `id_archivo` (int, NOT NULL) - FK a `tab.archivo`
- `id_recurso` (int, NOT NULL) - FK a `tab.recurso`

**Se usa para:**
- ✅ **Material de Apoyo** - Archivos adjuntos
- ✅ **Asignaciones** - Archivos adjuntos

**Nota:** Se inserta después de crear el archivo en `tab.archivo`.

---

### 7. `tab.tipo_archivo`
**Propósito:** Catálogo de tipos de archivos.

**Campos:**
- `id` (tinyint, IDENTITY) - Clave primaria
- `tipo` (nvarchar(255), NOT NULL) - Nombre del tipo (PDF, Video, Audio, etc.)
- `en_uso` (bit, NOT NULL) - Si está activo
- `orden` (tinyint, NOT NULL) - Orden de visualización

**Tipos comunes:**
- 1 = PDF
- 2 = Video
- 3 = Audio
- 4 = Otro

---

## 🎯 Tablas para Evaluaciones (Futuro)

### 8. `tab.cuestionario`
**Propósito:** Almacena información de cuestionarios/evaluaciones.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- (otros campos según la estructura)

**Nota:** Actualmente las evaluaciones se guardan como recursos normales con `id_tipo_recurso = 17`, pero en el futuro podrían usar esta tabla.

---

### 9. `tab.pregunta`
**Propósito:** Almacena preguntas de cuestionarios.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `id_tipo_pregunta` (int, NOT NULL) - FK a `tab.tipo_pregunta`
- (otros campos según la estructura)

---

### 10. `tab.opcion`
**Propósito:** Almacena opciones de respuesta para preguntas de opción múltiple.

**Campos:**
- `id` (int, IDENTITY) - Clave primaria
- `id_pregunta` (int, NOT NULL) - FK a `tab.pregunta`
- (otros campos según la estructura)

---

## 📊 Tablas de Consulta (Solo lectura para obtener datos)

### 11. `col.asignacion_academica`
**Propósito:** Define la relación entre profesor, grupo y asignatura.

**Campos relevantes:**
- `id` (int) - Clave primaria
- `id_grupo` (int) - FK a `aca.grupo`
- `id_asignatura` (int) - FK a `col.asignatura`
- `id_profesor` (int) - FK a `col.persona`

**Uso:** Se consulta para obtener el `id_asignacion_academica` necesario al crear recursos.

---

### 12. `aca.grupo`
**Propósito:** Define los grupos académicos.

**Campos relevantes:**
- `id` (int) - Clave primaria
- `id_anio` (int) - FK a `bas.anio`

**Uso:** Se consulta para obtener el año académico.

---

### 13. `bas.anio`
**Propósito:** Define los años académicos.

**Campos relevantes:**
- `id` (int) - Clave primaria
- `id_colegio` (int) - FK al colegio
- `anio` (smallint) - Año académico

---

### 14. `bas.periodo`
**Propósito:** Define los periodos académicos dentro de un año.

**Campos relevantes:**
- `id` (int) - Clave primaria
- `id_anio` (int) - FK a `bas.anio`
- `fec_inicio` (date) - Fecha de inicio
- `fec_termina` (date) - Fecha de término

---

## 🔄 Flujo de Guardado por Tipo de Recurso

### Para TODOS los tipos:
1. ✅ Insertar en `tab.recurso` → Obtener `id_recurso`
2. ✅ Insertar en `tab.asignacion_academica_recurso` → Vincular con asignación

### Para Video de Enganche (Tipo 1):
3. ✅ Insertar en `tab.hipertexto_recurso` → URLs de videos concatenados

### Para Preguntas Problematizadoras (Tipo 2):
3. ✅ Insertar en `tab.hipertexto_recurso` → Preguntas numeradas

### Para Asignaciones (Tipos 4-9):
3. ✅ Insertar en `tab.hipertexto_recurso` → Contenido HTML del editor Quill
4. ✅ (Opcional) Insertar en `tab.archivo` + `tab.archivo_recurso` → Si hay archivos adjuntos

### Para Material de Apoyo (Tipos 11-13, 16):
3. ✅ Insertar en `tab.archivo` + `tab.archivo_recurso` → Archivos adjuntos

### Para Clase Virtual (Tipo 14):
3. ✅ Insertar en `tab.hipertexto_recurso` → Link de la clase virtual

### Para Evaluación (Tipo 17):
3. ✅ Insertar en `tab.hipertexto_recurso` → Instrucciones adicionales

---

## ⚠️ Campos Críticos para Visualización en Calendario

Para que una actividad aparezca en el calendario, **DEBE cumplir**:

1. ✅ `tab.asignacion_academica_recurso.visible = 1` (true)
2. ✅ `tab.asignacion_academica_recurso.fecha_calendario IS NOT NULL`
3. ✅ `tab.asignacion_academica_recurso.id_asignacion_academica` debe existir y ser válido
4. ✅ `tab.recurso.id_tipo_recurso` debe existir en `tab.tipo_recurso` (aunque ahora usamos LEFT JOIN para evitar problemas)

---

## 📝 Notas Importantes

1. **Orden de inserción:** Siempre insertar primero en `tab.recurso`, luego en `tab.asignacion_academica_recurso`, y finalmente en las tablas opcionales.

2. **Transacciones:** Se recomienda usar transacciones para asegurar la integridad de los datos.

3. **Validaciones:** 
   - Verificar que `id_tipo_recurso` exista en `tab.tipo_recurso`
   - Verificar que `id_asignacion_academica` exista en `col.asignacion_academica`
   - Verificar que `fecha_calendario` no sea NULL

4. **Eliminación lógica:** Las actividades no se eliminan físicamente, se marca `visible = 0` en `tab.asignacion_academica_recurso`.
