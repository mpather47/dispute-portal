import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getDisputeById } from "../api/apiClient";
import StatusBadge from "../components/StatusBadge";
import type { Dispute } from "../types/types";

export default function DisputeDetailsPage() {
  const { id } = useParams();

  const [dispute, setDispute] = useState<Dispute | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      if (!id) return;

      try {
        const data = await getDisputeById(Number(id));
        setDispute(data);
      } catch {
        setError("Failed to load dispute. Please try again.");
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [id]);

  useEffect(() => {
    function onDisputeUpdated() {
      if (!id) return;
      getDisputeById(Number(id))
        .then((data) => setDispute(data))
        .catch(() => setError("Failed to load dispute. Please try again."));
    }
    window.addEventListener("dispute:updated", onDisputeUpdated);
    return () => window.removeEventListener("dispute:updated", onDisputeUpdated);
  }, [id]);

  if (loading) return <p>Loading dispute...</p>;
  if (error) return <p className="error">{error}</p>;
  if (!dispute) return <p>Dispute not found.</p>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>{dispute.caseNumber}</h1>
        <StatusBadge status={dispute.status} />
      </div>

      <div className="grid">
        <div className="card">
          <h2>Dispute Details</h2>
          <p>
            <strong>Merchant:</strong> {dispute.merchantName}
          </p>
          <p>
            <strong>Amount:</strong> R {dispute.amount.toFixed(2)}
          </p>
          <p>
            <strong>Reason:</strong> {dispute.reason}
          </p>
          <p>
            <strong>Customer Notes:</strong> {dispute.customerNotes}
          </p>

          {dispute.adminNotes && (
            <p>
              <strong>Admin Notes:</strong> {dispute.adminNotes}
            </p>
          )}
        </div>

        <div className="card">
          <h2>Status Timeline</h2>

          <div className="timeline">
            {dispute.events.map((event, index) => (
              <div className="timeline-item" key={index}>
                <div className="timeline-dot" />
                <div>
                  <strong>{event.status}</strong>
                  <p>{event.message}</p>
                  <small>
                    {event.createdBy} ·{" "}
                    {new Date(event.createdAt).toLocaleString()}
                  </small>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}