// grid operator home, shows the booking counts an operator is allowed to see
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

function OperatorHome() {
  const [stations, setStations] = useState<Station[]>([])
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    // reads the two lists an operator's role lets it call
    async function load() {
      try {
        setStations(await callApi('/api/stations'))
        setReservations(await callApi('/api/reservations'))
      } catch (err) {
        setError((err as Error).message)
      }
    }

    load()
  }, [])

  const stationNames: Record<string, string> = Object.fromEntries(
    stations.map((station) => [station.id, station.name]),
  )

  const pendingBookings = reservations.filter((reservation) => reservation.state === 'pending').length
  const approvedBookings = reservations.filter((reservation) => reservation.state === 'approved').length
  const doneBookings = reservations.filter((reservation) => reservation.state === 'done').length
  const activeStations = stations.filter((station) => station.status === 'active').length

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
      <h1 className="fw-semibold mb-3">Grid operator home</h1>

      {error && <Alert variant="danger">{error}</Alert>}

      <Row xs={1} sm={2} lg={3} className="g-3 mb-4">
        <StatCard
          label="Waiting to be approved"
          value={pendingBookings}
          to="/operator/bookings"
          lines={['Bookings still pending']}
        />
        <StatCard
          label="Approved bookings"
          value={approvedBookings}
          to="/operator/bookings"
          lines={[`${doneBookings} already finished`]}
        />
        <StatCard
          label="Microgrid nodes"
          value={stations.length}
          to="/operator/stations"
          lines={[`${activeStations} active`]}
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

export default OperatorHome
