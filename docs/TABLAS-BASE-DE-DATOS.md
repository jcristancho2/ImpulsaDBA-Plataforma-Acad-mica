# Tablas de base de datos usadas por la aplicación ImpulsaDBA

La aplicación se conecta a SQL Server y utiliza **varios esquemas**: `col`, `aca`, `bas`, `tab`, `seg` y, en algunos casos, la tabla `Usuarios` (sin esquema o en `dbo`). A continuación se listan las tablas, sus campos y el uso que hace la aplicación.

---

## 1. Tabla sin esquema (o dbo)

### **Usuarios**

Usada por `UsuarioRepository` para listar/consultar usuarios (posible uso auxiliar o otra base).

| Campo           | Uso |
|-----------------|-----|
| Id              | Identificador del usuario. |
| NroDocumento    | Número de documento (cédula). |
| Email           | Correo electrónico. |
| Celular         | Número de celular. |
| NombreCompleto  | Nombre completo. |
| Perfil          | Perfil: "Profesor", "Estudiante", "Padre". |
| FotoUrl         | URL de la foto. |
| Activo          | Si el usuario está activo. |

---

## 2. Esquema **col** (personas, asignaciones, asignaturas, sedes)

### **col.persona**

Personas del sistema (docentes, etc.). Base para login y datos del usuario.

| Campo             | Uso |
|-------------------|-----|
| id                | Identificador de la persona. |
| nro_documento     | Cédula; también se usa como credencial y como contraseña legacy. |
| e_mail            | Correo; se usa para login (junto con cédula y celular). |
| celular           | Celular; se usa para login. |
| primer_nombre     | Nombre. |
| primer_apellido   | Primer apellido. |
| segundo_apellido  | Segundo apellido. |
| otros_nombres     | Otros nombres. |
| url_foto          | URL de la foto del usuario. |

**Uso en la app:** `AuthService` (login por correo/cédula/celular, datos del usuario); asignaturas y actividades (nombre del profesor).

---

### **col.persona_password**

Contraseñas de las personas (hash BCrypt o legacy por `nro_documento`).

| Campo      | Uso |
|------------|-----|
| id         | Identificador del registro. |
| id_persona | FK a `col.persona`. |
| password   | Contraseña hasheada (BCrypt, NVARCHAR(MAX)). |

**Uso en la app:** `AuthService`: validar contraseña y cambio de contraseña.

---

### **col.asignacion_academica**

Relación profesor–grupo–asignatura (qué imparte cada docente en cada grupo).

| Campo            | Uso |
|------------------|-----|
| id               | Identificador de la asignación. |
| id_grupo         | FK a `aca.grupo`. |
| id_profesor      | FK a `col.persona`. |
| id_asignatura    | FK a `col.asignatura`. |

**Uso en la app:** Asignaturas por profesor, actividades por asignación, código DANE del colegio, duplicar actividades entre grupos, permisos (solo el profesor creador puede editar).

---

### **col.asignatura**

Catálogo de asignaturas.

| Campo     | Uso |
|-----------|-----|
| id        | Identificador. |
| asignatura| Nombre de la asignatura. |
| id_area   | FK a `col.area` (para color en paleta). |

**Uso en la app:** Nombre de asignatura en calendario y en listados de actividades.

---

### **col.sede**

Sedes del colegio.

| Campo | Uso |
|-------|-----|
| id    | Identificador. |
| sede  | Nombre de la sede. |

**Uso en la app:** Asignaturas del profesor (nombre de sede como institución/sede).

---

### **col.area**

Áreas (relación con áreas MEN y paleta de colores).

| Campo       | Uso |
|-------------|-----|
| id          | Identificador. |
| id_area_men | Para enlazar con `bas.area_men_paleta` y `bas.paleta`. |

**Uso en la app:** Color de la asignatura en la lista de asignaturas del profesor.

---

## 3. Esquema **aca** (académico: grupos y listas)

### **aca.grupo**

Grupos escolares (curso/grado + nombre de grupo).

| Campo      | Uso |
|------------|-----|
| id         | Identificador del grupo. |
| nombre     | Nombre del grupo. |
| id_anio    | FK a `bas.anio`. |
| id_sede    | FK a `col.sede`. |
| id_grado   | FK a `bas.grado`. |
| id_jornada | Jornada (si existe). |

**Uso en la app:** Asignaturas por profesor, actividades por grupo/fecha, periodos, código DANE, listado de grupos para duplicar actividades.

---

### **aca.lista**

Lista de estudiantes por grupo.

