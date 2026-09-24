// shows the navbar with role links and the logged in user, then the page content below it
import type { ReactNode } from 'react'
import Navbar from 'react-bootstrap/Navbar'
import Nav from 'react-bootstrap/Nav'
import Container from 'react-bootstrap/Container'
import Button from 'react-bootstrap/Button'
import Badge from 'react-bootstrap/Badge'
import { Link, useLocation, useNavigate } from 'react-router'
import { getUser, logout } from '../lib/auth'

type LayoutProps = {
  children: ReactNode
}

const adminLinks = [
  { to: '/admin', label: 'Home' },
  { to: '/admin/stations', label: 'Microgrid nodes' },
  { to: '/admin/reservations', label: 'Reservations' },
  { to: '/admin/users', label: 'Web users' },
  { to: '/admin/prosumers', label: 'Prosumers' },
  { to: '/admin/pending-activations', label: 'Pending activations' },
]

const operatorLinks = [
  { to: '/operator', label: 'Home' },
  { to: '/operator/stations', label: 'Microgrid nodes' },
  { to: '/operator/reservations', label: 'Reservations' },
  { to: '/operator/bookings', label: 'Booking monitor' },
  { to: '/operator/slots', label: 'Slot availability' },
]

function Layout({ children }: LayoutProps) {
  const user = getUser()
  const location = useLocation()
  const navigate = useNavigate()
  const links = user?.role === 'Backoffice' ? adminLinks : user?.role === 'GridOperator' ? operatorLinks : []

  // clears the login and sends the user back to the login page
  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <>
      <Navbar bg="primary" variant="dark" expand="md" className="mb-4 shadow-sm py-2" sticky="top">
        <Container>
          <Navbar.Brand as={Link} to="/" className="fw-semibold fs-5">
            Solar Microgrid
          </Navbar.Brand>
          <Navbar.Toggle aria-controls="main-nav" />
          <Navbar.Collapse id="main-nav">
            <Nav className="me-auto gap-1 my-2 my-md-0">
              {links.map((link) => {
                const isActive = location.pathname === link.to
                return (
                  <Nav.Link
                    key={link.to}
                    as={Link}
                    to={link.to}
                    active={isActive}
                    className={`px-3 rounded-pill ${isActive ? 'bg-white bg-opacity-25 fw-semibold' : ''}`}
                  >
                    {link.label}
                  </Nav.Link>
                )
              })}
            </Nav>
            {user && (
              <div className="d-flex align-items-center gap-3 ms-md-3">
                <span className="text-white d-flex align-items-center gap-2">
                  {user.fullName}
                  <Badge bg="light" text="dark" pill>
                    {user.role === 'GridOperator' ? 'Grid Operator' : user.role}
                  </Badge>
                </span>
                <Button variant="outline-light" size="sm" onClick={handleLogout}>
                  Log out
                </Button>
              </div>
            )}
          </Navbar.Collapse>
        </Container>
      </Navbar>
      <Container className="pb-5">{children}</Container>
    </>
  )
}

export default Layout
