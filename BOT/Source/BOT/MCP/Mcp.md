# MCP · RevitTutorIA (borrador de prueba)

Este documento describe un diseño inicial de **servidor MCP** para el proyecto RevitTutorIA.  
La idea es exponer, de forma estándar, algunas capacidades de lectura y navegación de Revit para que un modelo (Claude, Gemini, etc.) pueda usarlas como herramientas, sin modificar el modelo.

> Nota: Este es un diseño conceptual / de prueba, inspirado en el Model Context Protocol (MCP) oficial, pero simplificado para este proyecto educativo. [web:108][web:115]

---

## 1. Rol del servidor MCP RevitTutorIA

- Tipo: **MCP Server** orientado a Revit.
- Función:
  - Proveer contexto sobre el modelo de Revit (vistas, categorías, selección).
  - Exponer herramientas de navegación visual y selección, en modo solo lectura.
- Cliente típico:
  - Un host MCP compatible (por ejemplo, Claude Desktop / Claude MCP, Antigravity con soporte MCP, u otro agente MCP). [web:112][web:114]

---

## 2. Capacidades expuestas como herramientas

El servidor MCP RevitTutorIA expone las siguientes “tools” (métodos):

1. `get_model_context`
2. `list_views`
3. `list_categories`
4. `navigate_to_destination`
5. `select_elements_by_category`

A futuro se pueden añadir más, pero estas son suficientes para un prototipo.

---

## 3. Tool: `get_model_context`

### Descripción

Devuelve un contexto compacto del modelo Revit actual, apto para dar a un modelo de IA una visión general de vistas y categorías.

### Request

- **Nombre**: `get_model_context`
- **Parámetros**: ninguno (usa el documento activo de Revit).

Ejemplo de llamada MCP (simplificada):

```json
{
  "method": "tools/get_model_context",
  "params": {}
}
```

### Response

```json
{
  "view_summaries": [
    {
      "id": "12345",
      "name": "Nivel 1",
      "type": "FloorPlan"
    },
    {
      "id": "67890",
      "name": "3D Estructura",
      "type": "ThreeD"
    }
  ],
  "categories": [
    {
      "name": "Walls",
      "display_name": "Muros",
      "element_count": 120
    },
    {
      "name": "Floors",
      "display_name": "Losas",
      "element_count": 45
    }
  ]
}
```

- Esta estructura es equivalente al `ModelContext` que usa el plugin internamente.

---

## 4. Tool: `list_views`

### Descripción

Lista las vistas disponibles en el modelo, con información suficiente para que la IA elija una vista de destino.

### Request

- **Nombre**: `list_views`
- **Parámetros** (opcionales):
  - `filter_type` (string) – ej. `"FloorPlan"`, `"ThreeD"`, `"Section"`.
  - `search` (string) – texto a buscar en el nombre de la vista.

Ejemplo:

```json
{
  "method": "tools/list_views",
  "params": {
    "filter_type": "FloorPlan",
    "search": "Nivel"
  }
}
```

### Response

```json
{
  "views": [
    {
      "id": "12345",
      "name": "Nivel 1",
      "type": "FloorPlan"
    },
    {
      "id": "22345",
      "name": "Nivel 2",
      "type": "FloorPlan"
    }
  ]
}
```

---

## 5. Tool: `list_categories`

### Descripción

Devuelve la lista de categorías presentes en el modelo.

### Request

- **Nombre**: `list_categories`
- **Parámetros**: ninguno (opcionalmente podrían añadirse filtros más adelante).

```json
{
  "method": "tools/list_categories",
  "params": {}
}
```

### Response

```json
{
  "categories": [
    {
      "name": "Walls",
      "display_name": "Muros",
      "element_count": 120
    },
    {
      "name": "StructuralFraming",
      "display_name": "Vigas estructurales",
      "element_count": 35
    }
  ]
}
```

---

## 6. Tool: `navigate_to_destination`

### Descripción

Permite que la IA pida al plugin que **cambie la vista activa** y/o seleccione elementos en función de un “destino” estructurado.  
Es la herramienta clave para que el modelo “camine” visualmente dentro de Revit.

### Request

- **Nombre**: `navigate_to_destination`
- **Parámetros**:

```json
{
  "destination": {
    "view_id": "12345",
    "category_name": "Walls",
    "highlight": true
  }
}
```

- `view_id` (string, opcional) – Id de la vista a activar.
- `category_name` (string, opcional) – nombre interno de categoría a seleccionar.
- `highlight` (bool, opcional) – si se debe resaltar la selección (si el plugin implementa algún efecto visual adicional).

Ejemplo de llamada:

```json
{
  "method": "tools/navigate_to_destination",
  "params": {
    "destination": {
      "view_id": "12345",
      "category_name": "Walls",
      "highlight": true
    }
  }
}
```

### Response

```json
{
  "status": "ok",
  "message": "Vista activada y muros seleccionados."
}
```

- El servidor MCP se encarga de llamar internamente a los agentes de Navegación Visual y Selección/Resaltado.

---

## 7. Tool: `select_elements_by_category`

### Descripción

Herramienta más simple para que la IA seleccione elementos de una categoría, sin cambiar vista.

### Request

- **Nombre**: `select_elements_by_category`
- **Parámetros**:

```json
{
  "category_name": "Walls"
}
```

### Response

```json
{
  "status": "ok",
  "selected_count": 120,
  "message": "Se han seleccionado 120 elementos de categoría Walls."
}
```

---

## 8. Seguridad y límites

- El servidor MCP RevitTutorIA:
  - Solo expone capacidades de **lectura y navegación**.
  - No ofrece métodos para crear, borrar o modificar elementos.
  - No ofrece herramientas para ejecutar comandos externos ni scripts arbitrarios.
- Cualquier ampliación futura (por ejemplo, edición controlada) deberá:
  - Estar claramente separada.
  - Requerir permisos explícitos. [web:112][web:114]

---

## 9. Uso previsto con un Host MCP (ejemplo conceptual)

Un host MCP (Claude Desktop, Antigravity u otro) puede:

1. Descubrir el servidor MCP RevitTutorIA.
2. Registrar sus tools (`get_model_context`, `navigate_to_destination`, etc.).
3. En una sesión de chat:
   - Llamar a `get_model_context` para obtener vistas/categorías.
   - Hacer que el modelo de IA decida un destino.
   - Llamar a `navigate_to_destination` para cambiar vista y seleccionar elementos.
4. Mantener trazabilidad de las llamadas MCP (qué se pidió, qué se hizo). [web:108][web:115]

---

## 10. Estado actual

- Este documento describe un **borrador de MCP de prueba**:
  - Aún no está implementado como server MCP real.
  - Sirve como guía para:
    - Diseñar el código del plugin RevitTutorIA.
    - Definir qué herramientas habría que implementar si se monta un servidor MCP real.
- A medida que avances:
  - Podrás mapear estos tools a métodos reales en C#.
  - Más adelante, envolverlos en un servidor MCP conforme a la especificación oficial. [web:108][web:111]