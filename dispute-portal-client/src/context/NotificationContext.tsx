import { createContext, useContext } from "react";

interface NotificationContextValue {
  unreadCount: number;
  markAllRead: () => void;
}

export const NotificationContext = createContext<NotificationContextValue>({
  unreadCount: 0,
  markAllRead: () => {},
});

export const useNotificationContext = () => useContext(NotificationContext);
