import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getTransactions } from "../api/apiClient";
import type { Transaction } from "../types/types";

export default function TransactionsPage() {
  const navigate = useNavigate();

  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const data = await getTransactions();
        setTransactions(data);
      } catch {
        setError("Failed to load transactions. Please try again.");
      } finally {
        setLoading(false);
      }
    }

    load();
  }, []);

  const filtered = transactions.filter((transaction) => {
    const text = `${transaction.reference} ${transaction.merchantName} ${transaction.description}`.toLowerCase();
    return text.includes(search.toLowerCase());
  });

  if (loading) return <p>Loading transactions...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Transactions</h1>
        <p>View your recent transactions and raise disputes.</p>
      </div>

      <input
        className="search"
        placeholder="Search transactions..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      <div className="card">
        <table>
          <thead>
            <tr>
              <th>Reference</th>
              <th>Merchant</th>
              <th>Amount</th>
              <th>Date</th>
              <th>Dispute</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((transaction) => (
              <tr key={transaction.id}>
                <td>{transaction.reference}</td>
                <td>{transaction.merchantName}</td>
                <td>R {transaction.amount.toFixed(2)}</td>
                <td>{new Date(transaction.transactionDate).toLocaleDateString()}</td>
                <td>
                  {transaction.hasDispute ? (
                    <span className="muted">Already disputed</span>
                  ) : transaction.isDisputable ? (
                    <button
                      onClick={() =>
                        navigate(`/transactions/${transaction.id}/dispute`)
                      }
                    >
                      Dispute
                    </button>
                  ) : (
                    <span className="muted">Not eligible</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {filtered.length === 0 && <p>No transactions found.</p>}
      </div>
    </div>
  );
}