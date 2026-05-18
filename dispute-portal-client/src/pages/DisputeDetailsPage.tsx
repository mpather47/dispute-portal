import { isAxiosError } from "axios";
import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import {
  downloadAttachment,
  getDisputeById,
  replyToDispute,
  uploadAttachment,
} from "../api/apiClient";
import StatusBadge from "../components/StatusBadge";
import type { Dispute } from "../types/types";

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function DisputeDetailsPage() {
  const { id } = useParams();

  const [dispute, setDispute] = useState<Dispute | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [replyText, setReplyText] = useState("");
  const [replyError, setReplyError] = useState("");
  const [replyLoading, setReplyLoading] = useState(false);

  const [uploadError, setUploadError] = useState("");
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

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

  async function handleReply(e: { preventDefault(): void }) {
    e.preventDefault();
    if (!dispute || !replyText.trim()) return;
    setReplyLoading(true);
    setReplyError("");
    try {
      const updated = await replyToDispute(dispute.id, replyText.trim());
      setDispute(updated);
      setReplyText("");
    } catch (err) {
      setReplyError(
        isAxiosError(err)
          ? (err.response?.data as { message?: string })?.message ?? "Failed to send reply."
          : "Failed to send reply."
      );
    } finally {
      setReplyLoading(false);
    }
  }

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file || !dispute) return;
    setUploading(true);
    setUploadError("");
    try {
      const attachment = await uploadAttachment(dispute.id, file);
      setDispute((prev) =>
        prev ? { ...prev, attachments: [...prev.attachments, attachment] } : prev
      );
    } catch (err) {
      setUploadError(
        isAxiosError(err)
          ? (err.response?.data as { message?: string })?.message ?? "Upload failed."
          : "Upload failed."
      );
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

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
        <div>
          <div className="card" style={{ marginBottom: "1.5rem" }}>
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

          {dispute.status === "MoreInfoRequired" && (
            <div className="card" style={{ marginBottom: "1.5rem" }}>
              <h2>Reply to Admin</h2>
              <p className="muted" style={{ marginTop: 0 }}>
                The admin has requested more information. Please provide your response below.
              </p>
              <form onSubmit={handleReply}>
                <label htmlFor="replyText">Your Response</label>
                <textarea
                  id="replyText"
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  rows={4}
                  placeholder="Provide the requested information..."
                />
                {replyError && <div className="error">{replyError}</div>}
                <button type="submit" disabled={replyLoading || !replyText.trim()}>
                  {replyLoading ? "Sending…" : "Send Reply"}
                </button>
              </form>
            </div>
          )}

          <div className="card">
            <h2>Attachments</h2>
            {dispute.attachments.length === 0 ? (
              <p className="muted">No attachments yet.</p>
            ) : (
              <table>
                <thead>
                  <tr>
                    <th>File</th>
                    <th>Size</th>
                    <th>Uploaded by</th>
                    <th>Date</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {dispute.attachments.map((a) => (
                    <tr key={a.id}>
                      <td>{a.fileName}</td>
                      <td>{formatBytes(a.fileSize)}</td>
                      <td>{a.uploadedBy}</td>
                      <td>{new Date(a.uploadedAt).toLocaleString()}</td>
                      <td>
                        <button
                          type="button"
                          className="btn-secondary"
                          style={{ fontSize: "0.8rem", padding: "0.3rem 0.6rem" }}
                          onClick={() => downloadAttachment(dispute.id, a.id, a.fileName)}
                        >
                          Download
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <div style={{ marginTop: "1rem" }}>
              <label htmlFor="fileUpload" style={{ display: "block", marginBottom: "0.5rem" }}>
                Upload Attachment
              </label>
              <input
                id="fileUpload"
                ref={fileInputRef}
                type="file"
                accept="image/*,.pdf,.txt"
                onChange={handleFileChange}
                disabled={uploading}
                style={{ marginBottom: 0 }}
              />
              {uploading && <p className="muted" style={{ marginTop: "0.5rem" }}>Uploading…</p>}
              {uploadError && <div className="error" style={{ marginTop: "0.5rem" }}>{uploadError}</div>}
            </div>
          </div>
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
                    {event.createdBy} · {new Date(event.createdAt).toLocaleString()}
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
