# Agente de Tutor de Usuario

## Rol

Es la **cara visible** del sistema para el usuario dentro de Revit. Gestiona el chat, muestra explicaciones y coordina la lógica de alto nivel.

## Qué hace

- Recibe la pregunta del usuario desde el panel (UI WPF).
- Llama a:
  - Agente de Construcción de Contexto.
  - Agente de Interpretación de Preguntas.
  - Agente de Decisión de Destino.
- Muestra en el panel:
  - La explicación en lenguaje natural.
  - El JSON de contexto/intención/destino (en modo debug o avanzado).
- Solicita a:
  - Agente de Navegación Visual.
  - Agente de Selección y Resaltado.
  que apliquen el destino en Revit.

## Qué NO hace

- No interactúa directamente con Revit API a bajo nivel (lo delega en otros agentes).
- No modifica el modelo por sí mismo.

## Entrada

- Pregunta del usuario (texto).
- Resultados de otros agentes (contexto, intención, destino).

## Salida

- Mensajes en la UI (explicaciones).
- Llamadas a otros agentes para navegar y resaltar elementos.

## Uso en el plugin

- Forma parte del panel `TutorPane`.
- Es el punto de entrada lógico de la interacción de usuario con el sistema de agentes.