| Campo          | Uso |
|----------------|-----|
| id_grupo       | FK a `aca.grupo`. |
| id_estudiante  | FK al estudiante. |
| inactivo       | Si el estudiante está inactivo en el grupo (0 = activo). |

**Uso en la app:** Conteo de estudiantes por grupo (solo activos) en asignaturas y health check.

---

## 4. Esquema **bas** (básico: años, periodos, colegios, ayudas, colores)

### **bas.anio**

Año lectivo.

| Campo       | Uso |
|-------------|-----|
| id          | Identificador. |
| anio        | Año (ej. 2025). |
| id_colegio  | FK a `bas.colegio`. |

**Uso en la app:** Navegación grupo → año → colegio (código DANE, nombre institución); periodos.

---

### **bas.periodo**

Periodos del año (trimestres, semestres, etc.).

| Campo       | Uso |
|-------------|-----|
| id          | Identificador. |
| id_anio     | FK a `bas.anio`. |
| periodo     | Nombre/número del periodo (ej. "1", "2"). |
| fec_inicio  | Fecha de inicio. |
| fec_termina | Fecha de fin. |

**Uso en la app:** Periodo actual, actividades por fecha (saber en qué periodo cae una fecha), filtros por año/periodo.

---

### **bas.colegio**

Colegios/instituciones.

| Campo        | Uso |
|--------------|-----|
| id           | Identificador. |
| colegio      | Nombre del colegio. |
| codigo_dane  | Código DANE; se usa para organizar archivos en disco: `files/<codigo_dane>/<file_name_unico>`. |

**Uso en la app:** Nombre de institución, ruta de almacenamiento de archivos por colegio.

---

### **bas.grado**

Grados académicos.

| Campo | Uso |
|-------|-----|
| id   | Identificador. |

**Uso en la app:** Navegación para obtener `id_colegio` del profesor (asignación → grupo → grado → anio → colegio).

---

### **bas.ayuda**

Ayudas (PDF/VIDEO) por código de aplicación.

| Campo            | Uso |
|------------------|-----|
| id               | Identificador. |
| codigo_aplicacion| Código que identifica el componente (ej. 101=VIDEO, 102=PDF). |
| nombre_ayuda     | Nombre descriptivo. |
| url_ayuda        | URL del recurso (PDF o video). |
| tipo             | Opcional: "PDF", "VIDEO". |

**Uso en la app:** `AyudaService`: obtener ayuda PDF y VIDEO por `idComponente` (por convención PDF = idComponente+1, VIDEO = idComponente).

---

### **bas.paleta**

Colores en hexadecimal para asignaturas.

