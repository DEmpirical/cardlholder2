# Gallagher Cardholder Importer

Aplicación web monolítica para carga masiva de cardholders en Gallagher Command Centre vía REST API.

---

## 🚀 Características

- **Configuración centralizada**: Host API + API Key + versión
- **Importación desde CSV**: Arrastrar y soltar, con vista previa
- **Mapeo automático de columnas**: Detección inteligente (nombre, tarjeta, email, etc.)
- **Progreso en tiempo real**: Barra de progreso y contadores
- **Historial de importaciones**: Persistencia local (últimas 50)
- **Logs integrados**: Consola con timestamps
- **Sin dependencias externas**: HTML/CSS/JS puro, funciona desde cualquier servidor estático

---

## 📦 Estructura

```
hola-mundo/
├── index.html          ← Aplicación principal (este archivo)
└── README_IMPORTER.md  ← Esta documentación
```

---

## ⚙️ Configuración

1. **Obtén credenciales de Gallagher CC**:
   - En Command Centre, habilita REST API (consulta a tu admin)
   - Genera un API Key / Bearer Token con permisos para crear cardholders

2. **Abre la aplicación**:
   - Abre `index.html` en tu navegador
   - O hosting estático: GitHub Pages, Netlify, Vercel, etc.

3. **Configura la conexión**:
   - **Host**: URL completa del servidor Gallagher (ej: `https://gallagher-server:8443`)
   - **API Key**: Tu token de API (bearer token)
   - **Versión API**: Generalmente `v1`
   - Click "Guardar"

4. **Prueba la conexión**:
   - Click "🔗 Probar Conexión"
   - Deberías ver "✅ Conectado" en los logs

---

## 📄 Formato del CSV

### Columnas mínimas requeridas

| CSV Column | Campo Gallagher | Descripción |
|------------|-----------------|-------------|
| firstName  | firstName       | Nombre del cardholder |
| lastName   | lastName        | Apellido del cardholder |

### Columnas opcionales soportadas

| CSV Column | Campo Gallagher | Descripción |
|------------|-----------------|-------------|
| cardNumber | cardNumber      | Número de tarjeta (credencial física) |
| accessGroup | accessGroup    | Nombre del grupo de acceso (debe existir en CC) |
| companyId  | companyId       | ID de compañía/empresa |
| email      | email           | Correo electrónico |
| phone      | phone           | Número telefónico |
| siteCode   | siteCode        | Código de sitio (si aplica) |

### Ejemplo CSV

```csv
firstName,lastName,cardNumber,accessGroup,email
Juan,Pérez,12345678,Empleados,juan@empresa.com
María,García,87654321,Visitantes,maria@example.com
Carlos,Rodríguez,11223344,Empleados,carlos@empresa.com
```

---

## 🗺️ Mapeo Automático

El sistema detecta nombres de columnas comunes:
- `nombre`, `first name`, `firstname`, `givenname` → `firstName`
- `apellido`, `last name`, `surname`, `familyname` → `lastName`
- `tarjeta`, `card`, `credencial`, `badge` → `cardNumber`
- `acceso`, `grupo`, `group` → `accessGroup`
- `email`, `correo`, `e-mail` → `email`
- `teléfono`, `phone`, `mobile` → `phone`

Puedes ajustar manualmente el mapeo en el formulario que aparece después de subir el CSV.

---

## 🔄 Flujo de trabajo

1. Sube tu archivo CSV (arrastrar o click)
2. Revisa la vista previa (primeras 10 filas)
3. Ajusta el mapeo de columnas si es necesario
4. Click "🚀 Iniciar Importación"
5. Observa el progreso en tiempo real
6. Revisa el log para detalles de errores
7. Consulta el historial para récords anteriores

---

## 📊 Historial

- Los registros de importación se guardan en `localStorage`
- Persisten entre recargas (hasta 50 registros)
- Muestra: fecha, archivo, total, éxitos, fallos, estado

---

## 🐛 Troubleshooting

### "No se pudo conectar"
- Verifica que el Host incluya `https://` y puerto correcto
- Confirma que la API Key sea válida y tenga permisos
- Revisa que el servidor Gallagher esté accesible desde tu red

### "Falta nombre o apellido"
- Asegúrate de mapear las columnas `firstName` y `lastName`
- Verifica que tu CSV tenga datos en esas columnas

### "HTTP 400/422"
- Formato de JSON incorrecto (reporta bug)
- Campaign data mal formada (ej: accessGroup inexistente)
- Check logs para mensaje específico del servidor

### Cardholders no aparecen en Gallagher
- Verifica que el usuario de la API tenga permiso de escritura
- Algunos servidores requieren aprobación manual de nuevos cardholders
- Revisa la pestaña "Logs" para errores silenciados

---

## 🔒 Seguridad

- La API Key se almacena SOLO en `localStorage` de tu navegador
- No se envía a terceros, solo a tu servidor Gallagher
- Usa HTTPS para el host para proteger la credencial en tránsito
- Considera usar un proxy backend si necesitas mayor seguridad

---

## 🧑‍💻 Desarrollo

Esta aplicación es **static HTML** — no requiere build step.

Para modificar:
1. Edita `index.html`
2. Los cambios se reflejan al recargar

Puntos clave del código:
- `parseCSV()` — parser simple (no soporta comillas anidadas)
- `buildCardholder()` — construye payload desde mapeo
- `createCardholder()` — llamada REST a Gallagher API
- `state` — almacenamiento en memoria
- `localStorage` — persistencia de config e historial

---

## 📝 Notas

- La API de Gallagher puede tener rate limits; esta app no los maneja (simplemente para en error)
- Para importaciones muy grandes (>1000 registros), considera un backend server para manejar colas/retries
- La detección de mapeo es básica; puedes mejorarla con ML o diccionarios más amplios

---

## 📄 Licencia

MIT — usa libremente en tu organización.

---

**Desarrollado por Bizarro** — Ingeniero de Software, ecosistema Redhood.
