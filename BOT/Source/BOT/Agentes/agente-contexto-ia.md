# Agente de Construcción de Contexto para IA

## Rol

Este agente toma la información leída del modelo (vistas, categorías, etc.) y la empaqueta en un **`ModelContext`** que puede enviarse a un modelo de IA o usarse para lógica interna.

## Qué hace

- Recibe:
  - La lista de vistas generada por el Agente de Contexto de Vistas.
  - La lista de categorías generada por el Agente de Contexto de Categorías.
- Construye un objeto `ModelContext` con propiedades como:
  - `ViewNames`
  - `ViewTypes`
  - `Categories`
- Serializa este contexto a JSON cuando es necesario (para enviar a IA o mostrarlo en el panel).

## Qué NO hace

- No lee directamente el modelo; solo combina resultados de otros agentes.
- No llama a la IA por sí mismo (eso se hace en otra capa).

## Entrada

- Datos producidos por los agentes de Capa 1 (vistas, categorías).

## Salida

- `ModelContext` en memoria (C#).
- Representación JSON del contexto cuando se necesita.

## Uso en el plugin

- Cuando el usuario envía una pregunta:
  - El plugin construye el `ModelContext` por medio de este agente.
  - Luego combina `question + context` en un JSON que será enviado a la IA en fases posteriores.