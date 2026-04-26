// components/TutorBot.tsx
// Componente de integración del Tutor IA.
// Combina el sprite arcade existente + el panel de chat + navegación por secciones.
//
// Uso en layout.tsx o page.tsx:
//   import TutorBot from '@/components/TutorBot';
//   <TutorBot />

"use client";

import { useState } from "react";
import BotSprite, { BotSpriteState } from "./BotSprite";
import TutorChatPanel from "./TutorChatPanel";
import "./tutor.css"; // Si usas CSS modules, adaptar la importación

export default function TutorBot() {
  const [isChatOpen, setIsChatOpen] = useState(false);
  const [spriteState, setSpriteState] = useState<BotSpriteState>("sitting");

  const handleToggleChat = () => {
    setIsChatOpen((prev) => {
      const next = !prev;
      setSpriteState(next ? "idle" : "sitting");
      return next;
    });
  };

  const handleStateChange = (state: BotSpriteState) => {
    setSpriteState(state);
  };

  return (
    <>
      {/* Bot sprite — clickable para abrir/cerrar chat */}
      <div className="arcade-roamer">
        <button
          className="sprite-trigger"
          onClick={handleToggleChat}
          aria-label={isChatOpen ? "Cerrar Tutor IA" : "Abrir Tutor IA"}
          title={isChatOpen ? "Cerrar tutor" : "Preguntarle al tutor IA"}
        >
          <BotSprite
            state={spriteState}
            size={64}
            showLabel
          />
        </button>
      </div>

      {/* Panel de chat — aparece encima del sprite */}
      <TutorChatPanel
        isOpen={isChatOpen}
        onClose={() => {
          setIsChatOpen(false);
          setSpriteState("sitting");
        }}
        onStateChange={handleStateChange}
        contexto="general"
      />
    </>
  );
}
