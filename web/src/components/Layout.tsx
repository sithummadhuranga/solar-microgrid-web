import type { ReactNode } from 'react'
import Navbar from 'react-bootstrap/Navbar'
import Container from 'react-bootstrap/Container'
import Button from 'react-bootstrap/Button'
import { useNavigate } from 'react-router'
import { getUser, logout } from '../auth'

type LayoutProps = {
  children: ReactNode
}

// shows the navbar with the logged in user, then the page content below it
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
      <Navbar bg="light" className="border-bottom mb-4">
        <Container className="justify-content-between">
          <Navbar.Brand>Solar Microgrid</Navbar.Brand>
          {user && (
            <div className="d-flex align-items-center gap-3">
              <span>{user.fullName}</span>
              <Button variant="outline-secondary" size="sm" onClick={handleLogout}>
                Log out
              </Button>
            </div>
          )}
        </Container>
      </Navbar>
      <Container>{children}</Container>
    </>
  )
}

export default Layout
