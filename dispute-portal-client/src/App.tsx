import { Navigate, Route, Routes } from "react-router-dom";
import Navbar from "./components/Navbar";
import ProtectedRoute from "./components/ProtectedRoute";
import LoginPage from "./pages/LoginPage";
import TransactionsPage from "./pages/TransactionsPage";
import CreateDisputePage from "./pages/CreateDisputePage";
import MyDisputesPage from "./pages/MyDisputesPage";
import DisputeDetailsPage from "./pages/DisputeDetailsPage";
import AdminDisputesPage from "./pages/AdminDisputesPage";

export default function App() {
  return (
    <>
      <Navbar />

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
          path="/admin/disputes"
          element={
            <ProtectedRoute allowedRoles={["Admin"]}>
              <AdminDisputesPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </>
  );
}