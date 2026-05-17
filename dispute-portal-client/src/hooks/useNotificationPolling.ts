import { useEffect, useState } from "react";
import { getNotifications } from "../api/apiClient";
import type { NotificationLog } from "../types/types";

const POLL_INTERVAL = 30_000;
const TOASTED_KEY = "notifLastToastedId";
const READ_KEY = "notifLastReadId";

function getStored(key: string): number {
  return parseInt(localStorage.getItem(key) ?? "0", 10) || 0;
}

function setStored(key: string, value: number) {
  localStorage.setItem(key, String(value));
}

export function useNotificationPolling() {
  const [toasts, setToasts] = useState<NotificationLog[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);

  function dismiss(id: number) {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }

  function markAllRead() {
    setUnreadCount(0);
    const max = Math.max(getStored(TOASTED_KEY), getStored(READ_KEY));
    setStored(READ_KEY, max);
  }

  useEffect(() => {
    let cancelled = false;

    async function run(isFirstLoad: boolean) {
      const token = localStorage.getItem("token");
      if (!token || cancelled) return;

      try {
        const notifications = await getNotifications();
        if (cancelled || notifications.length === 0) return;

        const maxId = Math.max(...notifications.map((n) => n.id));
        const lastToasted = getStored(TOASTED_KEY);
        const lastRead = getStored(READ_KEY);

        const unread = notifications.filter((n) => n.id > lastRead).length;
        setUnreadCount(unread);

        if (isFirstLoad && lastToasted === 0) {
          setStored(TOASTED_KEY, maxId);
          return;
        }

        const newOnes = notifications.filter((n) => n.id > lastToasted);
        if (newOnes.length > 0) {
          setStored(TOASTED_KEY, maxId);
          setToasts((prev) => [...prev, ...newOnes]);
        }
      } catch {
        // silently ignore poll failures
      }
    }

    function handleLogin() {
      run(true);
    }

    window.addEventListener("user:login", handleLogin);
    run(true);
    const interval = setInterval(() => run(false), POLL_INTERVAL);
    return () => {
      cancelled = true;
      clearInterval(interval);
      window.removeEventListener("user:login", handleLogin);
    };
  }, []);

  return { toasts, unreadCount, dismiss, markAllRead };
}
