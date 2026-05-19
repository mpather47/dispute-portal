import { Link, useNavigate } from "react-router-dom";
import { useNotificationContext } from "../context/NotificationContext";

export default function Navbar() {
  const navigate = useNavigate();
  const { unreadCount } = useNotificationContext();

  const token = localStorage.getItem("token");
  const role = localStorage.getItem("role");
  const fullName = localStorage.getItem("fullName");

  function logout() {
    window.dispatchEvent(new CustomEvent("user:logout"));
    ["token", "userId", "fullName", "email", "role"].forEach((key) =>
      localStorage.removeItem(key)
    );
    navigate("/login");
  }

  if (!token) return null;

  return (
    <nav className="navbar">
      <Link to={role === "Admin" ? "/admin/dashboard" : "/transactions"} className="navbar-brand">
        <span className="brand-icon">🏦</span>
        Bank Dispute Portal
      </Link>

      <div className="nav-links">
        {role === "Customer" && (
          <>
            <Link to="/transactions">Transactions</Link>
            <Link to="/my-disputes">My Disputes</Link>
          </>
        )}

        {role === "Admin" && (
          <>
            <Link to="/admin/dashboard">Dashboard</Link>
            <Link to="/admin/disputes">Disputes</Link>
          </>
        )}

        <Link to="/notifications">
          Notifications
          {unreadCount > 0 && (
            <span className="nav-badge">{unreadCount > 99 ? "99+" : unreadCount}</span>
          )}
        </Link>

        <span className="nav-user">{fullName}</span>
        <button onClick={logout}>Sign out</button>
      </div>
    </nav>
  );
}
