// components/BotSprite.tsx
// Avatar pixel art del tutor IA. Muestra el sprite correcto según el estado actual.
// El sprite labeling replica el estilo "SENTADO" que ya existe en el portafolio.

"use client";

import Image from "next/image";

export type BotSpriteState =
  | "idle"
  | "sitting"
  | "thinking"
  | "excited"
  | "walking"
  | "walking-left"
  | "running"
  | "jumping"
  | "back";

interface BotSpriteProps {
  state?: BotSpriteState;
  size?: number;
  label?: string;      // Override del label automático
  showLabel?: boolean;
  onClick?: () => void;
  className?: string;
}

// Mapa estado → archivo PNG + label display
const SPRITE_CONFIG: Record<
  BotSpriteState,
  { file: string; label: string }
> = {
  idle:           { file: "estatico.png",                   label: "ESPERANDO" },
  sitting:        { file: "sentado.png",                    label: "SENTADO" },
  thinking:       { file: "brazos-cruzados.png",            label: "PENSANDO..." },
  excited:        { file: "emocionado.png",                 label: "¡LISTO!" },
  walking:        { file: "lateral-derecho-caminata.png",   label: "NAVEGANDO" },
  "walking-left": { file: "lateral-izquierdo-caminata.png", label: "NAVEGANDO" },
  running:        { file: "lateral-derecho-corrida.png",    label: "BUSCANDO" },
  jumping:        { file: "salto.png",                      label: "¡EUREKA!" },
  back:           { file: "espalda.png",                    label: "PENSANDO..." },
};

// Ajuste: la carpeta "sprit-dividido" tiene typo original — mantenerla para no romper rutas
const SPRITE_BASE = "/sprites/sprit-dividido";

export default function BotSprite({
  state = "sitting",
  size = 64,
  label,
  showLabel = true,
  onClick,
  className,
}: BotSpriteProps) {
  const config = SPRITE_CONFIG[state] ?? SPRITE_CONFIG.sitting;
  const displayLabel = label ?? config.label;

  return (
    <div
      onClick={onClick}
      className={className}
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: "4px",
        cursor: onClick ? "pointer" : "default",
        userSelect: "none",
      }}
    >
      {/* Label estilo "SENTADO" del portafolio */}
      {showLabel && (
        <span
          style={{
            color: "#94a3b8",
            fontSize: "9px",
            letterSpacing: "0.15em",
            fontFamily: "monospace",
            textTransform: "uppercase",
            lineHeight: 1,
          }}
        >
          {displayLabel}
        </span>
      )}

      {/* Sprite PNG */}
      <Image
        src={`${SPRITE_BASE}/${config.file}`}
        alt={`Bot: ${displayLabel}`}
        width={size}
        height={size}
        style={{
          imageRendering: "pixelated", // mantiene la estética pixel art
          transition: "transform 0.2s ease",
        }}
        unoptimized // Para GIFs y PNGs pixel art sin compresión
      />
    </div>
  );
}
