# Agente de Contexto de Categorías

## Rol

Este agente identifica las **categorías presentes en el proyecto de Revit**, para que la IA y otros agentes sepan qué tipos de elementos existen (muros, pisos, vigas, etc.).

## Qué hace

- Lee las categorías del `Document` o de `doc.Settings.Categories`.
- Puede contar cuántos elementos tiene cada categoría (opcional).
- Construye una lista de nombres de categorías, y opcionalmente un conteo.

## Qué NO hace

- No crea ni elimina categorías.
- No modifica elementos.
- No ejecuta transacciones.

## Entrada

- `Document` actual de Revit.

## Salida

- Lista de categorías (por nombre) que se incluye dentro de `ModelContext`.
- Opcional: información adicional como conteos por categoría.

## Uso en el plugin

- Se invoca junto con el agente de vistas al construir el `ModelContext`.
- Ayuda a la IA a entender qué tipos de objetos puede mencionar o explicar.