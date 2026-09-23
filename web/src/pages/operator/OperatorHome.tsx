// grid operator home, placeholder until the team designs it
import { Link } from 'react-router'
import Layout from '../../components/Layout'

function OperatorHome() {
  return (
    <Layout>
      <h1 className="fw-semibold mb-1">Grid operator home</h1>
      <p className="text-body-secondary">Booking and slot tools go here.</p>
      <Link to="/operator/stations" className="btn btn-primary me-2">
        Microgrid nodes
      </Link>
      <Link to="/operator/reservations">Reservations</Link>
    </Layout>
  )
}

export default OperatorHome
