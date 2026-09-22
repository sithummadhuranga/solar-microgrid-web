// sends to login if nobody is logged in, or shows a message if the role does not match
import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import Container from 'react-bootstrap/Container'
import { getUser } from '../lib/auth'

type RequireRoleProps = {
  role: string
  children: ReactNode
}

function RequireRole({ role, children }: RequireRoleProps) {
  const user = getUser()

  if (!user) return <Navigate to="/login" replace />

  if (user.role !== role) {
    return (
      <Container className="mt-5">
        <p>You do not have access to this page.</p>
      </Container>
    )
  }

  return children
}

export default RequireRole
