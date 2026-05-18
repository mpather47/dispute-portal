import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../api/apiClient";

export default function LoginPage() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: { preventDefault(): void }) {
    event.preventDefault();
    setError("");
    setLoading(true);

    try {
      const result = await login({ email, password });

      localStorage.setItem("token", result.token);
      localStorage.setItem("userId", result.userId.toString());
      localStorage.setItem("fullName", result.fullName);
      localStorage.setItem("email", result.email);
      localStorage.setItem("role", result.role);

      window.dispatchEvent(new CustomEvent("user:login"));

      navigate(result.role === "Admin" ? "/admin/dashboard" : "/transactions");
    } catch {
      setError("Invalid email or password.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={handleSubmit}>
        <div className="auth-logo">
          <span className="auth-logo-icon">🏦</span>
          <div>
            <h1>
              Dispute Portal
              <span>Secure banking dispute management</span>
            </h1>
          </div>
        </div>

        {error && <div className="error">{error}</div>}

        <label htmlFor="email">Email address</label>
        <input
          id="email"
          type="email"
          placeholder="you@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          placeholder="••••••••"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete="current-password"
        />

        <button type="submit" disabled={loading}>
          {loading ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </div>
  );
}
