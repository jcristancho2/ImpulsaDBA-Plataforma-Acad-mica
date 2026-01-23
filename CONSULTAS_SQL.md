# Consultas SQL de la Aplicación ImpulsaDBA

Este documento contiene todas las consultas SQL utilizadas en los endpoints y servicios de la aplicación.

---

## 🔐 AUTENTICACIÓN (AuthService.cs)

### 1. Validar Credenciales de Usuario
**Método:** `ValidarCredencialesUsuario()`
**Parámetros:** `@Email`, `@Cedula`, `@Celular`

```sql
SELECT TOP 1
    p.id,
    p.nro_documento,
    p.e_mail,
    p.celular,
    p.primer_nombre,
    p.primer_apellido,
    p.segundo_apellido,
    p.otros_nombres,
    p.url_foto,
    r.rol AS perfil
FROM col.persona AS p
LEFT OUTER JOIN seg.rol_persona AS rp ON p.id = rp.id_persona 
    AND rp.plataforma = 1 
    AND rp.eliminado = 0
    AND (rp.fec_terminacion IS NULL OR rp.fec_terminacion >= CAST(GETDATE() AS DATE))
LEFT OUTER JOIN seg.rol AS r ON rp.id_rol = r.id
WHERE (p.e_mail = @Email OR p.nro_documento = @Cedula OR p.celular = @Celular)
ORDER BY rp.id_anio DESC
```

### 2. Validar Contraseña (Obtener Documento)
**Método:** `ValidarCredencialesPassword()` - Primera consulta
**Parámetros:** `@UsuarioId`

```sql
SELECT TOP 1 p.nro_documento
FROM col.persona AS p
WHERE p.id = @UsuarioId
```

### 3. Obtener Usuario Completo después de Validar Contraseña
**Método:** `ValidarCredencialesPassword()` - Segunda consulta
**Parámetros:** `@UsuarioId`

```sql
SELECT TOP 1
    p.id,
    p.nro_documento,
    p.e_mail,
    p.celular,
    p.primer_nombre,
    p.primer_apellido,
    p.segundo_apellido,
    p.otros_nombres,
    p.url_foto,
    r.rol AS perfil
FROM col.persona AS p
LEFT OUTER JOIN seg.rol_persona AS rp ON p.id = rp.id_persona 
    AND rp.plataforma = 1 
    AND rp.eliminado = 0
    AND (rp.fec_terminacion IS NULL OR rp.fec_terminacion >= CAST(GETDATE() AS DATE))
LEFT OUTER JOIN seg.rol AS r ON rp.id_rol = r.id
WHERE p.id = @UsuarioId
ORDER BY rp.id_anio DESC
```

---

## 📚 MATERIAS (MateriaService.cs)

### 4. Obtener Materias por Profesor
**Método:** `ObtenerMateriasPorProfesor()`
**Parámetros:** `@ProfesorId`

```sql
SELECT 
    AA.id AS id_asignacion_academica,
    A.id AS id_asignatura,
    A.asignatura AS nombre,
    AA.id_profesor,
    AA.id_grupo,
    GR.id_grado,
    G.grado,
    GR.nombre AS grupo,
    GR.id AS id_grupo,
    ISNULL(PPR.id_paleta, 0) AS id_paleta,
    ISNULL(P.color_hexadecimal, '#E3F2FD') AS color_hex
FROM col.asignacion_academica AS AA
INNER JOIN col.asignatura AS A ON AA.id_asignatura = A.id
INNER JOIN aca.grupo AS GR ON AA.id_grupo = GR.id
INNER JOIN bas.grado AS G ON GR.id_grado = G.id
LEFT OUTER JOIN aca.paleta_profesor_rubrica AS PPR 
    ON AA.id_asignatura = PPR.id_asignatura 
    AND AA.id_profesor = PPR.id_profesor
LEFT OUTER JOIN bas.paleta AS P ON PPR.id_paleta = P.id
WHERE AA.id_profesor = @ProfesorId
ORDER BY G.orden, GR.orden, A.asignatura
```

### 5. Obtener Cantidad de Estudiantes por Grupo
**Método:** `ObtenerCantidadEstudiantesPorGrupo()`
**Parámetros:** `@GrupoId`

```sql
SELECT COUNT(*) AS cantidad
FROM aca.lista
WHERE id_grupo = @GrupoId
```

### 6. Obtener Estadísticas de Actividades
**Método:** `ObtenerEstadisticasActividades()`
**Parámetros:** `@IdAsignacionAcademica`

```sql
SELECT 
    COUNT(*) AS totales,
    SUM(CASE WHEN estado = 'Activa' THEN 1 ELSE 0 END) AS activas,
    SUM(CASE WHEN estado = 'Inactiva' THEN 1 ELSE 0 END) AS inactivas,
    SUM(CASE WHEN estado = 'Pendiente' THEN 1 ELSE 0 END) AS pendientes
FROM bas.actividad
WHERE id_asignacion_academica = @IdAsignacionAcademica
```

### 7. Obtener Periodo Académico Actual
**Método:** `ObtenerPeriodoActual()` - Primera consulta (periodo activo)

```sql
SELECT TOP 1
    P.id AS periodo_id,
    P.periodo,
    A.id AS anio_id,
    A.anio
FROM bas.periodo AS P
INNER JOIN bas.anio AS A ON P.id_anio = A.id
WHERE CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_final
ORDER BY P.periodo
```

### 8. Obtener Último Periodo del Año Actual (si no hay activo)
**Método:** `ObtenerPeriodoActual()` - Segunda consulta (fallback)

