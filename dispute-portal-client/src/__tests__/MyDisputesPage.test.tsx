import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import MyDisputesPage from '../pages/MyDisputesPage'
import * as apiClient from '../api/apiClient'
import type { Dispute } from '../types/types'

vi.mock('../api/apiClient')

const mockDisputes: Dispute[] = [
  {
    id: 1,
    caseNumber: 'CASE-001',
    transactionId: 10,
    merchantName: 'Coffee Shop',
    amount: 45.50,
    reason: 'I do not recognize this transaction',
    customerNotes: 'Never been there',
    adminNotes: null,
    status: 'Submitted',
    createdAt: '2024-01-15T10:00:00Z',
    resolvedAt: null,
    events: [],
  },
  {
    id: 2,
    caseNumber: 'CASE-002',
    transactionId: 11,
    merchantName: 'Supermarket',
    amount: 200.00,
    reason: 'I was charged twice',
    customerNotes: 'Duplicate charge',
    adminNotes: null,
    status: 'UnderReview',
    createdAt: '2024-01-16T12:00:00Z',
    resolvedAt: null,
    events: [],
  },
]

describe('MyDisputesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.getMyDisputes).mockReturnValue(new Promise(() => {}))
    render(<MemoryRouter><MyDisputesPage /></MemoryRouter>)
    expect(screen.getByText(/loading disputes/i)).toBeInTheDocument()
  })

  it('renders dispute rows after loading', async () => {
    vi.mocked(apiClient.getMyDisputes).mockResolvedValue(mockDisputes)
    render(<MemoryRouter><MyDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText('CASE-001')).toBeInTheDocument()
      expect(screen.getByText('Coffee Shop')).toBeInTheDocument()
      expect(screen.getByText('R 45.50')).toBeInTheDocument()
      expect(screen.getByText('CASE-002')).toBeInTheDocument()
    })
  })

  it('shows error message when the API fails', async () => {
    vi.mocked(apiClient.getMyDisputes).mockRejectedValue(new Error('Network error'))
    render(<MemoryRouter><MyDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/failed to load disputes/i)).toBeInTheDocument()
    })
  })

  it('shows "No disputes found" when the list is empty', async () => {
    vi.mocked(apiClient.getMyDisputes).mockResolvedValue([])
    render(<MemoryRouter><MyDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/no disputes found/i)).toBeInTheDocument()
    })
  })

  it('renders case numbers as links to the dispute detail page', async () => {
    vi.mocked(apiClient.getMyDisputes).mockResolvedValue(mockDisputes)
    render(<MemoryRouter><MyDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      const link = screen.getByRole('link', { name: 'CASE-001' })
      expect(link).toHaveAttribute('href', '/disputes/1')
    })
  })
})
