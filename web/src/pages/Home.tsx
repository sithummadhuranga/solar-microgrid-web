// placeholder home page, the real design is member 4's page
import { Link } from 'react-router'
import Container from 'react-bootstrap/Container'

function Home() {
  return (
    <Container className="d-flex flex-column align-items-center justify-content-center text-center min-vh-100 gap-3">
      <h1 className="fw-semibold">Solar Microgrid</h1>
      <p className="text-body-secondary">Backoffice and grid operator sign in</p>
      <Link to="/login" className="btn btn-primary btn-lg px-4">
        Log in
      </Link>
    </Container>
  )
}

export default Home
