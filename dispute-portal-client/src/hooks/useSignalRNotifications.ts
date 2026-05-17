import { useEffect, useRef, useState } from "react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from "@microsoft/signalr";
import { getNotifications } from "../api/apiClient";
import type { NotificationLog } from "../types/types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

function uid(): string {
  return localStorage.getItem("userId") ?? "";
}

function toastedKey(): string {
  return `notifLastToastedId_${uid()}`;
}

function readKey(): string {
  return `notifLastReadId_${uid()}`;
}

function getStored(key: string): number {
  return parseInt(localStorage.getItem(key) ?? "0", 10) || 0;
}

function setStored(key: string, value: number) {
  localStorage.setItem(key, String(value));
}

export function useSignalRNotifications() {
  const [toasts, setToasts] = useState<NotificationLog[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const connectionRef = useRef<HubConnection | null>(null);

  function dismiss(id: number) {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }

  function markAllRead() {
    const max = Math.max(getStored(toastedKey()), getStored(readKey()));
    setStored(readKey(), max);
    setUnreadCount(0);
  }

  useEffect(() => {
    let cancelled = false;

    async function loadMissed() {
      try {
        const notifications = await getNotifications();
        if (cancelled || notifications.length === 0) return;

        const maxId = Math.max(...notifications.map((n) => n.id));
        const lastToasted = getStored(toastedKey());
        const lastRead = getStored(readKey());

        setUnreadCount(notifications.filter((n) => n.id > lastRead).length);

        if (lastToasted === 0) {
          setStored(toastedKey(), maxId);
          return;
        }

        const missed = notifications.filter((n) => n.id > lastToasted);
        if (missed.length > 0) {
          setStored(toastedKey(), maxId);
          setToasts((prev) => [...prev, ...missed]);
        }
      } catch {
        // silently ignore
      }
    }

    async function connect() {
      const token = localStorage.getItem("token");
      if (!token || cancelled) return;

      if (
        connectionRef.current &&
        connectionRef.current.state !== HubConnectionState.Disconnected
      ) {
        await connectionRef.current.stop();
      }

      const connection = new HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}/hubs/notifications`, {
          accessTokenFactory: () => localStorage.getItem("token") ?? "",
        })
        .withAutomaticReconnect()
        .build();

      connection.on("ReceiveNotification", (notification: NotificationLog) => {
        if (cancelled) return;
        setStored(toastedKey(), notification.id);
        setUnreadCount((prev) => prev + 1);
        setToasts((prev) => [...prev, notification]);
      });

      try {
        await connection.start();
        if (cancelled) {
          await connection.stop();
          return;
        }
        connectionRef.current = connection;
        await loadMissed();
      } catch {
        // will retry on next login event
      }
    }

    async function disconnect() {
      await connectionRef.current?.stop();
      connectionRef.current = null;
      if (!cancelled) {
        setToasts([]);
        setUnreadCount(0);
      }
    }

    function handleLogin() {
      connect();
    }

    function handleLogout() {
      disconnect();
    }

    connect();
    window.addEventListener("user:login", handleLogin);
    window.addEventListener("user:logout", handleLogout);

    return () => {
      cancelled = true;
      connectionRef.current?.stop();
      connectionRef.current = null;
      window.removeEventListener("user:login", handleLogin);
      window.removeEventListener("user:logout", handleLogout);
    };
  }, []);

  return { toasts, unreadCount, dismiss, markAllRead };
}
