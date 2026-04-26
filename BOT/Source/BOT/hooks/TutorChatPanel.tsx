// components/TutorChatPanel.tsx
// Panel de chat del Tutor IA que se abre al hacer click en el bot arcade.
// Se integra al bot existente del portafolio sin reemplazarlo.

"use client";

import { useRef, useEffect, useState, KeyboardEvent } from "react";
import { useBotChat } from "../hooks/useBotChat";
import { useNavigator } from "../hooks/useNavigator";
import { useSectionMap } from "../hooks/useSectionMap";
import { useLocalApiKey } from "../hooks/useLocalApiKey";
import { BotSpriteState } from "./BotSprite";

interface TutorChatPanelProps {
  isOpen: boolean;
  onClose: () => void;
  onStateChange?: (state: BotSpriteState) => void;
  contexto?: string; // contexto actual (autocad, revit, etc.)
}

export default function TutorChatPanel({
  isOpen,
  onClose,
  onStateChange,
  contexto = "general",
}: TutorChatPanelProps) {
  const { messages, loading, sendMessage } = useBotChat();
  const { navigateTo } = useNavigator();
  const { getAllSectionIds } = useSectionMap();
  const { apiKey } = useLocalApiKey();

  const [input, setInput] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll al último mensaje
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Cambiar estado del sprite cuando loading cambia
  useEffect(() => {
    if (loading) {
      onStateChange?.("thinking");
    }
  }, [loading, onStateChange]);

  const handleSend = async () => {
    if (!input.trim() || loading) return;
    const texto = input;
    setInput("");

    const response = await sendMessage(texto, contexto);

    if (response) {
      // Sprite → excited brevemente
      onStateChange?.("excited");

      // Si hay destino → navegar + sprite walking
      if (response.destino) {
        setTimeout(() => {
          onStateChange?.("walking");
          navigateTo(`[data-section-id="${response.destino}"], #${response.destino}`);
        }, 800);

        setTimeout(() => {
          onStateChange?.("idle");
        }, 3000);
      } else {
        setTimeout(() => onStateChange?.("idle"), 2000);
      }
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  if (!isOpen) return null;

  return (
    <div
      style={{
        position: "fixed",
        bottom: "90px",
        right: "16px",
        width: "320px",
        maxHeight: "420px",
        background: "rgba(5, 5, 20, 0.96)",
        border: "1px solid rgba(0, 100, 255, 0.3)",
        borderRadius: "12px",
        display: "flex",
        flexDirection: "column",
        zIndex: 9999,
        backdropFilter: "blur(12px)",
        boxShadow: "0 0 40px rgba(0, 100, 255, 0.15)",
        fontFamily: "monospace",
        animation: "slideUp 0.2s ease-out",
      }}
    >
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "12px 16px",
          borderBottom: "1px solid rgba(0, 100, 255, 0.2)",
        }}
      >
        <span style={{ color: "#0066ff", fontSize: "11px", letterSpacing: "0.1em" }}>
          TUTOR_IA // MODO EXPLORACIÓN
        </span>
        <button
          onClick={onClose}
          style={{
            background: "none",
            border: "none",
            color: "#94a3b8",
            cursor: "pointer",
            fontSize: "16px",
            lineHeight: 1,
          }}
          aria-label="Cerrar chat"
        >
          ×
        </button>
      </div>

      {/* Messages */}
      <div
        style={{
          flex: 1,
          overflowY: "auto",
          padding: "12px 16px",
          display: "flex",
          flexDirection: "column",
          gap: "10px",
          minHeight: "200px",
          maxHeight: "280px",
        }}
      >
        {messages.length === 0 && (
          <p style={{ color: "#475569", fontSize: "12px", textAlign: "center", marginTop: "40px" }}>
            Hola 👋 Pregúntame sobre el portafolio o sobre BIM / AEC.
          </p>
        )}

        {messages.map((msg) => (
          <div
            key={msg.id}
            style={{
              display: "flex",
              justifyContent: msg.role === "user" ? "flex-end" : "flex-start",
            }}
          >
            <div
              style={{
                maxWidth: "85%",
                padding: "8px 12px",
                borderRadius: msg.role === "user" ? "12px 12px 2px 12px" : "12px 12px 12px 2px",
                background:
                  msg.role === "user"
                    ? "rgba(0, 100, 255, 0.25)"
                    : "rgba(255, 255, 255, 0.06)",
                border:
                  msg.role === "user"
                    ? "1px solid rgba(0, 100, 255, 0.4)"
                    : "1px solid rgba(255, 255, 255, 0.08)",
                color: msg.role === "user" ? "#93c5fd" : "#e2e8f0",
                fontSize: "12px",
                lineHeight: "1.5",
              }}
            >
              {msg.content}
            </div>
          </div>
        ))}

        {loading && (
          <div style={{ display: "flex", justifyContent: "flex-start" }}>
            <div
              style={{
                padding: "8px 14px",
                borderRadius: "12px 12px 12px 2px",
                background: "rgba(255,255,255,0.06)",
                border: "1px solid rgba(255,255,255,0.08)",
                color: "#0066ff",
                fontSize: "12px",
              }}
            >
              <span style={{ animation: "blink 1s infinite" }}>●</span>
              <span style={{ animation: "blink 1s 0.2s infinite" }}>●</span>
              <span style={{ animation: "blink 1s 0.4s infinite" }}>●</span>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div
        style={{
          display: "flex",
          gap: "8px",
          padding: "10px 12px",
          borderTop: "1px solid rgba(0, 100, 255, 0.2)",
        }}
      >
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Escribe tu pregunta..."
          disabled={loading}
          style={{
            flex: 1,
            background: "rgba(255,255,255,0.05)",
            border: "1px solid rgba(0, 100, 255, 0.3)",
            borderRadius: "6px",
            color: "#e2e8f0",
            padding: "7px 10px",
            fontSize: "12px",
            outline: "none",
            fontFamily: "monospace",
          }}
        />
        <button
          onClick={handleSend}
          disabled={loading || !input.trim()}
          style={{
            background: loading ? "rgba(0,100,255,0.2)" : "#0066ff",
            border: "none",
            borderRadius: "6px",
            color: "white",
            padding: "7px 12px",
            fontSize: "12px",
            cursor: loading ? "not-allowed" : "pointer",
            fontFamily: "monospace",
            transition: "background 0.2s",
          }}
        >
          {loading ? "..." : "▶"}
        </button>
      </div>
    </div>
  );
}
