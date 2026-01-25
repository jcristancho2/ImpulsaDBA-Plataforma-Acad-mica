# 🚀 Instrucciones para Ejecutar la Aplicación

## Opción 1: Usando el Script Automático (Recomendado)

```bash
# Dar permisos de ejecución (solo la primera vez)
chmod +x ejecutar.sh

# Ejecutar ambos proyectos
./ejecutar.sh
```

El script ejecutará automáticamente:
- ✅ API en `https://localhost:7001`
- ✅ Cliente en `https://localhost:7023`

Para detener: Presiona `Ctrl+C`

---

## Opción 2: Ejecutar Manualmente en Terminales Separadas

### Terminal 1 - API Backend

```bash
cd ImpulsaDBA.API
dotnet run
```

**URLs de la API:**
- HTTP: `http://localhost:5001`
- HTTPS: `https://localhost:7001`
- Swagger: `https://localhost:7001/swagger`

### Terminal 2 - Cliente Blazor

```bash
cd ImpulsaDBA.Client
dotnet run
```

**URLs del Cliente:**
- HTTP: `http://localhost:5079`
- HTTPS: `https://localhost:7023`

---

## Opción 3: Ejecutar en Background (Una sola terminal)

### API en Background

```bash
cd ImpulsaDBA.API
dotnet run &
```

### Cliente en Background

```bash
cd ImpulsaDBA.Client
dotnet run &
```

---

## 📋 Orden de Ejecución

**IMPORTANTE:** Siempre ejecuta primero la API y luego el Cliente.

1. **Primero:** API Backend (`ImpulsaDBA.API`)
2. **Segundo:** Cliente Blazor (`ImpulsaDBA.Client`)

El Cliente necesita que la API esté corriendo para funcionar correctamente.

---

## 🔗 URLs de Acceso

Una vez que ambos servicios estén ejecutándose:

### Cliente (Aplicación Principal)
- **HTTPS:** `https://localhost:7023`
- **HTTP:** `http://localhost:5079`

### API Backend
- **HTTPS:** `https://localhost:7001`
- **HTTP:** `http://localhost:5001`
- **Swagger UI:** `https://localhost:7001/swagger`

---

## ⚠️ Notas Importantes

1. **Certificados SSL:** Si aparece un error de certificado en HTTPS, acepta la excepción (es normal en desarrollo).

2. **Puertos en Uso:** Si los puertos están ocupados:
   - Cambia los puertos en `launchSettings.json`
   - O detén el proceso que está usando el puerto

3. **Base de Datos:** Asegúrate de tener:
   - SQL Server ejecutándose
   - La cadena de conexión configurada en `ImpulsaDBA.API/appsettings.json`
   - La base de datos creada

---

## 🛑 Detener los Servicios

### Si usaste el script:
- Presiona `Ctrl+C` en la terminal

### Si ejecutaste manualmente:
- En cada terminal, presiona `Ctrl+C`
- O encuentra el proceso y mátalo:
  ```bash
  # Encontrar procesos
  lsof -i :5001
  lsof -i :5079
  
  # Matar proceso (reemplaza PID con el número)
  kill -9 PID
  ```

---

## ✅ Verificar que Están Corriendo

```bash
# Verificar API
curl http://localhost:5001/api/health/db

# Verificar Cliente
curl http://localhost:5079
```

Si ambos responden, están funcionando correctamente.