| Campo              | Uso |
|--------------------|-----|
| id                 | Identificador. |
| color_hexadecimal  | Color en hex (con o sin #). |

**Uso en la app:** Color de la asignatura en la lista del profesor (vía `bas.area_men_paleta`).

---

### **bas.area_men_paleta**

Relación área MEN – paleta (orden de color por área).

| Campo        | Uso |
|--------------|-----|
| id_area_men  | Área MEN. |
| id_paleta    | FK a `bas.paleta`. |
| orden        | Orden para asignar color a la asignatura. |

**Uso en la app:** Asignar color a cada asignatura del profesor según su área.

---

## 5. Esquema **seg** (seguridad: roles)

### **seg.rol**

Catálogo de roles.

| Campo | Uso |
|-------|-----|
| id   | Identificador. |
| rol   | Nombre del rol (ej. "Profesor", "Estudiante"). |

**Uso en la app:** Perfil del usuario tras el login (vía `seg.rol_persona`).

---

### **seg.rol_persona**

Asignación persona–rol (qué rol tiene cada persona en la plataforma).

| Campo           | Uso |
|-----------------|-----|
| id_persona      | FK a `col.persona`. |
| id_rol          | FK a `seg.rol`. |
| id_anio         | Año al que aplica el rol. |
| plataforma      | 1 = plataforma activa. |
| eliminado       | 0 = vigente. |
| fec_terminacion | Fecha de fin del rol (NULL = sin fin). |

**Uso en la app:** Obtener el rol activo del usuario para el login y para mostrar “perfil” en la app.

---

## 6. Esquema **tab** (contenidos: recursos, actividades, archivos)

### **tab.recurso**

Recurso/actividad genérico (título, descripción, tipo, si genera entregable).

| Campo                          | Uso |
|--------------------------------|-----|
| id                             | Identificador. |
| titulo                         | Título de la actividad. |
| descripcion                    | Descripción. |
| id_tipo_recurso                | FK a `tab.tipo_recurso`. |
| debe_entregar_archivo_estudiante| Si el estudiante debe entregar archivo (entregable). |

**Uso en la app:** Crear, actualizar, listar y duplicar actividades; datos base de la actividad.

---

### **tab.tipo_recurso**

Tipos de actividad (Video de Enganche, Tarea, Taller, etc.).

| Campo       | Uso |
|-------------|-----|
| id          | Identificador. |
| tipo_recurso| Nombre del tipo. |
| abreviatura | Abreviatura. |
| orden        | Orden de visualización. |
| en_uso      | 1 = se muestra en la app. |

**Uso en la app:** Listado de tipos de actividad al crear/editar y en el calendario (nombre e icono).

---

### **tab.asignacion_academica_recurso**

Une un recurso con una asignación académica y define cuándo se muestra y si está visible (actividad en el calendario).

| Campo                    | Uso |
|--------------------------|-----|
| id                       | Identificador (ID de “actividad” en calendario). |
| id_asignacion_academica  | FK a `col.asignacion_academica`. |
| id_recurso               | FK a `tab.recurso`. |
| presencial               | Si es presencial. |
| visible                  | Si la actividad está activa/visible para estudiantes. |
| fecha_calendario         | Fecha de publicación/entrega. |
| fecha_creacion_registro  | Fecha de creación. |

**Uso en la app:** CRUD de actividades, mover fecha, duplicar, permisos (profesor creador), listados por asignación/fecha/grupo, eliminación en cascada (al borrar actividad se borran archivos e hipertexto asociados).

---

### **tab.hipertexto_recurso**

Contenido en HTML o texto (instrucciones, URLs de videos, preguntas en texto).

| Campo     | Uso |
|-----------|-----|
| id_recurso| FK a `tab.recurso`. |
| hipertexto | Contenido (HTML Quill, URLs de videos, lista de preguntas en texto). |

**Uso en la app:** Asignaciones (editor Quill), Video de Enganche (URLs), Preguntas problematizadoras (enunciados en texto); crear, actualizar y eliminar junto con la actividad.

---

### **tab.archivo**

Metadatos de archivos subidos (la ruta física se arma en la app: `files/<codigo_dane>/<file_name_unico>`).

| Campo             | Uso |
|-------------------|-----|
| id                | Identificador. |
| file_name_original| Nombre original del archivo. |
| file_name_unico   | Nombre único en disco. |
| id_tipo_archivo   | Tipo de archivo (FK a catálogo si existe). |

**Uso en la app:** Material de apoyo y archivos en asignaciones; subida, descarga y borrado (disco + BD).

---

### **tab.archivo_recurso**

Relación recurso–archivo (varios archivos por actividad).

| Campo        | Uso |
|--------------|-----|
| id_archivo   | FK a `tab.archivo`. |
| id_recurso   | FK a `tab.recurso`. |
| renderizable | Si se puede previsualizar en el navegador. |

**Uso en la app:** Al crear/actualizar actividad con archivos; al eliminar actividad o quitar archivos (borrar en disco y en `tab.archivo`/`tab.archivo_recurso`).

---

## 7. Función

### **dbo.fEsUnPeriodo**

Función usada en el health check para probar conectividad y que existan objetos en la base.

**Uso en la app:** `UsuarioRepository.ProbarConectividadCompletaAsync`.

---

## Resumen por funcionalidad

| Funcionalidad        | Tablas principales |
|----------------------|--------------------|
| Login / auth         | col.persona, col.persona_password, seg.rol, seg.rol_persona |
| Asignaturas del profesor | col.asignacion_academica, aca.grupo, aca.lista, col.asignatura, col.sede, bas.anio, bas.colegio, bas.paleta, bas.area_men_paleta, col.area |
| Calendario / actividades | tab.recurso, tab.tipo_recurso, tab.asignacion_academica_recurso, tab.hipertexto_recurso, tab.archivo, tab.archivo_recurso, col.asignacion_academica, aca.grupo, bas.anio, bas.periodo, bas.colegio |
| Archivos (subir/descargar/borrar) | tab.archivo, tab.archivo_recurso, tab.asignacion_academica_recurso, bas.colegio (codigo_dane) |
| Ayudas (PDF/VIDEO)   | bas.ayuda |
| Periodo actual / año | bas.periodo, bas.anio |
| Usuarios (repositorio auxiliar) | Usuarios |

Si necesitas el script DDL de alguna tabla en particular o más detalle de una relación, indícalo y lo detallo.
