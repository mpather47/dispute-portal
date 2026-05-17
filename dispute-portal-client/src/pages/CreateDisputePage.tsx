import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createDispute } from "../api/apiClient";
import axios from "axios";

export default function CreateDisputePage() {
  const navigate = useNavigate();
  const { id } = useParams();

  const [reason, setReason] = useState("I do not recognize this transaction");
  const [customerNotes, setCustomerNotes] = useState("");
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    if (!id) {
      setError("Missing transaction ID.");
      return;
    }

    if (!reason.trim() || !customerNotes.trim()) {
      setError("Please provide a reason and notes.");
      return;
    }

    try {
      const dispute = await createDispute({
        transactionId: Number(id),
        reason,
        customerNotes,
      });

      navigate(`/disputes/${dispute.id}`);
    } catch (err: unknown) {
      if (axios.isAxiosError(err)) {
        setError(
          err.response?.data?.message ?? "Failed to create dispute."
        );
      } else {
        setError("Failed to create dispute.");
      }
    }
  }

  return (
    <div className="page">
      <div className="card form-card">
        <h1>Create Dispute</h1>
        <p>Submit a dispute for transaction #{id}</p>

        {error && <div className="error">{error}</div>}

        <form onSubmit={handleSubmit}>
          <label htmlFor="reason">Reason</label>
          <select
            id="reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          >
            <option>I do not recognize this transaction</option>
            <option>I was charged twice</option>
            <option>The amount is incorrect</option>
            <option>I cancelled this purchase</option>
            <option>Goods or services were not received</option>
          </select>

          <label htmlFor="customerNotes">Notes</label>
          <textarea
            id="customerNotes"
            value={customerNotes}
            onChange={(e) => setCustomerNotes(e.target.value)}
            placeholder="Explain what happened..."
            rows={6}
          />

          <button type="submit">Submit Dispute</button>
        </form>
      </div>
    </div>
  );
}