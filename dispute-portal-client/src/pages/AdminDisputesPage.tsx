import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { getAdminDisputes, updateDisputeStatus } from "../api/apiClient";
import StatusBadge from "../components/StatusBadge";
import type { Dispute } from "../types/types";

const statuses = [
  { value: 1, label: "Submitted" },
  { value: 2, label: "UnderReview" },
  { value: 3, label: "MoreInfoRequired" },
  { value: 4, label: "Approved" },
  { value: 5, label: "Rejected" },
  { value: 6, label: "Resolved" },
];

export default function AdminDisputesPage() {
  const [disputes, setDisputes] = useState<Dispute[]>([]);
  const [selectedDispute, setSelectedDispute] = useState<Dispute | null>(null);
  const [status, setStatus] = useState(2);
  const [adminNotes, setAdminNotes] = useState("");
  const [error, setError] = useState("");

  async function loadDisputes() {
    try {
      const data = await getAdminDisputes();
      setDisputes(data);
    } catch {
      setError("Failed to load disputes.");
    }
  }

  useEffect(() => {
    async function init() {
      try {
        const data = await getAdminDisputes();
        setDisputes(data);
      } catch {
        setError("Failed to load disputes.");
      }
    }
    init();
  }, []);

  function startReview(dispute: Dispute) {
    setSelectedDispute(dispute);
    setAdminNotes("");
    setStatus(2);
    setError("");
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    if (!selectedDispute) return;

    try {
      await updateDisputeStatus(selectedDispute.id, {
        status,
        adminNotes,
      });

      setSelectedDispute(null);
      await loadDisputes();
    } catch {
      setError("Failed to update dispute status.");
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Admin Dispute Review</h1>
        <p>Review and update customer dispute cases.</p>
      </div>

      {error && <div className="error">{error}</div>}

      <div className="grid">
        <div className="card">
          <h2>Open Disputes</h2>

          <table>
            <thead>
              <tr>
                <th>Case</th>
                <th>Merchant</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>

            <tbody>
              {disputes.map((dispute) => (
                <tr key={dispute.id}>
                  <td>{dispute.caseNumber}</td>
                  <td>{dispute.merchantName}</td>
                  <td>R {dispute.amount.toFixed(2)}</td>
                  <td>
                    <StatusBadge status={dispute.status} />
                  </td>
                  <td>
                    <button type="button" onClick={() => startReview(dispute)}>
                      Review
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {disputes.length === 0 && <p>No disputes found.</p>}
        </div>

        {selectedDispute && (
          <div className="card form-card">
            <h2>Update Case</h2>
            <p>{selectedDispute.caseNumber}</p>

            <form onSubmit={handleUpdate}>
              <label htmlFor="status">Status</label>
              <select
                id="status"
                value={status}
                onChange={(e) => setStatus(Number(e.target.value))}
              >
                {statuses.map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>

              <label htmlFor="adminNotes">Admin Notes</label>
              <textarea
                id="adminNotes"
                value={adminNotes}
                onChange={(e) => setAdminNotes(e.target.value)}
                rows={5}
                placeholder="Add review notes..."
              />

              <button type="submit">Update Status</button>
            </form>
          </div>
        )}
      </div>
    </div>
  );
}