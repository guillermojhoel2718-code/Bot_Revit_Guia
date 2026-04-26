# Agente de Interpretación de Preguntas

## Rol

Este agente se encarga de **interpretar la pregunta del usuario** a partir de texto libre y convertirla en una intención estructurada que el sistema pueda entender.

## Qué hace

- Recibe:
  - Texto de la pregunta del usuario.
  - `ModelContext` (lista de vistas, categorías, etc.).
- Devuelve un objeto de intención con campos como:
  - `intent` (ej. "ver elementos estructurales", "ir a vista planta").
  - `target_type` (ej. `view`, `category`).
  - `target_hint` (ej. “estructura”, “nivel 1”).

## Qué NO hace

- No interactúa directamente con Revit API.
- No selecciona elementos ni cambia vistas.

## Entrada

- Pregunta del usuario (string).
- `ModelContext`.

## Salida

- Un objeto de intención (intent/target) que será usado por el Agente de Decisión de Destino y, eventualmente, por la IA.

## Uso en el plugin

- En la versión inicial (sin IA):
  - Este agente puede usar reglas simples/mapeos fijos (palabras clave).
- En la versión con IA:
  - Este agente se apoyará en un modelo externo (Claude/Gemini) para interpretar preguntas complejas y generar la intención.