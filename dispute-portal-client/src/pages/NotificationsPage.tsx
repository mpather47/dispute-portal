import { useEffect, useState } from "react";
import { getNotifications } from "../api/apiClient";
import { useNotificationContext } from "../context/NotificationContext";
import type { NotificationLog } from "../types/types";

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const { markAllRead } = useNotificationContext();

  useEffect(() => {
    async function load() {
      try {
        const data = await getNotifications();
        setNotifications(data);
        markAllRead();
      } catch {
        setError("Failed to load notifications. Please try again.");
      } finally {
        setLoading(false);
      }
    }

    load();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) return <p>Loading notifications...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Notifications</h1>
        <p>System messages sent to your account.</p>
      </div>

      <div className="card">
        {notifications.length === 0 ? (
          <p>No notifications yet.</p>
        ) : (
          <ul className="notification-list">
            {notifications.map((n) => (
              <li key={n.id} className="notification-item">
                <div className="notification-header">
                  <strong>{n.subject}</strong>
                  <span className="muted">
                    {new Date(n.sentAt).toLocaleString()}
                  </span>
                </div>
                <p>{n.message}</p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
