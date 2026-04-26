// hooks/useNavigator.ts
// Hook para la navegación visual (scroll suave) a las distintas secciones del portafolio o elementos del modelo.
import { useCallback } from "react";

export type DestinoId = string; // por ejemplo "#autocad-layers", "#revit-views"

export function useNavigator() {
  const navigateTo = useCallback((destino: DestinoId) => {
    if (!destino) return;
    const el = document.querySelector(destino) as HTMLElement | null;
    if (!el) return;

    el.scrollIntoView({ behavior: "smooth", block: "start" });
    el.classList.add("highlight-destino");

    window.setTimeout(() => {
      el.classList.remove("highlight-destino");
    }, 2000);
  }, []);

  return { navigateTo };
}