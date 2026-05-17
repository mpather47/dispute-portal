import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../api/apiClient";

export default function LoginPage() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("customer@test.com");
  const [password, setPassword] = useState("Password123!");
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    try {
      const result = await login({ email, password });

      localStorage.setItem("token", result.token);
      localStorage.setItem("userId", result.userId.toString());
      localStorage.setItem("fullName", result.fullName);
      localStorage.setItem("email", result.email);
      localStorage.setItem("role", result.role);

      if (result.role === "Admin") {
        navigate("/admin/disputes");
      } else {
        navigate("/transactions");
      }
    } catch {
      setError("Invalid email or password.");
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={handleSubmit}>
        <h1>Bank Dispute Portal</h1>
        <p>Sign in to continue</p>

        {error && <div className="error">{error}</div>}

        <label htmlFor="email">Email</label>
        <input
          id="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          value={password}
          type="password"
          onChange={(e) => setPassword(e.target.value)}
        />

        <button type="submit">Login</button>

        <div className="hint">
          <p>
            Customer: <strong>customer@test.com</strong>
          </p>
          <p>
            Admin: <strong>admin@test.com</strong>
          </p>
          <p>
            Password: <strong>Password123!</strong>
          </p>
        </div>
      </form>
    </div>
  );
}