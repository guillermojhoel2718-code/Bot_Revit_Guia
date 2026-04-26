# Agente de Contexto de Vistas

## Rol

Este agente se encarga de **leer las vistas del modelo de Revit** y construir una lista clara y compacta que pueda usarse como contexto para la IA y para otros agentes.

## Qué hace

- Recorre todas las vistas del `Document`.
- Filtra vistas que:
  - No son plantillas.
  - Son relevantes para el usuario (plantas, 3D, secciones, alzados, etc.).
- Construye una lista con:
  - Id de la vista.
  - Nombre de la vista.
  - Tipo de vista (Planta, 3D, Sección, etc.).

## Qué NO hace

- No crea, borra ni modifica vistas.
- No ejecuta transacciones.
- No cambia la vista activa.

## Entrada

- `UIApplication` / `UIDocument` (para acceder al `Document` actual).

## Salida

- Una colección (lista) de objetos con información de vistas.
- Esta información se encapsula en el `ModelContext` que se usa más adelante para IA y navegación.

## Uso en el plugin

- Se llama normalmente cuando el usuario envía una pregunta al Tutor IA.
- Su resultado se combina con otras fuentes de contexto (categorías, etc.) para construir el objeto `ModelContext`.