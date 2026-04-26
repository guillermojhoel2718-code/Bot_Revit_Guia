// hooks/useSectionMap.ts
// Registra secciones navegables del portafolio y las expone para el navegador.
// Usa data-section-id en el DOM para no depender de IDs globales.

import { useCallback } from "react";

export type SectionId = string;

export interface SectionEntry {
  id: SectionId;
  label: string;
  selector: string; // ej: '[data-section-id="proyectos"]' o '#proyectos'
}

// Mapa estático de las secciones del portafolio de Guillermo Jhoel.
// Actualizar cuando se añadan secciones nuevas.
export const SECTION_MAP: Record<SectionId, SectionEntry> = {
  hero: {
    id: "hero",
    label: "Inicio",
    selector: "#hero, [data-section-id='hero'], section:first-of-type",
  },
  proyectos: {
    id: "proyectos",
    label: "Proyectos",
    selector: "#proyectos, [data-section-id='proyectos'], [id*='proyect']",
  },
  archivo: {
    id: "archivo",
    label: "Archivo",
    selector: "#archivo, [data-section-id='archivo']",
  },
  curriculum: {
    id: "curriculum",
    label: "Trayectoria Profesional",
    selector: "#curriculum, [data-section-id='curriculum'], [id*='trayectoria']",
  },
  contacto: {
    id: "contacto",
    label: "Contacto",
    selector: "#contacto, [data-section-id='contacto']",
  },
};

export function useSectionMap() {
  /**
   * Obtiene el elemento DOM de una sección por su ID lógico.
   * Intenta múltiples selectores para mayor compatibilidad.
   */
  const getSectionElement = useCallback(
    (id: SectionId): HTMLElement | null => {
      const entry = SECTION_MAP[id];
      if (!entry) return null;

      // Intentar cada selector separado por coma
      const selectors = entry.selector.split(",").map((s) => s.trim());
      for (const sel of selectors) {
        const el = document.querySelector(sel) as HTMLElement | null;
        if (el) return el;
      }
      return null;
    },
    []
  );

  /**
   * Devuelve todos los IDs de secciones conocidas.
   */
  const getAllSectionIds = useCallback((): SectionId[] => {
    return Object.keys(SECTION_MAP);
  }, []);

  /**
   * Devuelve la label legible de una sección.
   */
  const getSectionLabel = useCallback((id: SectionId): string => {
    return SECTION_MAP[id]?.label ?? id;
  }, []);

  return {
    getSectionElement,
    getAllSectionIds,
    getSectionLabel,
    SECTION_MAP,
  };
}
