// app/api/tutor/route.ts
// Endpoint mock del Tutor IA. Simula respuesta IA con delay realista.
// En Fase 3, este archivo se actualiza para llamar a Claude/Gemini con BYOK.

import { NextRequest, NextResponse } from "next/server";

export interface TutorRequest {
  message: string;
  contexto?: string;
  apiKey?: string; // Fase 3: BYOK
}

export interface TutorResponse {
  respuesta: string;
  contexto: string;
  destino?: string;
}

// Respuestas mock para simular comportamiento del tutor
const MOCK_RESPONSES: Array<{
  keywords: string[];
  respuesta: string;
  contexto: string;
  destino: string;
}> = [
  {
    keywords: ["proyecto", "trabajo", "hiciste", "realizaste", "portafolio"],
    respuesta: "He trabajado en modelado BIM de edificios de gran altura y coordinación de proyectos AEC. En la sección de proyectos puedes ver los detalles.",
    contexto: "bim",
    destino: "proyectos",
  },
  {
    keywords: ["revit", "modelo", "bim", "ifc", "familia"],
    respuesta: "Tengo experiencia sólida en Revit para modelado BIM, coordinación de fases y generación de documentación técnica. Revisa mis proyectos BIM.",
    contexto: "revit",
    destino: "proyectos",
  },
  {
    keywords: ["autocad", "planos", "cad", "dwg", "dibujo"],
    respuesta: "Uso AutoCAD para documentación técnica y planos de detalle como complemento al flujo BIM. Puedes ver ejemplos en el archivo.",
    contexto: "autocad",
    destino: "archivo",
  },
  {
    keywords: ["contacto", "contratar", "trabajo", "email", "mensaje", "hablar"],
    respuesta: "¡Claro! Puedes contactarme directamente desde la sección de contacto. Estaré feliz de conversar sobre tu proyecto.",
    contexto: "general",
    destino: "contacto",
  },
  {
    keywords: ["curriculum", "experiencia", "cv", "trayectoria", "empleo", "empresa"],
    respuesta: "Mi trayectoria incluye roles en BIM desde 2022 en empresas de construcción y prefabricado. Revisa mi curriculum completo.",
    contexto: "general",
    destino: "curriculum",
  },
  {
    keywords: ["precio", "apuc", "costo", "presupuesto", "cuanto"],
    respuesta: "Tengo experiencia en análisis de precios unitarios para presupuestos de obra. Puedes ver proyectos relacionados en el archivo.",
    contexto: "excel",
    destino: "archivo",
  },
  {
    keywords: ["dynamo", "parametrico", "automatizacion", "script", "programacion"],
    respuesta: "He desarrollado rutinas en Dynamo para automatizar tareas de modelado BIM. Los detalles están en la sección de proyectos.",
    contexto: "revit",
    destino: "proyectos",
  },
];

// Simular delay de IA real (600–1200ms)
function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Buscar respuesta mock por keywords
function findMockResponse(message: string): TutorResponse {
  const lower = message.toLowerCase();

  for (const mock of MOCK_RESPONSES) {
    if (mock.keywords.some((kw) => lower.includes(kw))) {
      return {
        respuesta: mock.respuesta,
        contexto: mock.contexto,
        destino: mock.destino,
      };
    }
  }

  // Respuesta por defecto
  return {
    respuesta: "Interesante pregunta. Puedo ayudarte con temas de BIM, Revit, AutoCAD, proyectos y mi trayectoria. ¿Qué quieres saber?",
    contexto: "general",
    destino: undefined,
  };
}

export async function POST(req: NextRequest) {
  try {
    const body: TutorRequest = await req.json();
    const { message, contexto } = body;

    if (!message || typeof message !== "string") {
      return NextResponse.json(
        { error: "El campo 'message' es requerido." },
        { status: 400 }
      );
    }

    // Simular latencia IA
    const delay = 600 + Math.random() * 600;
    await sleep(delay);

    // TODO Fase 3: si body.apiKey existe, llamar a Claude/Gemini real
    const response = findMockResponse(message);

    return NextResponse.json(response);
  } catch (error) {
    console.error("[/api/tutor] Error:", error);
    return NextResponse.json(
      { error: "Error interno del servidor." },
      { status: 500 }
    );
  }
}
