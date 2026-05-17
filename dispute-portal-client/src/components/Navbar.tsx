import { Link, useNavigate } from "react-router-dom";
import { useNotificationContext } from "../context/NotificationContext";

export default function Navbar() {
  const navigate = useNavigate();
  const { unreadCount } = useNotificationContext();

  const token = localStorage.getItem("token");
  const role = localStorage.getItem("role");
  const fullName = localStorage.getItem("fullName");

  function logout() {
    localStorage.clear();
    navigate("/login");
  }

  if (!token) {
    return null;
  }

  return (
    <nav className="navbar">
      <div>
        <strong>Dispute Portal</strong>
      </div>

      <div className="nav-links">
        {role === "Customer" && (
          <>
            <Link to="/transactions">Transactions</Link>
            <Link to="/my-disputes">My Disputes</Link>
          </>
        )}

        {role === "Admin" && <Link to="/admin/disputes">Admin Disputes</Link>}

        <Link to="/notifications">
          Notifications
          {unreadCount > 0 && (
            <span className="nav-badge">{unreadCount > 99 ? "99+" : unreadCount}</span>
          )}
        </Link>

        <span>{fullName}</span>
        <button onClick={logout}>Logout</button>
      </div>
    </nav>
  );
}
