import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getAdminStats } from "../api/apiClient";
import StatusBadge from "../components/StatusBadge";
import type { AdminStats } from "../types/types";

const STATUS_ORDER = [
  "Submitted",
  "UnderReview",
  "MoreInfoRequired",
  "Approved",
  "Rejected",
  "Resolved",
];

interface StatCardProps {
  label: string;
  value: string | number;
  sub?: string;
  accent?: string;
}

function StatCard({ label, value, sub, accent }: StatCardProps) {
  return (
    <div className="card" style={{ borderTop: `4px solid ${accent ?? "#1d4ed8"}` }}>
      <p style={{ margin: "0 0 0.25rem", fontSize: "0.8rem", color: "#6b7280", textTransform: "uppercase", fontWeight: 700, letterSpacing: "0.05em" }}>
        {label}
      </p>
      <p style={{ margin: "0 0 0.25rem", fontSize: "2rem", fontWeight: 700, color: "#111827" }}>
        {value}
      </p>
      {sub && <p style={{ margin: 0, fontSize: "0.8rem", color: "#6b7280" }}>{sub}</p>}
    </div>
  );
}

export default function AdminDashboardPage() {
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    getAdminStats()
      .then(setStats)
      .catch(() => setError("Failed to load stats."));
  }, []);

  useEffect(() => {
    function onDisputeUpdated() {
      getAdminStats().then(setStats).catch(() => {});
    }
    window.addEventListener("dispute:updated", onDisputeUpdated);
    return () => window.removeEventListener("dispute:updated", onDisputeUpdated);
  }, []);

  if (error) return <div className="page"><div className="error">{error}</div></div>;
  if (!stats) return <div className="page"><p>Loading…</p></div>;

  const resolutionRate = stats.total > 0
    ? Math.round(((stats.byStatus["Resolved"] ?? 0) + (stats.byStatus["Approved"] ?? 0)) / stats.total * 100)
    : 0;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Dashboard</h1>
        <p>Live overview of all dispute activity.</p>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))", gap: "1.25rem", marginBottom: "2rem" }}>
        <StatCard label="Total Disputes" value={stats.total} />
        <StatCard label="Open Disputes" value={stats.openCount} sub="Awaiting resolution" accent="#f59e0b" />
        <StatCard label="Submitted Today" value={stats.submittedToday} accent="#6366f1" />
        <StatCard label="Avg Resolution" value={`${stats.avgResolutionDays}d`} sub="For closed cases" accent="#10b981" />
        <StatCard label="Resolution Rate" value={`${resolutionRate}%`} sub="Approved + Resolved" accent="#0ea5e9" />
      </div>

      <div className="card">
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "1rem" }}>
          <h2 style={{ margin: 0 }}>Disputes by Status</h2>
          <Link to="/admin/disputes" style={{ fontSize: "0.875rem" }}>View all →</Link>
        </div>

        <table>
          <thead>
            <tr>
              <th>Status</th>
              <th>Count</th>
              <th style={{ width: "50%" }}>Distribution</th>
            </tr>
          </thead>
          <tbody>
            {STATUS_ORDER.map((status) => {
              const count = stats.byStatus[status] ?? 0;
              const pct = stats.total > 0 ? Math.round((count / stats.total) * 100) : 0;
              return (
                <tr key={status}>
                  <td><StatusBadge status={status} /></td>
                  <td><strong>{count}</strong></td>
                  <td>
                    <div style={{ display: "flex", alignItems: "center", gap: "0.6rem" }}>
                      <div style={{ flex: 1, background: "#f3f4f6", borderRadius: "999px", height: "8px", overflow: "hidden" }}>
                        <div style={{ width: `${pct}%`, height: "100%", background: "#1d4ed8", borderRadius: "999px", transition: "width 0.4s ease" }} />
                      </div>
                      <span style={{ fontSize: "0.8rem", color: "#6b7280", minWidth: "2.5rem" }}>{pct}%</span>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
