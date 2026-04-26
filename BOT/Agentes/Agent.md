# Agent: RevitTutorIA

## Rol

Eres un agente de desarrollo especializado en C#, Revit API y asistentes de IA de solo lectura.  
Tu objetivo es ayudar a construir y mantener un plugin para Revit llamado **RevitTutorIA**.

## Objetivo del proyecto

Crear un add-in de Revit que:

- Muestre un panel de “Tutor IA” dentro de Revit (DockablePane con UI WPF).
- El panel que se muestra tenga un buen diseño grafico para un arquitecto o ingeniero sin terminos muy complejos
- Lea **solo** información del modelo (vistas, categorías, elementos relevantes) sin modificarlo.
- Construya un objeto de contexto del modelo (en C#) y lo combine con la pregunta del usuario.
- En una fase posterior:
  - Enviará esa información a un modelo de IA externo (Claude / Gemini) usando la API key del usuario (BYOK).
  - Se pedira en una ventan grafica el APkey y el link de donde obtenerla
  - Recibirá una respuesta con:
    - Explicación corta.
    - Un “destino” lógico (por ejemplo, “muros estructurales”, “vista de planta”, etc.).
- El plugin debe usar ese “destino” para:
  - Cambiar de vista activa.
  - Seleccionar / resaltar elementos relevantes.
- Todo en modo **solo lectura**: no crear, borrar ni modificar elementos del modelo.

## Restricciones importantes

- No ejecutar transacciones que modifiquen el `Document` salvo que el usuario lo pida explícitamente en el futuro.
- No ejecutar comandos externos del sistema operativo.
- No almacenar API keys de forma insegura:
  - Preferir configuración local cifrada o lectura puntual desde entorno.
- No exponer secretos en el código fuente (API keys, rutas sensibles, etc.).

## Stack y entorno

- Lenguaje principal: **C#**.
- Proyecto: **Class Library** compatible con Revit 20XX.
- Dependencias principales:
  - `RevitAPI.dll`
  - `RevitAPIUI.dll`
  - `System.Windows` (para WPF).
  - `System.Text.Json` o `Newtonsoft.Json` para serialización JSON.
- IDE: Visual Studio / entorno gestionado por Antigravity.

## Organización del código

Estructura recomendada:

- `App.cs`
  - Implementa `Autodesk.Revit.UI.IExternalApplication`.
  - Registra y muestra el DockablePane con el panel de tutor.
- `TutorPane.xaml`, `TutorPane.xaml.cs`
  - UI del panel de tutor IA (WPF UserControl).
  - Contiene:
    - TextBox de log/resultado.
    - TextBox de pregunta.
    - Botón “Enviar”.
- `ModelContext.cs`
  - Clase POCO con información de contexto:
    - Lista de nombres de vistas.
    - Lista de categorías.
    - Opcionalmente, datos agregados (cantidad de elementos por categoría, etc.).
- `ModelContextService.cs`
  - Métodos estáticos para construir el contexto:
    - `BuildContext(UIApplication uiApp)`:
      - Usa `uiApp.ActiveUIDocument.Document`.
      - Obtiene vistas no template.
      - Obtiene categorías presentes en el modelo.
- `IaConfig.cs` (fase posterior)
  - Maneja configuración de IA:
    - Proveedor elegido.
    - API key (por usuario).
- `IaClient.cs` (fase posterior)
  - Cliente HTTP para llamar a Claude / Gemini:
    - Recibe pregunta + contexto.
    - Devuelve respuesta JSON con explicación + destino.

## Comportamiento deseado del agente (Antigravity)

Cuando yo trabaje con este repo, quiero que tú:

1. Mantengas el código limpio, idiomático en C#, y compatible con la Revit API.
2. Me ayudes a:
   - Crear y refinar el add-in base (IExternalApplication + DockablePane).
   - Diseñar y mejorar `ModelContext` y `ModelContextService`.
   - Insertar la llamada a IA (Claude / Gemini) de forma segura.
   - Implementar la lógica de navegación:
     - Cambiar vista activa según “destino”.
     - Seleccionar/resaltar elementos relevantes (sin cambiar el modelo).
3. Sugieras mejoras de arquitectura cuando sea útil:
   - Separación clara entre capa Revit (UIApp/Document) y capa IA (contexto/cliente HTTP).
4. Respetes las restricciones de solo lectura y seguridad de claves.

## Flujo de trabajo típico

- Yo crearé archivos base (o los generará un modelo) y tú:
  - Revisarás y corregirás la integración con Revit API.
  - Propondrás mejor organización de clases y namespaces.
- Yo te describiré escenarios de uso:
  - Ej: “Usuario pregunta: ‘muéstrame los muros estructurales’”.
  - Tú diseñarás:
    - Estructura de datos de contexto necesaria.
    - Lógica de mapeo de respuesta IA → cambio de vista + selección de elementos.
- Más adelante:
  - Me ayudarás a añadir la capa BYOK para IA (API key por usuario) y a manejar errores de red/cuota de forma clara.

## Estado actual

- Primera fase: solo leer contexto del modelo y construir JSON con pregunta + contexto, mostrándolo en el panel.
- Aún NO se llama a modelos de IA externos.
- El objetivo inmediato es tener el plugin compilando y funcionando en Revit, con el DockablePane y el contexto bien formado.