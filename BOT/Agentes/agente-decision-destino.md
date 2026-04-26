# Agente de Decisión de Destino

## Rol

Este agente traduce la **intención** interpretada a un “destino” concreto en el modelo de Revit (una vista o un conjunto de elementos).

## Qué hace

- Recibe:
  - Objeto de intención (`intent`, `target_type`, `target_hint`).
  - `ModelContext`.
- Decide un destino, por ejemplo:
  - `destination_view_id`: Id de la vista que mejor coincide.
  - `destination_category`: categoría a seleccionar (ej. `OST_Walls`).
- Resuelve ambigüedades básicas (si hay varias vistas parecidas, elige una por defecto o la más relevante).

## Qué NO hace

- No cambia la vista ni selecciona elementos directamente.
- No habla con la UI.

## Entrada

- Intención interpretada.
- `ModelContext`.

## Salida

- Objeto `destination` que indica qué vista/categoría deben usar los agentes de Navegación y Selección.

## Uso en el plugin

- Es invocado después de interpretar la pregunta.
- Su resultado es usado por:
  - Agente de Navegación Visual.
  - Agente de Selección y Resaltado.