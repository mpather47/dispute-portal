import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import StatusBadge from '../components/StatusBadge'

describe('StatusBadge', () => {
  const statuses = ['Submitted', 'UnderReview', 'MoreInfoRequired', 'Approved', 'Rejected', 'Resolved']

  it.each(statuses)('renders "%s" text', (status) => {
    render(<StatusBadge status={status} />)
    expect(screen.getByText(status)).toBeInTheDocument()
  })

  it.each(statuses)('applies status-specific CSS class for "%s"', (status) => {
    render(<StatusBadge status={status} />)
    const badge = screen.getByText(status)
    expect(badge.className).toContain(`status-${status}`)
    expect(badge.className).toContain('status-badge')
  })

  it('renders as a span element', () => {
    render(<StatusBadge status="Submitted" />)
    expect(screen.getByText('Submitted').tagName).toBe('SPAN')
  })
})
