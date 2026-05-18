import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import DisputeDetailsPage from '../pages/DisputeDetailsPage'
import * as apiClient from '../api/apiClient'
import type { Dispute } from '../types/types'

vi.mock('../api/apiClient')

const baseDispute: Dispute = {
  id: 7,
  caseNumber: 'DSP-20240115-ABCD1234',
  transactionId: 10,
  merchantName: 'Tech Store',
  amount: 1299.99,
  reason: 'I do not recognize this transaction',
  customerNotes: 'Never visited this store',
  adminNotes: null,
  status: 'Submitted',
  createdAt: '2024-01-15T10:00:00Z',
  resolvedAt: null,
  events: [
    { status: 'Submitted', message: 'Dispute submitted by customer.', createdBy: 'Customer', createdAt: '2024-01-15T10:00:00Z' },
  ],
  attachments: [],
}

function renderPage(id = '7') {
  return render(
    <MemoryRouter initialEntries={[`/disputes/${id}`]}>
      <Routes>
        <Route path="/disputes/:id" element={<DisputeDetailsPage />} />
      </Routes>
    </MemoryRouter>
  )
}

describe('DisputeDetailsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.getDisputeById).mockReturnValue(new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading dispute/i)).toBeInTheDocument()
  })

  it('renders dispute details after loading', async () => {
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(baseDispute)
    renderPage()
    await waitFor(() => {
      expect(screen.getByText('DSP-20240115-ABCD1234')).toBeInTheDocument()
      expect(screen.getByText('Tech Store')).toBeInTheDocument()
      expect(screen.getByText('R 1299.99')).toBeInTheDocument()
      expect(screen.getByText('Never visited this store')).toBeInTheDocument()
    })
  })

  it('renders timeline events', async () => {
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(baseDispute)
    renderPage()
    await waitFor(() => {
      expect(screen.getByText('Dispute submitted by customer.')).toBeInTheDocument()
    })
  })

  it('shows error when API fails', async () => {
    vi.mocked(apiClient.getDisputeById).mockRejectedValue(new Error('Not found'))
    renderPage()
    await waitFor(() => {
      expect(screen.getByText(/failed to load dispute/i)).toBeInTheDocument()
    })
  })

  it('renders admin notes when present', async () => {
    const dispute = { ...baseDispute, adminNotes: 'Please provide your receipt.' }
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(dispute)
    renderPage()
    await waitFor(() => {
      expect(screen.getByText('Please provide your receipt.')).toBeInTheDocument()
    })
  })

  it('does NOT show reply form when status is Submitted', async () => {
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(baseDispute)
    renderPage()
    await waitFor(() => screen.getByText('DSP-20240115-ABCD1234'))
    expect(screen.queryByRole('button', { name: /send reply/i })).not.toBeInTheDocument()
  })

  it('shows reply form only when status is MoreInfoRequired', async () => {
    const dispute = { ...baseDispute, status: 'MoreInfoRequired' }
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(dispute)
    renderPage()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /send reply/i })).toBeInTheDocument()
      expect(screen.getByPlaceholderText(/provide the requested information/i)).toBeInTheDocument()
    })
  })

  it('submits reply and updates dispute', async () => {
    const user = userEvent.setup()
    const dispute = { ...baseDispute, status: 'MoreInfoRequired' }
    const updatedDispute = { ...baseDispute, status: 'UnderReview' }
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(dispute)
    vi.mocked(apiClient.replyToDispute).mockResolvedValue(updatedDispute)
    renderPage()
    await waitFor(() => screen.getByPlaceholderText(/provide the requested information/i))
    await user.type(screen.getByPlaceholderText(/provide the requested information/i), 'Here is my receipt info')
    await user.click(screen.getByRole('button', { name: /send reply/i }))
    await waitFor(() => {
      expect(apiClient.replyToDispute).toHaveBeenCalledWith(7, 'Here is my receipt info')
    })
  })

  it('shows reply API error message on failure', async () => {
    const user = userEvent.setup()
    const dispute = { ...baseDispute, status: 'MoreInfoRequired' }
    const error = Object.assign(new Error(), {
      isAxiosError: true,
      response: { data: { message: 'Reply window has closed.' } },
    })
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(dispute)
    vi.mocked(apiClient.replyToDispute).mockRejectedValue(error)
    renderPage()
    await waitFor(() => screen.getByPlaceholderText(/provide the requested information/i))
    await user.type(screen.getByPlaceholderText(/provide the requested information/i), 'My info')
    await user.click(screen.getByRole('button', { name: /send reply/i }))
    await waitFor(() => {
      expect(screen.getByText('Reply window has closed.')).toBeInTheDocument()
    })
  })

  it('shows attachments table when attachments exist', async () => {
    const dispute = {
      ...baseDispute,
      attachments: [{
        id: 1, fileName: 'receipt.pdf', contentType: 'application/pdf',
        fileSize: 12345, uploadedBy: 'Customer', uploadedAt: '2024-01-15T11:00:00Z',
      }],
    }
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(dispute)
    renderPage()
    await waitFor(() => {
      expect(screen.getByText('receipt.pdf')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /download/i })).toBeInTheDocument()
    })
  })

  it('shows "No attachments yet" when list is empty', async () => {
    vi.mocked(apiClient.getDisputeById).mockResolvedValue(baseDispute)
    renderPage()
    await waitFor(() => {
      expect(screen.getByText(/no attachments yet/i)).toBeInTheDocument()
    })
  })
})
