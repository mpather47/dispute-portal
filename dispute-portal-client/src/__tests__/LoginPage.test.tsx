import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import LoginPage from '../pages/LoginPage'
import * as apiClient from '../api/apiClient'

const mockNavigate = vi.hoisted(() => vi.fn())

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

vi.mock('../api/apiClient')

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('renders email, password fields and sign in button', () => {
    render(<MemoryRouter><LoginPage /></MemoryRouter>)
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('does not pre-fill credentials', () => {
    render(<MemoryRouter><LoginPage /></MemoryRouter>)
    expect(screen.getByLabelText<HTMLInputElement>(/email/i).value).toBe('')
    expect(screen.getByLabelText<HTMLInputElement>(/password/i).value).toBe('')
  })

  it('shows error message when login fails', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.login).mockRejectedValue(new Error('Unauthorized'))
    render(<MemoryRouter><LoginPage /></MemoryRouter>)
    await user.type(screen.getByLabelText(/email/i), 'wrong@example.com')
    await user.type(screen.getByLabelText(/password/i), 'badpassword')
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    await waitFor(() => {
      expect(screen.getByText(/invalid email or password/i)).toBeInTheDocument()
    })
  })

  it('stores token in localStorage and navigates to /transactions for Customer', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.login).mockResolvedValue({
      token: 'customer-token',
      userId: 1,
      fullName: 'Test Customer',
      email: 'customer@test.com',
      role: 'Customer',
    })
    render(<MemoryRouter><LoginPage /></MemoryRouter>)
    await user.type(screen.getByLabelText(/email/i), 'customer@test.com')
    await user.type(screen.getByLabelText(/password/i), 'Password123!')
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    await waitFor(() => {
      expect(localStorage.getItem('token')).toBe('customer-token')
      expect(localStorage.getItem('role')).toBe('Customer')
      expect(mockNavigate).toHaveBeenCalledWith('/transactions')
    })
  })

  it('navigates to /admin/dashboard for Admin role', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.login).mockResolvedValue({
      token: 'admin-token',
      userId: 2,
      fullName: 'Test Admin',
      email: 'admin@test.com',
      role: 'Admin',
    })
    render(<MemoryRouter><LoginPage /></MemoryRouter>)
    await user.type(screen.getByLabelText(/email/i), 'admin@test.com')
    await user.type(screen.getByLabelText(/password/i), 'Password123!')
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/admin/dashboard')
    })
  })
})
