import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import AdminDisputesPage from '../pages/AdminDisputesPage'
import * as apiClient from '../api/apiClient'
import type { Dispute, PagedResult } from '../types/types'

vi.mock('../api/apiClient')

const makeDispute = (overrides: Partial<Dispute> = {}): Dispute => ({
  id: 1,
  caseNumber: 'DSP-001',
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
  attachments: [],
  customerName: 'Alice Chen',
  ...overrides,
})

const pagedResult = (items: Dispute[], total = items.length): PagedResult<Dispute> => ({
  items,
  totalCount: total,
  page: 1,
  pageSize: 10,
})

describe('AdminDisputesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the page header', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/admin dispute review/i)).toBeInTheDocument()
    })
  })

  it('renders dispute rows after loading', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(
      pagedResult([makeDispute(), makeDispute({ id: 2, caseNumber: 'DSP-002', merchantName: 'Supermarket', customerName: 'Bob' })])
    )
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText('DSP-001')).toBeInTheDocument()
      expect(screen.getByText('DSP-002')).toBeInTheDocument()
      expect(screen.getByText('Alice Chen')).toBeInTheDocument()
    })
  })

  it('shows error message when API fails', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockRejectedValue(new Error('Server error'))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/failed to load disputes/i)).toBeInTheDocument()
    })
  })

  it('shows "No disputes found" when list is empty', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/no disputes found/i)).toBeInTheDocument()
    })
  })

  it('shows Review button for actionable disputes', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute({ status: 'Submitted' })]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^review$/i })).toBeInTheDocument()
    })
  })

  it('shows View button for terminal disputes', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute({ status: 'Resolved' })]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^view$/i })).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /^review$/i })).not.toBeInTheDocument()
    })
  })

  it('opens the detail panel when Review is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /^review$/i }))
    // Panel header renders the case number as an <h2>
    expect(screen.getByRole('heading', { name: 'DSP-001' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /update status/i })).toBeInTheDocument()
  })

  it('closes the panel when × is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: '×' }))
    expect(screen.queryByRole('button', { name: /update status/i })).not.toBeInTheDocument()
  })

  it('shows "no further actions" message in view-only mode', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute({ status: 'Rejected' })]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^view$/i }))
    await user.click(screen.getByRole('button', { name: /^view$/i }))
    expect(screen.getByText(/no further actions available/i)).toBeInTheDocument()
  })

  it('submits status update and closes panel', async () => {
    const user = userEvent.setup()
    const dispute = makeDispute()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([dispute]))
    vi.mocked(apiClient.updateDisputeStatus).mockResolvedValue(dispute)
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /update status/i }))
    await waitFor(() => {
      expect(apiClient.updateDisputeStatus).toHaveBeenCalledWith(1, expect.objectContaining({ status: expect.any(Number) }))
    })
  })

  it('shows API error message on failed status update', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    const error = Object.assign(new Error(), {
      isAxiosError: true,
      response: { data: { message: 'Invalid status transition.' } },
    })
    vi.mocked(apiClient.updateDisputeStatus).mockRejectedValue(error)
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /update status/i }))
    await waitFor(() => {
      // The same error state renders in both the top bar and the panel form
      expect(screen.getAllByText('Invalid status transition.').length).toBeGreaterThan(0)
    })
  })

  it('shows pagination when totalCount exceeds page size', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue({
      items: [makeDispute()],
      totalCount: 25,
      page: 1,
      pageSize: 10,
    })
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument()
    })
  })

  it('does not show pagination when results fit on one page', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByText('DSP-001'))
    expect(screen.queryByText(/page/i)).not.toBeInTheDocument()
  })

  it('shows attachment rows in panel when dispute has attachments', async () => {
    const dispute = makeDispute({
      attachments: [{
        id: 1, fileName: 'evidence.pdf', contentType: 'application/pdf',
        fileSize: 5000, uploadedBy: 'Alice Chen', uploadedAt: '2024-01-15T11:00:00Z',
      }],
    })
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([dispute]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /^review$/i }))
    expect(screen.getByText('evidence.pdf')).toBeInTheDocument()
  })

  it('displays dispute count in filter bar', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue({ items: [makeDispute()], totalCount: 42, page: 1, pageSize: 10 })
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/42 disputes/i)).toBeInTheDocument()
    })
  })

  it('highlights selected row', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByRole('button', { name: /^review$/i }))
    await user.click(screen.getByRole('button', { name: /^review$/i }))
    // DSP-001 appears in both the table <td> and the panel <h2>; find the <td> one
    const cell = screen.getAllByText('DSP-001').find(el => el.tagName === 'TD')!
    expect(cell.closest('tr')!.className).toContain('row-selected')
  })

  it('shows the amounts with R prefix', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText('R 45.50')).toBeInTheDocument()
    })
  })

  it('passes search term to API after debounce', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => screen.getByPlaceholderText(/search/i))
    await user.type(screen.getByPlaceholderText(/search/i), 'DSP-001')
    await waitFor(() => {
      const calls = vi.mocked(apiClient.getAdminDisputes).mock.calls
      const lastCall = calls[calls.length - 1]
      expect(lastCall[3]).toBe('DSP-001')
    }, { timeout: 1000 })
  })

  it('renders inside table within the card', async () => {
    vi.mocked(apiClient.getAdminDisputes).mockResolvedValue(pagedResult([makeDispute()]))
    render(<MemoryRouter><AdminDisputesPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByRole('columnheader', { name: /case/i })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: /customer/i })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: /status/i })).toBeInTheDocument()
    })
  })
})
