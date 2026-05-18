import { useEffect, useState } from "react";

import { isAxiosError } from "axios";
import { getAdminDisputes, updateDisputeStatus } from "../api/apiClient";
import StatusBadge from "../components/StatusBadge";
import type { Dispute } from "../types/types";

const PAGE_SIZE = 10;

const allStatuses = [
  { value: 1, label: "Submitted" },
  { value: 2, label: "UnderReview" },
  { value: 3, label: "MoreInfoRequired" },
  { value: 4, label: "Approved" },
  { value: 5, label: "Rejected" },
  { value: 6, label: "Resolved" },
];

const validTransitions: Record<string, number[]> = {
  Submitted: [2, 3, 5],
  UnderReview: [3, 4, 5, 6],
  MoreInfoRequired: [2, 5],
  Approved: [6],
  Rejected: [],
  Resolved: [],
};

export default function AdminDisputesPage() {
  const [disputes, setDisputes] = useState<Dispute[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [selectedDispute, setSelectedDispute] = useState<Dispute | null>(null);
  const [panelMode, setPanelMode] = useState<"view" | "review">("view");
  const [newStatus, setNewStatus] = useState(2);
  const [adminNotes, setAdminNotes] = useState("");
  const [error, setError] = useState("");

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  // Debounce search input → search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    async function init() {
      try {
        const result = await getAdminDisputes(page, PAGE_SIZE, statusFilter || undefined, search || undefined);
        setDisputes(result.items);
        setTotalCount(result.totalCount);
        setSelectedDispute((prev) =>
          prev ? (result.items.find((d) => d.id === prev.id) ?? prev) : null
        );
      } catch {
        setError("Failed to load disputes.");
      }
    }
    init();
  }, [page, statusFilter, search]);

  useEffect(() => {
    function onDisputeUpdated() {
      getAdminDisputes(page, PAGE_SIZE, statusFilter || undefined, search || undefined)
        .then((result) => {
          setDisputes(result.items);
          setTotalCount(result.totalCount);
          setSelectedDispute((prev) =>
            prev ? (result.items.find((d) => d.id === prev.id) ?? prev) : null
          );
        })
        .catch(() => {});
    }
    window.addEventListener("dispute:updated", onDisputeUpdated);
    return () => window.removeEventListener("dispute:updated", onDisputeUpdated);
  }, [page, statusFilter, search]);

  function openPanel(dispute: Dispute, mode: "view" | "review") {
    setSelectedDispute(dispute);
    setPanelMode(mode);
    setAdminNotes("");
    setError("");
    if (mode === "review") {
      const allowed = validTransitions[dispute.status] ?? [];
      setNewStatus(allowed[0] ?? 2);
    }
  }

  async function handleUpdate(e: { preventDefault(): void }) {
    e.preventDefault();
    if (!selectedDispute) return;
    setError("");
    try {
      await updateDisputeStatus(selectedDispute.id, { status: newStatus, adminNotes });
      setSelectedDispute(null);
      const result = await getAdminDisputes(page, PAGE_SIZE, statusFilter || undefined);
      setDisputes(result.items);
      setTotalCount(result.totalCount);
    } catch (err: unknown) {
      setError(
        isAxiosError(err)
          ? (err.response?.data as { message?: string })?.message ?? "Failed to update."
          : "Failed to update."
      );
    }
  }

  function handleFilterChange(value: number | "") {
    setStatusFilter(value);
    setPage(1);
    setSelectedDispute(null);
  }

  function handleSearchChange(value: string) {
    setSearchInput(value);
    setSelectedDispute(null);
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Admin Dispute Review</h1>
        <p>Review and update customer dispute cases.</p>
      </div>

      <div className="filter-bar">
        <input
          type="search"
          placeholder="Search by case or customer…"
          value={searchInput}
          onChange={(e) => handleSearchChange(e.target.value)}
          className="search-input"
        />
        <label htmlFor="statusFilter" className="filter-label">Status</label>
        <select
          id="statusFilter"
          value={statusFilter}
          onChange={(e) =>
            handleFilterChange(e.target.value === "" ? "" : Number(e.target.value))
          }
        >
          <option value="">All</option>
          {allStatuses.map((s) => (
            <option key={s.value} value={s.value}>
              {s.label}
            </option>
          ))}
        </select>
        <span className="muted">{totalCount} dispute{totalCount !== 1 ? "s" : ""}</span>
      </div>

      {error && <div className="error">{error}</div>}

      <div className="grid">
        <div className="card">
          <table>
            <thead>
              <tr>
                <th>Case</th>
                <th>Customer</th>
                <th>Merchant</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {disputes.map((dispute) => {
                const hasTransitions = (validTransitions[dispute.status]?.length ?? 0) > 0;
                return (
                  <tr
                    key={dispute.id}
                    className={selectedDispute?.id === dispute.id ? "row-selected" : ""}
                  >
                    <td>{dispute.caseNumber}</td>
                    <td>{dispute.customerName ?? "—"}</td>
                    <td>{dispute.merchantName}</td>
                    <td>R {dispute.amount.toFixed(2)}</td>
                    <td>
                      <StatusBadge status={dispute.status} />
                    </td>
                    <td className="action-cell">
                      {hasTransitions ? (
                        <button type="button" onClick={() => openPanel(dispute, "review")}>
                          Review
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="btn-secondary"
                          onClick={() => openPanel(dispute, "view")}
                        >
                          View
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>

          {disputes.length === 0 && <p>No disputes found.</p>}

          {totalPages > 1 && (
            <div className="pagination">
              <button
                type="button"
                disabled={page === 1}
                onClick={() => setPage((p) => p - 1)}
              >
                ← Prev
              </button>
              <span>
                Page {page} of {totalPages}
              </span>
              <button
                type="button"
                disabled={page === totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Next →
              </button>
            </div>
          )}
        </div>

        {selectedDispute && (
          <div className="card form-card">
            <div className="panel-header">
              <h2>{selectedDispute.caseNumber}</h2>
              <button
                type="button"
                className="btn-close"
                onClick={() => setSelectedDispute(null)}
              >
                ×
              </button>
            </div>

            <div className="dispute-meta">
              {selectedDispute.customerName && (
                <p>
                  <strong>Customer:</strong> {selectedDispute.customerName}
                </p>
              )}
              <p>
                <strong>Merchant:</strong> {selectedDispute.merchantName}
              </p>
              <p>
                <strong>Amount:</strong> R {selectedDispute.amount.toFixed(2)}
              </p>
              <p>
                <strong>Reason:</strong> {selectedDispute.reason}
              </p>
              {selectedDispute.customerNotes && (
                <p>
                  <strong>Customer Notes:</strong> {selectedDispute.customerNotes}
                </p>
              )}
              {selectedDispute.adminNotes && (
                <p>
                  <strong>Admin Notes:</strong> {selectedDispute.adminNotes}
                </p>
              )}
            </div>

            <h3>Timeline</h3>
            <div className="timeline">
              {selectedDispute.events.map((event, index) => (
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

            {panelMode === "review" ? (
              <>
                <h3>Update Status</h3>
                <form onSubmit={handleUpdate}>
                  <label htmlFor="status">New Status</label>
                  <select
                    id="status"
                    value={newStatus}
                    onChange={(e) => setNewStatus(Number(e.target.value))}
                  >
                    {allStatuses
                      .filter((s) =>
                        (validTransitions[selectedDispute.status] ?? []).includes(s.value)
                      )
                      .map((item) => (
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
                    rows={4}
                    placeholder="Add review notes..."
                  />

                  {error && <div className="error">{error}</div>}
                  <button type="submit">Update Status</button>
                </form>
              </>
            ) : (
              <p className="muted">No further actions available for this case.</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
