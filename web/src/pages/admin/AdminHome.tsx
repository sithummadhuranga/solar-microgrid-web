// backoffice home, placeholder until the team designs it
import { Link } from 'react-router'
import Layout from '../../components/Layout'

function AdminHome() {
  return (
    <Layout>
      <h1 className="fw-semibold mb-1">Backoffice home</h1>
      <p className="text-body-secondary">Pages for user and node management go here.</p>
      <Link to="/admin/stations" className="btn btn-primary">
        Microgrid nodes
      </Link>
      <Link to="/admin/reservations">Reservations</Link>
      <Link to="/admin/users">Web users</Link>
      <Link to="/admin/prosumers">Prosumers</Link>
      <Link to="/admin/pending-activations">Pending activations</Link>
    </Layout>
  )
}

export default AdminHome
