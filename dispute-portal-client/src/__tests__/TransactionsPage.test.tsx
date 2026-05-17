import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import TransactionsPage from '../pages/TransactionsPage'
import * as apiClient from '../api/apiClient'
import type { Transaction } from '../types/types'

const mockNavigate = vi.hoisted(() => vi.fn())

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

vi.mock('../api/apiClient')

const mockTransactions: Transaction[] = [
  {
    id: 1,
    reference: 'TXN-001',
    merchantName: 'Coffee Shop',
    amount: 45.50,
    type: 'Debit',
    transactionDate: '2024-01-15T10:00:00Z',
    description: 'Morning coffee',
    isDisputable: true,
    hasDispute: false,
  },
  {
    id: 2,
    reference: 'TXN-002',
    merchantName: 'Supermarket',
    amount: 200.00,
    type: 'Debit',
    transactionDate: '2024-01-16T12:00:00Z',
    description: 'Groceries',
    isDisputable: true,
    hasDispute: true,
  },
  {
    id: 3,
    reference: 'TXN-003',
    merchantName: 'ATM Withdrawal',
    amount: 500.00,
    type: 'Debit',
    transactionDate: '2024-01-17T14:00:00Z',
    description: 'Cash withdrawal',
    isDisputable: false,
    hasDispute: false,
  },
]

describe('TransactionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.getTransactions).mockReturnValue(new Promise(() => {}))
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    expect(screen.getByText(/loading transactions/i)).toBeInTheDocument()
  })

  it('renders transaction rows after loading', async () => {
    vi.mocked(apiClient.getTransactions).mockResolvedValue(mockTransactions)
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText('TXN-001')).toBeInTheDocument()
      expect(screen.getByText('Coffee Shop')).toBeInTheDocument()
      expect(screen.getByText('R 45.50')).toBeInTheDocument()
    })
  })

  it('shows error message when the API fails', async () => {
    vi.mocked(apiClient.getTransactions).mockRejectedValue(new Error('Network error'))
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/failed to load transactions/i)).toBeInTheDocument()
    })
  })

  it('filters transactions by search input', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getTransactions).mockResolvedValue(mockTransactions)
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => screen.getByText('TXN-001'))
    await user.type(screen.getByPlaceholderText(/search/i), 'Coffee')
    expect(screen.getByText('TXN-001')).toBeInTheDocument()
    expect(screen.queryByText('TXN-002')).not.toBeInTheDocument()
  })

  it('shows "No transactions found" when search yields no results', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getTransactions).mockResolvedValue(mockTransactions)
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => screen.getByText('TXN-001'))
    await user.type(screen.getByPlaceholderText(/search/i), 'xyz-no-match')
    expect(screen.getByText(/no transactions found/i)).toBeInTheDocument()
  })

  it('shows "Already disputed" for transactions with an existing dispute', async () => {
    vi.mocked(apiClient.getTransactions).mockResolvedValue(mockTransactions)
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => screen.getByText('TXN-002'))
    expect(screen.getByText('Already disputed')).toBeInTheDocument()
  })

  it('shows "Not eligible" for non-disputable transactions', async () => {
    vi.mocked(apiClient.getTransactions).mockResolvedValue(mockTransactions)
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => screen.getByText('TXN-003'))
    expect(screen.getByText('Not eligible')).toBeInTheDocument()
  })

  it('navigates to dispute creation when Dispute button is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getTransactions).mockResolvedValue(mockTransactions)
    render(<MemoryRouter><TransactionsPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^dispute$/i }))
    await user.click(screen.getByRole('button', { name: /^dispute$/i }))
    expect(mockNavigate).toHaveBeenCalledWith('/transactions/1/dispute')
  })
})
