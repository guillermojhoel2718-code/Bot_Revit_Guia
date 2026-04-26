# Sistema de Agentes para RevitTutorIA

Este documento describe el sistema de agentes que apoyan al plugin **RevitTutorIA**.  
La arquitectura está inspirada en el sistema de agentes por capas usado en el Vault de Obsidian, adaptado al contexto de Revit y BIM.

El objetivo es que cada agente tenga un rol claro y limitado, y que juntos permitan:

- Leer el modelo de Revit en **modo solo lectura**.
- Construir un contexto útil para IA (vistas, categorías, elementos clave).
- Interpretar preguntas del usuario.
- Decidir un “destino” en el modelo (vista / conjunto de elementos) sin modificarlo.
- Coordinar acciones de navegación (cambiar vista activa, seleccionar elementos).

---

## Visión general de capas

El sistema se organiza en **tres capas** de agentes:

- **Capa 1 · Agentes de modelo (Revit)**  
  Interactúan directamente con la Revit API en modo lectura.  
  Se encargan de entender qué hay en el modelo.

- **Capa 2 · Agentes de interpretación y decisión**  
  Transforman preguntas + contexto en instrucciones estructuradas para la UI y la IA.  
  Deciden “qué significa” lo que el usuario pide.

- **Capa 3 · Agentes de tutoría y presentación**  
  Hablan con el usuario, muestran respuestas comprensibles y coordinan la navegación visual en Revit.

Cada capa puede tener varios agentes especializados.

---

## Capa 1 · Agentes de modelo (Revit)

Agentes enfocados en **leer** el modelo y construir estructuras de datos.

### 1.1 Agente de Contexto de Vistas

- **Rol**: Construir una lista de vistas relevantes del modelo actual.
- **Entrada**:
  - `UIApplication` / `UIDocument`.
- **Salida**:
  - Lista de vistas no plantilla, con:
    - Nombre de vista.
    - Tipo de vista (Planta, 3D, Sección, Alzado, etc.).
    - Id de elemento.
- **Responsabilidades**:
  - No modifica nada.
  - Solo usa `FilteredElementCollector` y propiedades de `View`.

### 1.2 Agente de Contexto de Categorías

- **Rol**: Identificar las categorías presentes en el proyecto.
- **Entrada**:
  - `Document`.
- **Salida**:
  - Lista de nombres de categorías y, opcionalmente, conteo de elementos por categoría.
- **Responsabilidades**:
  - Ayudar a saber qué tipos de elementos existen (Walls, Floors, Structural Framing, etc.).
  - No crear ni borrar nada.

### 1.3 Agente de Selección / Resaltado

- **Rol**: Dado un criterio, seleccionar o resaltar elementos en el modelo.
- **Entrada**:
  - Criterios de filtro (categoría, parámetro, etc.).
- **Salida**:
  - Selección actual en Revit (solo selección, sin cambios).
- **Responsabilidades**:
  - Encapsular la lógica de selección y vista activa.
  - Ser invocado solo cuando la Capa 2 / 3 decida un “destino”.

---

## Capa 2 · Agentes de interpretación y decisión

Agentes que reciben **preguntas + contexto** y devuelven instrucciones estructuradas.

### 2.1 Agente de Construcción de Contexto IA

- **Rol**: Juntar la salida de la Capa 1 en un objeto preparado para la IA.
- **Entrada**:
  - Resultado de:
    - Agente de Contexto de Vistas.
    - Agente de Contexto de Categorías.
- **Salida**:
  - Objeto `ModelContext` (C#) y su versión JSON.
- **Responsabilidades**:
  - Decidir qué información es necesaria para la IA (no todo el modelo).
  - Mantener el contexto compacto y útil.

### 2.2 Agente de Interpretación de Preguntas

- **Rol**: Entender qué quiere el usuario según su pregunta y el contexto.
- **Entrada**:
  - Pregunta del usuario (texto).
  - `ModelContext` (estructura de vistas y categorías).
- **Salida**:
  - Un objeto estructurado con:
    - `intent` (intención, ej. “ver muros estructurales”, “ir a planta de nivel 1”).
    - `target_type` (ej. `view`, `category`).
    - `target_hint` (ej. nombre aproximado: “estructura”, “planta nivel 1”).
- **Responsabilidades**:
  - En la versión con IA:
    - Hablar con el modelo (Claude / Gemini) para clasificar la intención.
  - En la versión sin IA (demo):
    - Usar reglas simples / mapeos fijos.

### 2.3 Agente de Decisión de Destino

- **Rol**: Convertir `intent` en un “destino” concreto dentro del modelo.
- **Entrada**:
  - Objeto de intención (`intent`, `target_type`, `target_hint`).
  - `ModelContext`.
- **Salida**:
  - `destination`:
    - `destination_view_id` (vista específica) o
    - `destination_category` + criterios de selección.
- **Responsabilidades**:
  - Resolver ambigüedades (“estructura” → vista 3D de estructura, o planta de estructura).
  - Preparar instrucciones para la Capa 3 (cambiar vista, seleccionar elementos).

---

## Capa 3 · Agentes de tutoría y presentación

Agentes que coordinan la interacción con el usuario y la navegación en Revit.

### 3.1 Agente de Tutor de Usuario

- **Rol**: Ser la “cara” del sistema ante el usuario.
- **Entrada**:
  - Pregunta en lenguaje natural.
  - Respuesta de IA (opcional, en fases posteriores).
  - `destination` (destino calculado en Capa 2).
- **Salida**:
  - Mensaje claro en el panel de Revit.
  - Orden a los agentes de Capa 1 (Selección/Resaltado) para aplicar la navegación.
- **Responsabilidades**:
  - Mostrar explicaciones breves y claras.
  - Nunca prometer acciones que modifiquen el modelo (solo lectura).
  - Mantener el log de interacción en el panel.

### 3.2 Agente de Navegación Visual

- **Rol**: Ejecutar el “caminar” dentro de Revit.
- **Entrada**:
  - `destination` decidido por la Capa 2.
- **Salida**:
  - Cambios visibles en Revit:
    - Vista activa cambiada si es necesario.
    - Selección/resaltado de elementos.
- **Responsabilidades**:
  - Usar los métodos de Capa 1 para selección y vista.
  - No abrir diálogos invasivos ni ejecutar comandos externos.

---

## Flujo resumido de interacción

1. Usuario escribe una pregunta en el panel “Tutor IA”.
2. Capa 1 construye el `ModelContext` (vistas + categorías).
3. Capa 2:
   - Recibe pregunta + contexto.
   - Interpreta la intención.
   - Decide un `destination`.
4. Capa 3:
   - Muestra una respuesta amigable al usuario.
   - Llama a los agentes de Capa 1 para:
     - Cambiar vista.
     - Seleccionar elementos relevantes.

En la versión inicial (sin IA externa):

- Capa 2 puede estar parcialmente mockeada (reglas simples).
- El JSON que se construye (pregunta + contexto) se muestra en el panel como log.

En versiones futuras:

- Capa 2 integrará llamadas reales a Claude / Gemini (BYOK) para interpretar correctamente la intención del usuario.

---

## Notas

- Este sistema está pensado para ser extensible:
  - Más agentes de Capa 1 (por ejemplo, para parámetros estructurales).
  - Más agentes de Capa 2 (por ejemplo, detección de “errores típicos”).
  - Más capacidades de Capa 3 (tutoriales paso a paso).
- Toda la arquitectura mantiene la filosofía de **solo lectura**: ayudar a entender y navegar el modelo sin modificarlo.
