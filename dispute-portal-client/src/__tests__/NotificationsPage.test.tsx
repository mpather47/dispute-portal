import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import NotificationsPage from '../pages/NotificationsPage'
import * as apiClient from '../api/apiClient'
import { NotificationContext } from '../context/NotificationContext'
import type { NotificationLog } from '../types/types'

vi.mock('../api/apiClient')

const mockMarkAllRead = vi.fn()

function renderPage() {
  return render(
    <MemoryRouter>
      <NotificationContext.Provider value={{ unreadCount: 0, markAllRead: mockMarkAllRead }}>
        <NotificationsPage />
      </NotificationContext.Provider>
    </MemoryRouter>
  )
}

const mockNotifications: NotificationLog[] = [
  { id: 1, subject: 'Dispute Status Updated', message: 'Your dispute DSP-001 is now UnderReview.', sentAt: '2024-01-15T10:00:00Z' },
  { id: 2, subject: 'New Dispute Submitted', message: 'Dispute DSP-002 submitted by Alice Chen.', sentAt: '2024-01-16T09:00:00Z' },
]

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.getNotifications).mockReturnValue(new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('renders notification subjects and messages after loading', async () => {
    vi.mocked(apiClient.getNotifications).mockResolvedValue(mockNotifications)
    renderPage()
    await waitFor(() => {
      expect(screen.getByText('Dispute Status Updated')).toBeInTheDocument()
      expect(screen.getByText('Your dispute DSP-001 is now UnderReview.')).toBeInTheDocument()
      expect(screen.getByText('New Dispute Submitted')).toBeInTheDocument()
      expect(screen.getByText('Dispute DSP-002 submitted by Alice Chen.')).toBeInTheDocument()
    })
  })

  it('shows error when API fails', async () => {
    vi.mocked(apiClient.getNotifications).mockRejectedValue(new Error('Server error'))
    renderPage()
    await waitFor(() => {
      expect(screen.getByText(/failed to load notifications/i)).toBeInTheDocument()
    })
  })

  it('shows empty state message when no notifications', async () => {
    vi.mocked(apiClient.getNotifications).mockResolvedValue([])
    renderPage()
    await waitFor(() => {
      expect(screen.getByText(/no notifications yet/i)).toBeInTheDocument()
    })
  })

  it('calls markAllRead after loading', async () => {
    vi.mocked(apiClient.getNotifications).mockResolvedValue(mockNotifications)
    renderPage()
    await waitFor(() => screen.getByText('Dispute Status Updated'))
    expect(mockMarkAllRead).toHaveBeenCalled()
  })

  it('renders items in the order returned by the API', async () => {
    vi.mocked(apiClient.getNotifications).mockResolvedValue(mockNotifications)
    renderPage()
    await waitFor(() => screen.getByText('Dispute Status Updated'))
    const items = screen.getAllByRole('listitem')
    expect(items[0]).toHaveTextContent('Dispute Status Updated')
    expect(items[1]).toHaveTextContent('New Dispute Submitted')
  })

  it('renders two notification items', async () => {
    vi.mocked(apiClient.getNotifications).mockResolvedValue(mockNotifications)
    renderPage()
    await waitFor(() => {
      expect(screen.getAllByRole('listitem')).toHaveLength(2)
    })
  })
})
