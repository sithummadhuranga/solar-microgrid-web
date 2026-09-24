// backoffice home, shows the live counts and the bookings that are coming up
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Card from 'react-bootstrap/Card'
import Table from 'react-bootstrap/Table'
import Badge from 'react-bootstrap/Badge'
import Alert from 'react-bootstrap/Alert'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'

type Prosumer = {
  id: string
  status: string
  deactivationRequested: boolean
}

type Station = {
  id: string
  name: string
  status: string
}

type Reservation = {
  id: string
  nic: string
  stationId: string
  scheduledTime: string
  state: string
}

type WebUser = {
  id: string
  role: string
}

type StatCardProps = {
  label: string
  value: number
  to: string
  lines: string[]
}

// picks the bootstrap colour that fits a reservation state
function stateVariant(state: string) {
  if (state === 'pending') return 'warning'
  if (state === 'approved') return 'success'
  if (state === 'done') return 'secondary'
  return 'danger'
}

// turns a utc iso string from the api into a readable local date and time
function showTime(iso: string) {
  return new Date(iso).toLocaleString()
}

// one clickable counter box on the dashboard
function StatCard({ label, value, to, lines }: StatCardProps) {
  return (
    <Col>
      <Link to={to} className="text-decoration-none d-block h-100">
        <Card className="h-100">
          <Card.Body>
            <div className="text-body-secondary small">{label}</div>
            <div className="fs-2 fw-semibold text-body">{value}</div>
            {lines.map((line) => (
              <div key={line} className="small text-body-secondary">
                {line}
              </div>
            ))}
          </Card.Body>
        </Card>
      </Link>
    </Col>
  )
}

function AdminHome() {
  const [prosumers, setProsumers] = useState<Prosumer[]>([])
  const [stations, setStations] = useState<Station[]>([])
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [users, setUsers] = useState<WebUser[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    // reads the four lists the counts are worked out from
    async function load() {
      try {
        setProsumers(await callApi('/api/prosumers'))
        setStations(await callApi('/api/stations'))
        setReservations(await callApi('/api/reservations'))
        setUsers(await callApi('/api/users'))
      } catch (err) {
        setError((err as Error).message)
      }
    }

    load()
  }, [])

  const stationNames: Record<string, string> = Object.fromEntries(
    stations.map((station) => [station.id, station.name]),
  )

  const pendingProsumers = prosumers.filter((prosumer) => prosumer.status === 'pending').length
  const deactivationRequests = prosumers.filter((prosumer) => prosumer.deactivationRequested).length
  const activeStations = stations.filter((station) => station.status === 'active').length
  const pendingBookings = reservations.filter((reservation) => reservation.state === 'pending').length
  const approvedBookings = reservations.filter((reservation) => reservation.state === 'approved').length
  const operators = users.filter((user) => user.role === 'GridOperator').length

  const upcoming = reservations
    .filter(
      (reservation) =>
        (reservation.state === 'pending' || reservation.state === 'approved') &&
        new Date(reservation.scheduledTime) > new Date(),
    )
    .sort((a, b) => a.scheduledTime.localeCompare(b.scheduledTime))
    .slice(0, 8)

  return (
    <Layout>
      <h1 className="fw-semibold mb-3">Backoffice home</h1>

      {error && <Alert variant="danger">{error}</Alert>}

      <Row xs={1} sm={2} lg={4} className="g-3 mb-4">
        <StatCard
          label="Prosumers"
          value={prosumers.length}
          to="/admin/prosumers"
          lines={[`${pendingProsumers} pending activation`, `${deactivationRequests} asked to deactivate`]}
        />
        <StatCard
          label="Microgrid nodes"
          value={stations.length}
          to="/admin/stations"
          lines={[`${activeStations} active`]}
        />
        <StatCard
          label="Reservations"
          value={reservations.length}
          to="/admin/reservations"
          lines={[`${pendingBookings} pending`, `${approvedBookings} approved`]}
        />
        <StatCard
          label="Web users"
          value={users.length}
          to="/admin/users"
          lines={[`${users.length - operators} backoffice`, `${operators} grid operator`]}
        />
      </Row>

      <h2 className="fs-4 fw-semibold mb-3">Upcoming bookings</h2>

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>NIC</th>
            <th>Microgrid node</th>
            <th>Scheduled</th>
            <th>State</th>
          </tr>
        </thead>
        <tbody>
          {upcoming.map((reservation) => (
            <tr key={reservation.id}>
              <td>{reservation.nic}</td>
              <td>{stationNames[reservation.stationId] ?? reservation.stationId}</td>
              <td>{showTime(reservation.scheduledTime)}</td>
              <td>
                <Badge bg={stateVariant(reservation.state)}>{reservation.state}</Badge>
              </td>
            </tr>
          ))}
          {upcoming.length === 0 && (
            <tr>
              <td colSpan={4} className="text-center text-body-secondary">
                Nothing booked for the days ahead
              </td>
            </tr>
          )}
        </tbody>
      </Table>
    </Layout>
  )
}

export default AdminHome