```sql
SELECT TOP 1
    P.id AS periodo_id,
    P.periodo,
    A.id AS anio_id,
    A.anio
FROM bas.periodo AS P
INNER JOIN bas.anio AS A ON P.id_anio = A.id
WHERE A.anio = YEAR(GETDATE())
ORDER BY P.periodo DESC
```

### 9. Obtener Nombre de Institución
**Método:** `ObtenerNombreInstitucion()`
**Parámetros:** `@ColegioId`

```sql
SELECT TOP 1 colegio
FROM bas.colegio
WHERE id = @ColegioId
```

### 10. Obtener ID del Colegio por Profesor
**Método:** `ObtenerColegioIdPorProfesor()`
**Parámetros:** `@ProfesorId`

```sql
SELECT TOP 1 A.id_colegio
FROM col.asignacion_academica AS AA
INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
INNER JOIN bas.anio AS A ON G.id_anio = A.id
WHERE AA.id_profesor = @ProfesorId
```

---

## 👥 USUARIOS (UsuarioRepository.cs)

### 11. Obtener Todos los Usuarios
**Método:** `ObtenerUsuariosAsync()`
**Endpoint:** `GET /api/usuarios`

```sql
SELECT * FROM Usuarios
```

### 12. Obtener Usuario por ID
**Método:** `ObtenerUsuarioPorIdAsync()`
**Endpoint:** `GET /api/usuarios/{id}`
**Parámetros:** `@Id`

```sql
SELECT * FROM Usuarios WHERE Id = @Id
```

### 13. Información del Servidor SQL
**Método:** `ProbarConectividadCompletaAsync()`

```sql
SELECT @@SERVERNAME AS Servidor, 
       DB_NAME() AS BaseDatos, 
       USER_NAME() AS Usuario, 
       GETDATE() AS FechaHora, 
       @@VERSION AS VersionSQL
```

### 14. Contar Personas
**Método:** `ProbarConectividadCompletaAsync()`

```sql
SELECT COUNT(*) FROM col.persona
```

### 15. Contar Grupos
**Método:** `ProbarConectividadCompletaAsync()`

```sql
SELECT COUNT(*) FROM aca.grupo
```

### 16. Contar Asignaturas
**Método:** `ProbarConectividadCompletaAsync()`

```sql
SELECT COUNT(*) FROM col.asignatura
```

### 17. Contar Estudiantes
**Método:** `ProbarConectividadCompletaAsync()`

```sql
SELECT COUNT(DISTINCT id_estudiante) FROM aca.lista
```

### 18. Probar Función Definida
**Método:** `ProbarConectividadCompletaAsync()`

```sql
SELECT dbo.fEsUnPeriodo('1', '1;2;3;4;5') AS Resultado
```

---

## 🏥 HEALTH CHECK (HealthController.cs)

### 19. Información del Servidor (Health Check)
**Endpoint:** `GET /api/health`

```sql
SELECT @@SERVERNAME AS Servidor, 
       DB_NAME() AS BaseDatos, 
       USER_NAME() AS Usuario, 
       GETDATE() AS FechaHora
```

### 20. Conteos de Tablas (Health Check Detallado)
**Endpoint:** `GET /api/health/detailed`

```sql
-- Personas
SELECT COUNT(*) FROM col.persona

-- Grupos
SELECT COUNT(*) FROM aca.grupo

-- Asignaturas
SELECT COUNT(*) FROM col.asignatura

-- Estudiantes
SELECT COUNT(DISTINCT id_estudiante) FROM aca.lista
```

---

## 📊 RESUMEN DE TABLAS UTILIZADAS

### Esquema `col` (Colegio)
- `col.persona` - Información de personas (estudiantes, profesores, etc.)
- `col.asignacion_academica` - Asignaciones de profesores a materias y grupos
- `col.asignatura` - Catálogo de asignaturas/materias
- `col.sede` - Sedes del colegio
- `col.jornada` - Jornadas académicas

### Esquema `aca` (Académico)
- `aca.grupo` - Grupos académicos
- `aca.lista` - Lista de estudiantes por grupo
- `aca.paleta_profesor_rubrica` - Colores personalizados por profesor

### Esquema `bas` (Base/Datos Maestros)
- `bas.grado` - Grados académicos
- `bas.periodo` - Periodos académicos
- `bas.anio` - Años académicos
- `bas.colegio` - Información del colegio
- `bas.actividad` - Actividades académicas
- `bas.paleta` - Paleta de colores

### Esquema `seg` (Seguridad)
- `seg.rol_persona` - Roles asignados a personas
- `seg.rol` - Catálogo de roles

---

## 🔒 SEGURIDAD

Todas las consultas utilizan **parámetros parametrizados** para prevenir inyección SQL:
- `@Email`, `@Cedula`, `@Celular`
- `@UsuarioId`, `@ProfesorId`
- `@GrupoId`, `@IdAsignacionAcademica`
- `@ColegioId`

---

## 📝 NOTAS IMPORTANTES

1. **Validación de Roles Activos**: Las consultas de autenticación filtran roles activos usando:
   - `rp.plataforma = 1`
   - `rp.eliminado = 0`
   - `rp.fec_terminacion IS NULL OR rp.fec_terminacion >= CAST(GETDATE() AS DATE)`

2. **Periodo Académico**: Se busca primero el periodo activo (fecha actual entre inicio y fin), si no existe, se toma el último periodo del año actual.

3. **Colores de Materias**: Se obtiene el color personalizado del profesor, con fallback a `#E3F2FD` si no tiene color asignado.

4. **Estadísticas de Actividades**: Se calculan usando `CASE WHEN` para contar por estado (Activa, Inactiva, Pendiente).
