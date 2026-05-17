import { useEffect } from "react";
import type { NotificationLog } from "../types/types";

interface Props {
  toasts: NotificationLog[];
  onDismiss: (id: number) => void;
}

export default function NotificationToaster({ toasts, onDismiss }: Props) {
  if (toasts.length === 0) return null;

  return (
    <div className="toast-container">
      {toasts.map((toast) => (
        <Toast key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>
  );
}

function Toast({
  toast,
  onDismiss,
}: {
  toast: NotificationLog;
  onDismiss: (id: number) => void;
}) {
  useEffect(() => {
    const timer = setTimeout(() => onDismiss(toast.id), 5000);
    return () => clearTimeout(timer);
  }, [toast.id, onDismiss]);

  return (
    <div className="toast">
      <div className="toast-content">
        <strong>{toast.subject}</strong>
        <p>{toast.message}</p>
      </div>
      <button className="toast-close" onClick={() => onDismiss(toast.id)}>
        ×
      </button>
    </div>
  );
}
