# Agente de Selección y Resaltado

## Rol

Este agente se encarga de **seleccionar y resaltar elementos en el modelo de Revit** basándose en criterios simples (categoría, parámetros, etc.), siempre en modo lectura.

## Qué hace

- Recibe criterios de selección:
  - Categoría (ej. Walls, Floors, etc.).
  - Filtros simples por parámetros (opcional).
- Usa `FilteredElementCollector` para encontrar elementos que cumplen esos criterios.
- Actualiza la selección en Revit (por ejemplo, `uidoc.Selection.SetElementIds(...)`).

## Qué NO hace

- No modifica parámetros de elementos.
- No crea ni borra elementos.
- No ejecuta cambios irreversibles en el modelo.

## Entrada

- `UIDocument` actual.
- Criterios de selección (definidos por otros agentes o por la IA).

## Salida

- Una selección visible de elementos en la vista activa, que ayuda al usuario a ver “de qué está hablando” el tutor IA.

## Uso en el plugin

- Es llamado por agentes de nivel superior (Tutor de Usuario, Navegación Visual) cuando la IA indica un “destino” (por ejemplo, “muros estructurales”).
- Permite que Revit “señale” visualmente los elementos relevantes sin alterar el modelo.