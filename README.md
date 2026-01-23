# ImpulsaDBA-Plataforma-Acad-mica

Plataforma académica desarrollada con Blazor Server.

## Configuración de Base de Datos

El proyecto soporta dos métodos para configurar la conexión a la base de datos:

### Método 1: Usando appsettings.json (Recomendado para desarrollo)

Edita el archivo `ImpulsaDBA/appsettings.json` y actualiza la cadena de conexión:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=ImpulsaDBA;User Id=sa;Password=tu_password;TrustServerCertificate=true;Encrypt=false;"
    }
}
```

### Método 2: Usando variables de entorno (Recomendado para producción)

Crea un archivo `.env` en la carpeta `ImpulsaDBA/` con las siguientes variables:

```
DB_SERVER=10.211.55.3
DB_NAME=ImpulsaDBA
DB_USER=sa
DB_PASSWORD=tu_password_aqui
DB_PORT=1433
```

**Nota:** El archivo `.env` debe estar en `.gitignore` para no exponer credenciales.

### Configuración para Parallels Desktop / Máquina Virtual Windows

Si tu SQL Server está en una máquina virtual Windows (Parallels Desktop, VMware, etc.), necesitas:

1. **Obtener la IP de la VM Windows:**
   - En Windows, abre PowerShell o CMD y ejecuta: `ipconfig`
   - Busca la dirección IPv4 (generalmente algo como `10.211.55.x` o `192.168.x.x`)

2. **Configurar el archivo `.env`:**
   
   Crea o edita el archivo `.env` en la carpeta `ImpulsaDBA/` con:
   ```
   DB_SERVER=10.211.55.3
   DB_NAME=ImpulsaDBA
   DB_USER=sa
   DB_PASSWORD=tu_password
   DB_PORT=1433
   ```
   
   **Ejemplo real basado en tu configuración:**
   ```
   DB_SERVER=10.211.55.3
   DB_NAME=ImpulsaDBA
   DB_USER=sa
   DB_PASSWORD=tu_password_aqui
   DB_PORT=1433
   ```
   
   O si usas nombre de instancia (ej: SQLEXPRESS):
   ```
   DB_SERVER=10.211.55.3\SQLEXPRESS
   DB_NAME=ImpulsaDBA
   DB_USER=sa
   DB_PASSWORD=tu_password
   ```

3. **Configurar SQL Server para aceptar conexiones remotas:**
   - Abre SQL Server Configuration Manager en Windows
   - Habilita TCP/IP en Protocolos para SQL Server
   - Reinicia el servicio SQL Server
   - Configura el firewall de Windows para permitir el puerto 1433 (o el que uses)

4. **Verificar conectividad desde macOS:**
   ```bash
   # Probar si el puerto está abierto
   nc -zv 10.211.55.3 1433
   ```

### Prioridad de configuración

1. Primero se intenta leer desde `appsettings.json` (ConnectionStrings.DefaultConnection)
2. Si no está disponible, se construye desde variables de entorno
3. Si ninguna está configurada, se usan valores por defecto (localhost, ImpulsaDBA, sa, sin password)

## Estructura del Proyecto

- `ImpulsaDBA/` - Aplicación principal Blazor Server
  - `Services/DatabaseService.cs` - Servicio para operaciones de base de datos
  - `Components/Pages/Home.razor` - Página principal con prueba de conexión
  - `appsettings.json` - Configuración de la aplicación

## Prueba de Conexión

La página principal (`/`) incluye un botón para probar la conexión a la base de datos. Asegúrate de tener:

1. SQL Server ejecutándose
2. La base de datos `ImpulsaDBA` creada (o el nombre que hayas configurado)
3. Credenciales válidas configuradas

## Tecnologías

- .NET 10.0
- Blazor Server
- Microsoft.Data.SqlClient
- DotNetEnv (para variables de entorno)
