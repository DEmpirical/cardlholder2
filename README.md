# Gallagher Cardholders API (C#)

Aplicación ASP.NET Core para consultar cardholders de Gallagher Command Centre.

## Requisitos

- **.NET 8 SDK** (verificar con `dotnet --version`)
- **Windows** (para acceder al certificado desde store)
- Certificado cliente instalado en `Current User → Personal`
- Gallagher con REST habilitado y API Key

## Configuración

1. **Edita `appsettings.json`** con tus valores reales:

```json
{
  "Gallagher": {
    "Host": "WIN-2UIUN05L20N",
    "Port": 8904,
    "ApiKey": "GGL-API-KEY-...",
    "ClientCertificateThumbprint": "C61F162F474F8B8137E33A11B101253A6E6D8FEB",
    "IgnoreServerCertificateErrors": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

- `Host`: nombre del equipo Gallagher (sin `https://`)
- `Port`: puerto API (por defecto 8904)
- `ApiKey`: tu clave `GGL-API-KEY-...`
- `ClientCertificateThumbprint`: huella del certificado (sin espacios, mayúsculas)
- `IgnoreServerCertificateErrors`: `true` si Gallagher usa certificado autofirmado

2. **Restaurar paquetes y ejecutar**:

```bash
dotnet restore
dotnet run
```

La API estará en `https://localhost:5001/api/cardholders`.

## Probar la API

```http
GET https://localhost:5001/api/cardholders?limit=10
```

Parámetros query:
- `search` (opcional): texto para filtrar
- `limit` (opcional): número máximo de resultados
- `offset` (opcional): paginación

Ejemplo:
```
https://localhost:5001/api/cardholders?search=john&limit=5
```

## Certificado cliente

La aplicación busca el certificado en **Windows Certificate Store** (`CurrentUser\My`). Asegúrate de que el certificado con el thumbprint configurado esté instalado allí.

Para verificar:
1. Ejecuta `certmgr.msc`
2. Ve a **Personal → Certificates**
3. Busca el certificado y copia su **Thumbprint** (elimina espacios y usa mayúsculas)

### Importar certificado (.pfx)

```powershell
Import-PfxCertificate -FilePath "C:\ruta\cert.pfx" -CertStoreLocation "Cert:\CurrentUser\My" -Password (ConvertTo-SecureString -String "tu password" -Force -AsPlainText)
```

## Solución de problemas

### Error CS0029: No se puede convertir implícitamente el tipo 'string' en 'bool'
Asegúrate de que `appsettings.json` tenga la clave `"IgnoreServerCertificateErrors"` con valor booleano (`true` o `false`). Si falta, se usa `false` por defecto.

### Error de certificado no encontrado
Verifica que el thumbprint en `appsettings.json` coincida exactamente con el del certificado en el store (sin espacios, mayúsculas).

### Puerto 5001 en uso
Cambia el puerto en `Program.cs` o usa:
```bash
dotnet run --urls "https://localhost:5002"
```

## Estructura del proyecto

```
GallagherCardholders/
├── Controllers/
│   └── CardholdersController.cs   # Endpoint GET /api/cardholders
├── Services/
│   └── GallagherClient.cs         # Cliente HTTP con certificado
├── GallagherCardholders.csproj
├── Program.cs
├── appsettings.json
├── README.md
└── goat.txt
```

## Notas

- La API usa el header `Authorization: GGL-API-KEY {apiKey}` (igual que Postman).
- Si Gallagher usa certificado autofirmado, `IgnoreServerCertificateErrors` debe ser `true`.
- El código está en C# 12 con .NET 8.

---
