# Despliegue a producción (PWA)


## 1. Cliente (Blazor WebAssembly)

### 1.1 URLs de la API

- **Antes de publicar**, configura la URL de la API en producción.
- Edita **`ImpulsaDBA.Client/wwwroot/appsettings.Production.json`** y sustituye:
  - `ApiBaseUrl`: URL pública de tu API (ej: `https://api.tudominio.com`).
  - `BlazorClientUrl`: URL pública del cliente (ej: `https://app.tudominio.com`).

En producción la app cargará `appsettings.json` y `appsettings.Production.json`; los valores de Production sobrescriben.

- Si prefieres no usar `appsettings.Production.json`, deja las URLs de producción en **`ImpulsaDBA.Client/wwwroot/appsettings.json`** antes de hacer `dotnet publish`.

### 1.2 Publicar

```bash
cd ImpulsaDBA.Client
dotnet publish -c Release -o ./publish
```

Sube el contenido de **`publish`** (o solo **`publish/wwwroot`** si tu host solo pide archivos estáticos) a tu hosting (IIS, Nginx, Azure Static Web Apps, Netlify, etc.).

### 1.3 Si desplegaste en una subcarpeta (ej: `https://dominio.com/app/`)

- En **`ImpulsaDBA.Client/wwwroot/index.html`** cambia:

  ```html
  <base href="/" />
  ```

  por:

  ```html
  <base href="/app/" />
  ```

- En **`ImpulsaDBA.Client/wwwroot/manifest.webmanifest`** ajusta:

  ```json
  "scope": "/app/",
  "start_url": "/app/"
  ```

- En **`ImpulsaDBA.Client/wwwroot/service-worker.published.js`** (antes de publicar) cambia:

  ```js
  const base = "/";
  ```

  por:

  ```js
  const base = "/app/";
  ```

- Vuelve a publicar después de estos cambios.

---

## 2. API

### 2.1 CORS: origen del cliente

- Edita **`ImpulsaDBA.API/appsettings.Production.json`** y pon la URL exacta del cliente en producción:

  ```json
  {
    "BlazorClientUrl": "https://app.tudominio.com"
  }
  ```

- Esa URL debe coincidir con el origen desde el que se sirve la PWA (incluye protocolo y dominio, sin barra final).
- La API ya usa `BlazorClientUrl` en la política CORS; en producción se cargará desde `appsettings.Production.json`.

### 2.2 Otras configuraciones

- **ConnectionStrings**: en producción no uses la cadena de desarrollo. Configura **`ImpulsaDBA.API/appsettings.Production.json`** (o variables de entorno) con la base de datos de producción.
- **HTTPS**: sirve la API siempre por HTTPS en producción.

---

## 3. PWA: requisitos que deben cumplirse

| Requisito | Comprobación |
|-----------|--------------|
| **HTTPS** | Cliente y API en producción deben servirse por HTTPS. |
| **Manifest** | `manifest.webmanifest` con rutas relativas (ya configurado). |
| **Iconos** | `icons/icon-192.png` e `icons/icon-512.png` en `wwwroot` (ya configurado). |
| **Service Worker** | En el publish se usa la versión con caché (`service-worker.published.js`). No hace falta cambiar nada si desplegas en la raíz. |
| **Origen único** | Si el cliente está en `https://app.tudominio.com`, no mezcles con `http://` ni con otro dominio al instalar la PWA. |

---

## 4. Después del despliegue

1. Abre la app en **HTTPS** en el navegador.
2. DevTools → **Application** → **Manifest**: sin errores y iconos correctos.
3. **Application** → **Service Workers**: registrado y activo.
4. Prueba **Instalar aplicación** (Chrome: icono en la barra de direcciones o menú).
5. Prueba login y una pantalla que llame a la API para confirmar que `ApiBaseUrl` y CORS son correctos.

---

## 5. Resumen rápido

- **Cliente**: definir `ApiBaseUrl` (y opcionalmente `BlazorClientUrl`) en `wwwroot/appsettings.Production.json` (o en `appsettings.json`) y publicar.
- **API**: definir `BlazorClientUrl` en `appsettings.Production.json` y conexión a BD de producción.
- **Despliegue en subcarpeta**: ajustar `<base href>`, `scope`/`start_url` del manifest y `base` del service worker antes de publicar.
