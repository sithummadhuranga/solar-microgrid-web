// sends to login if nobody is logged in, or shows a message if the role does not match
// checks the role with the api, a role only stored in the browser can be edited by the user
import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import Container from 'react-bootstrap/Container'
import { getUser } from '../lib/auth'
import { callApi } from '../lib/api'

type RequireRoleProps = {
  role: string
  children: ReactNode
}

function RequireRole({ role, children }: RequireRoleProps) {
  const [checkedRole, setCheckedRole] = useState<string | null>(null)
  const [checked, setChecked] = useState(false)

  // asks the api for the real role behind the token, ignores a role edited in local storage
  useEffect(() => {
    let cancelled = false

    callApi('/api/auth/me')
      .then((me) => {
        if (!cancelled) setCheckedRole(me.role)
      })
      .catch(() => {
        if (!cancelled) setCheckedRole(null)
      })
      .finally(() => {
        if (!cancelled) setChecked(true)
      })

    return () => {
      cancelled = true
    }
  }, [role])

  if (!getUser()) return <Navigate to="/login" replace />

  if (!checked) return null

  if (checkedRole !== role) {
    return (
      <Container className="mt-5">
        <p>You do not have access to this page.</p>
      </Container>
    )
  }

  return children
}

export default RequireRole
