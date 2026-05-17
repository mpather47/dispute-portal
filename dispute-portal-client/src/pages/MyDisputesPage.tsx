import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyDisputes } from "../api/apiClient";
import StatusBadge from "../components/StatusBadge";
import type { Dispute } from "../types/types";

export default function MyDisputesPage() {
  const [disputes, setDisputes] = useState<Dispute[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const data = await getMyDisputes();
        setDisputes(data);
      } catch {
        setError("Failed to load disputes. Please try again.");
      } finally {
        setLoading(false);
      }
    }

    load();
  }, []);

  if (loading) return <p>Loading disputes...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>My Disputes</h1>
        <p>Track your submitted dispute cases.</p>
      </div>

      <div className="card">
        <table>
          <thead>
            <tr>
              <th>Case</th>
              <th>Merchant</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Created</th>
            </tr>
          </thead>

          <tbody>
            {disputes.map((dispute) => (
              <tr key={dispute.id}>
                <td>
                  <Link to={`/disputes/${dispute.id}`}>
                    {dispute.caseNumber}
                  </Link>
                </td>
                <td>{dispute.merchantName}</td>
                <td>R {dispute.amount.toFixed(2)}</td>
                <td>
                  <StatusBadge status={dispute.status} />
                </td>
                <td>{new Date(dispute.createdAt).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {disputes.length === 0 && <p>No disputes found.</p>}
      </div>
    </div>
  );
}