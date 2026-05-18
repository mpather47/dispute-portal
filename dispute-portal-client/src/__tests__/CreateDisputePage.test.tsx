import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import CreateDisputePage from '../pages/CreateDisputePage'
import * as apiClient from '../api/apiClient'
import type { Dispute } from '../types/types'

const mockNavigate = vi.hoisted(() => vi.fn())

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

vi.mock('../api/apiClient')

const mockDispute: Dispute = {
  id: 5,
  caseNumber: 'CASE-005',
  transactionId: 42,
  merchantName: 'Coffee Shop',
  amount: 45.50,
  reason: 'I do not recognize this transaction',
  customerNotes: 'Never been there',
  adminNotes: null,
  status: 'Submitted',
  createdAt: '2024-01-15T10:00:00Z',
  resolvedAt: null,
  events: [],
  attachments: [],
}

function renderWithRoute(transactionId = '42') {
  return render(
    <MemoryRouter initialEntries={[`/transactions/${transactionId}/dispute`]}>
      <Routes>
        <Route path="/transactions/:id/dispute" element={<CreateDisputePage />} />
      </Routes>
    </MemoryRouter>
  )
}

describe('CreateDisputePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders reason select, notes textarea and submit button', () => {
    renderWithRoute()
    expect(screen.getByLabelText(/reason/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/notes/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /submit dispute/i })).toBeInTheDocument()
  })

  it('shows validation error when notes are empty', async () => {
    const user = userEvent.setup()
    renderWithRoute()
    await user.click(screen.getByRole('button', { name: /submit dispute/i }))
    expect(screen.getByText(/please provide a reason and notes/i)).toBeInTheDocument()
  })

  it('submits the form and navigates to the new dispute on success', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.createDispute).mockResolvedValue(mockDispute)
    renderWithRoute()
    await user.type(screen.getByLabelText(/notes/i), 'I never made this purchase')
    await user.click(screen.getByRole('button', { name: /submit dispute/i }))
    await waitFor(() => {
      expect(apiClient.createDispute).toHaveBeenCalledWith({
        transactionId: 42,
        reason: 'I do not recognize this transaction',
        customerNotes: 'I never made this purchase',
      })
      expect(mockNavigate).toHaveBeenCalledWith('/disputes/5')
    })
  })

  it('shows the API error message when submission fails with an Axios error', async () => {
    const user = userEvent.setup()
    const error = Object.assign(new Error('Bad request'), {
      isAxiosError: true,
      response: { data: { message: 'Transaction already disputed' } },
    })
    vi.mocked(apiClient.createDispute).mockRejectedValue(error)
    renderWithRoute()
    await user.type(screen.getByLabelText(/notes/i), 'I never made this purchase')
    await user.click(screen.getByRole('button', { name: /submit dispute/i }))
    await waitFor(() => {
      expect(screen.getByText('Transaction already disputed')).toBeInTheDocument()
    })
  })

  it('shows a generic error message for non-Axios errors', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.createDispute).mockRejectedValue(new Error('Unknown error'))
    renderWithRoute()
    await user.type(screen.getByLabelText(/notes/i), 'I never made this purchase')
    await user.click(screen.getByRole('button', { name: /submit dispute/i }))
    await waitFor(() => {
      expect(screen.getByText(/failed to create dispute/i)).toBeInTheDocument()
    })
  })
})
