// shows the navbar with the logged in user, then the page content below it
import type { ReactNode } from 'react'
import Navbar from 'react-bootstrap/Navbar'
import Container from 'react-bootstrap/Container'
import Button from 'react-bootstrap/Button'
import { useNavigate } from 'react-router'
import { getUser, logout } from '../lib/auth'

type LayoutProps = {
  children: ReactNode
}

function Layout({ children }: LayoutProps) {
  const user = getUser()
  const navigate = useNavigate()

  // clears the login and sends the user back to the login page
  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <>
      <Navbar bg="primary" variant="dark" className="mb-4 shadow-sm">
        <Container className="justify-content-between">
          <Navbar.Brand className="fw-semibold">Solar Microgrid</Navbar.Brand>
          {user && (
            <div className="d-flex align-items-center gap-3">
              <span className="text-white">{user.fullName}</span>
              <Button variant="outline-light" size="sm" onClick={handleLogout}>
                Log out
              </Button>
            </div>
          )}
        </Container>
      </Navbar>
      <Container className="pb-5">{children}</Container>
    </>
  )
}

export default Layout
