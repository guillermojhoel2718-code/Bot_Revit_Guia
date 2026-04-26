// hooks/useBotChat.ts
import { useState } from "react";

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
}

export interface BotResponse {
  respuesta: string;
  contexto: string;       // "autocad" | "revit" | "excel" | ...
  destino?: string;       // id lógico, que luego mapearás a un selector
}

export function useBotChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);

  const sendMessage = async (text: string, contexto: string) => {
    if (!text.trim()) return;
    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content: text,
    };
    setMessages((prev) => [...prev, userMessage]);

    setLoading(true);
    try {
      const res = await fetch("/api/tutor", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: text, contexto }),
      });

      const data: BotResponse = await res.json();

      const botMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: "assistant",
        content: data.respuesta,
      };

      setMessages((prev) => [...prev, botMessage]);

      return data; // devuelve para que el componente pueda usar destino
    } finally {
      setLoading(false);
    }
  };

  return { messages, loading, sendMessage };
}