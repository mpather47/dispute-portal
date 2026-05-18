import { render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import AdminDashboardPage from '../pages/AdminDashboardPage'
import * as apiClient from '../api/apiClient'
import type { AdminStats } from '../types/types'

vi.mock('../api/apiClient')

const mockStats: AdminStats = {
  total: 20,
  openCount: 8,
  submittedToday: 3,
  avgResolutionDays: 4.5,
  byStatus: {
    Submitted: 4,
    UnderReview: 3,
    MoreInfoRequired: 1,
    Approved: 5,
    Rejected: 2,
    Resolved: 5,
  },
}

describe('AdminDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.getAdminStats).mockReturnValue(new Promise(() => {}))
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('renders the stat cards after loading', async () => {
    vi.mocked(apiClient.getAdminStats).mockResolvedValue(mockStats)
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      // Scope each numeric value to its labelled card to avoid clashes with the status table
      const totalCard = screen.getByText(/total disputes/i).closest('.card') as HTMLElement
      expect(within(totalCard).getByText('20')).toBeInTheDocument()
      const openCard = screen.getByText(/open disputes/i).closest('.card') as HTMLElement
      expect(within(openCard).getByText('8')).toBeInTheDocument()
      const todayCard = screen.getByText(/submitted today/i).closest('.card') as HTMLElement
      expect(within(todayCard).getByText('3')).toBeInTheDocument()
      expect(screen.getByText('4.5d')).toBeInTheDocument()
    })
  })

  it('renders the correct resolution rate', async () => {
    vi.mocked(apiClient.getAdminStats).mockResolvedValue(mockStats)
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      const rateCard = screen.getByText(/resolution rate/i).closest('.card') as HTMLElement
      expect(within(rateCard).getByText('50%')).toBeInTheDocument()
    })
  })

  it('renders 0% resolution rate when total is 0', async () => {
    vi.mocked(apiClient.getAdminStats).mockResolvedValue({
      ...mockStats,
      total: 0,
      byStatus: { Submitted: 0, UnderReview: 0, MoreInfoRequired: 0, Approved: 0, Rejected: 0, Resolved: 0 },
    })
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      const rateCard = screen.getByText(/resolution rate/i).closest('.card') as HTMLElement
      expect(within(rateCard).getByText('0%')).toBeInTheDocument()
    })
  })

  it('shows all six statuses in the breakdown table', async () => {
    vi.mocked(apiClient.getAdminStats).mockResolvedValue(mockStats)
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText('Submitted')).toBeInTheDocument()
      expect(screen.getByText('UnderReview')).toBeInTheDocument()
      expect(screen.getByText('MoreInfoRequired')).toBeInTheDocument()
      expect(screen.getByText('Approved')).toBeInTheDocument()
      expect(screen.getByText('Rejected')).toBeInTheDocument()
      expect(screen.getByText('Resolved')).toBeInTheDocument()
    })
  })

  it('shows a link to the disputes page', async () => {
    vi.mocked(apiClient.getAdminStats).mockResolvedValue(mockStats)
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      const link = screen.getByRole('link', { name: /view all/i })
      expect(link).toHaveAttribute('href', '/admin/disputes')
    })
  })

  it('shows error when API fails', async () => {
    vi.mocked(apiClient.getAdminStats).mockRejectedValue(new Error('Server error'))
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/failed to load stats/i)).toBeInTheDocument()
    })
  })

  it('renders stat card labels', async () => {
    vi.mocked(apiClient.getAdminStats).mockResolvedValue(mockStats)
    render(<MemoryRouter><AdminDashboardPage /></MemoryRouter>)
    await waitFor(() => {
      expect(screen.getByText(/total disputes/i)).toBeInTheDocument()
      expect(screen.getByText(/open disputes/i)).toBeInTheDocument()
      expect(screen.getByText(/submitted today/i)).toBeInTheDocument()
      expect(screen.getByText(/avg resolution/i)).toBeInTheDocument()
      expect(screen.getByText(/resolution rate/i)).toBeInTheDocument()
    })
  })
})
