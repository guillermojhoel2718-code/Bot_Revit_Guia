// hooks/useLocalApiKey.ts
// Hook para gestionar el almacenamiento local de la API Key, permitiendo usar IA sin backend dedicado (BYOK).
import { useEffect, useState } from "react";

const STORAGE_KEY = "tutor_ia_api_key";

export function useLocalApiKey() {
  const [apiKey, setApiKey] = useState<string | null>(null);

  useEffect(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored) setApiKey(stored);
  }, []);

  const saveApiKey = (key: string) => {
    setApiKey(key);
    window.localStorage.setItem(STORAGE_KEY, key);
  };

  const clearApiKey = () => {
    setApiKey(null);
    window.localStorage.removeItem(STORAGE_KEY);
  };

  return { apiKey, saveApiKey, clearApiKey };
}