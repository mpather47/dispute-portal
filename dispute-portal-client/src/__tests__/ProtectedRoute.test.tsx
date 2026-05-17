import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, beforeEach } from 'vitest'
import ProtectedRoute from '../components/ProtectedRoute'

function renderWithRouter(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>)
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('redirects to /login when no token is present', () => {
    renderWithRouter(
      <ProtectedRoute>
        <div>Protected content</div>
      </ProtectedRoute>
    )
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument()
  })

  it('renders children when a valid token is present', () => {
    localStorage.setItem('token', 'fake-token')
    localStorage.setItem('role', 'Customer')
    renderWithRouter(
      <ProtectedRoute allowedRoles={['Customer']}>
        <div>Protected content</div>
      </ProtectedRoute>
    )
    expect(screen.getByText('Protected content')).toBeInTheDocument()
  })

  it('redirects when the user role is not in allowedRoles', () => {
    localStorage.setItem('token', 'fake-token')
    localStorage.setItem('role', 'Customer')
    renderWithRouter(
      <ProtectedRoute allowedRoles={['Admin']}>
        <div>Admin only content</div>
      </ProtectedRoute>
    )
    expect(screen.queryByText('Admin only content')).not.toBeInTheDocument()
  })

  it('renders children for Admin when Admin role is allowed', () => {
    localStorage.setItem('token', 'fake-token')
    localStorage.setItem('role', 'Admin')
    renderWithRouter(
      <ProtectedRoute allowedRoles={['Customer', 'Admin']}>
        <div>Shared content</div>
      </ProtectedRoute>
    )
    expect(screen.getByText('Shared content')).toBeInTheDocument()
  })
})
