import { Navigate, Route, Routes } from "react-router-dom";
import Navbar from "./components/Navbar";
import ProtectedRoute from "./components/ProtectedRoute";
import ErrorBoundary from "./components/ErrorBoundary";
import NotificationToaster from "./components/NotificationToaster";
import { NotificationContext } from "./context/NotificationContext";
import { useSignalRNotifications } from "./hooks/useSignalRNotifications";
import LoginPage from "./pages/LoginPage";
import TransactionsPage from "./pages/TransactionsPage";
import CreateDisputePage from "./pages/CreateDisputePage";
import MyDisputesPage from "./pages/MyDisputesPage";
import DisputeDetailsPage from "./pages/DisputeDetailsPage";
import AdminDisputesPage from "./pages/AdminDisputesPage";
import AdminDashboardPage from "./pages/AdminDashboardPage";
import NotificationsPage from "./pages/NotificationsPage";

export default function App() {
  const { toasts, unreadCount, dismiss, markAllRead } = useSignalRNotifications();

  return (
    <NotificationContext.Provider value={{ unreadCount, markAllRead }}>
      <ErrorBoundary>
        <Navbar />
        <NotificationToaster toasts={toasts} onDismiss={dismiss} />

        <Routes>
          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="/login" element={<LoginPage />} />

          <Route
            path="/transactions"
            element={
              <ProtectedRoute allowedRoles={["Customer"]}>
                <TransactionsPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/transactions/:id/dispute"
            element={
              <ProtectedRoute allowedRoles={["Customer"]}>
                <CreateDisputePage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/my-disputes"
            element={
              <ProtectedRoute allowedRoles={["Customer"]}>
                <MyDisputesPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/disputes/:id"
            element={
              <ProtectedRoute allowedRoles={["Customer", "Admin"]}>
                <DisputeDetailsPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/admin/dashboard"
            element={
              <ProtectedRoute allowedRoles={["Admin"]}>
                <AdminDashboardPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/admin/disputes"
            element={
              <ProtectedRoute allowedRoles={["Admin"]}>
                <AdminDisputesPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/notifications"
            element={
              <ProtectedRoute allowedRoles={["Customer", "Admin"]}>
                <NotificationsPage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </ErrorBoundary>
    </NotificationContext.Provider>
  );
}
