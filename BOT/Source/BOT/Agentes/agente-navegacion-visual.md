# Agente de Navegación Visual

## Rol

Este agente se encarga de hacer que Revit “camine” hacia el destino elegido: cambia la vista activa y colabora con el agente de selección para resaltar los elementos relevantes.

## Qué hace

- Recibe un `destination` (vista y/o categoría).
- Cambia la vista activa si es necesario:
  - Abre la vista destino.
  - O activa la vista ya abierta.
- Invoca al Agente de Selección y Resaltado para:
  - Seleccionar los elementos relacionados con el destino.

## Qué NO hace

- No decide por sí mismo qué destino es correcto (eso es trabajo del Agente de Decisión de Destino).
- No interpreta preguntas de usuario.
- No modifica elementos.

## Entrada

- `UIDocument` / `UIApplication`.
- Objeto `destination`.

## Salida

- Cambios visuales en Revit:
  - Vista activa.
  - Selección de elementos.

## Uso en el plugin

- Es llamado por el Agente de Tutor de Usuario.
- Permite que la reacción a la respuesta de la IA no sea solo texto, sino también una navegación visual clara dentro del modelo.