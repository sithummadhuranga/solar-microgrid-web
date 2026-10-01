// grid operator page to monitor power trading bookings and cancel one for a prosumer
import { useEffect, useState } from 'react'
import Table from 'react-bootstrap/Table'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Alert from 'react-bootstrap/Alert'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'

type Reservation = {
  id: string
  nic: string
  stationId: string
  slotId: string
  scheduledTime: string
  state: string
}

type Station = {
  id: string
  name: string
}

// turns a utc iso string from the api into a readable local date and time
function showTime(iso: string) {
  return new Date(iso).toLocaleString()
}

function BookingMonitor() {
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [stationNames, setStationNames] = useState<Record<string, string>>({})
  const [state, setState] = useState('')
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    // loads the node names and the bookings when the page opens
    async function load() {
      try {
        const stations: Station[] = await callApi('/api/stations')
        setStationNames(Object.fromEntries(stations.map((station) => [station.id, station.name])))
        setReservations(await callApi('/api/reservations'))
      } catch (err) {
        setError((err as Error).message)
      }
    }

    load()
  }, [])

  // asks the api for the bookings that match the state filter and the search box
  async function loadReservations(nextState: string, nextSearch: string) {
    setError('')

    try {
      const query = `state=${encodeURIComponent(nextState)}&search=${encodeURIComponent(nextSearch)}`
      setReservations(await callApi(`/api/reservations?${query}`))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // reloads with the newly picked state
  function handleStateChange(value: string) {
    setState(value)
    loadReservations(value, search)
  }

  // reloads with what is typed in the search box
  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    loadReservations(state, search)
  }

  // cancels a booking for a prosumer after the operator confirms
  async function handleCancel(reservation: Reservation) {
    if (!window.confirm(`Cancel the booking of ${reservation.nic}?`)) return
    setError('')

    try {
      await callApi(`/api/reservations/${reservation.id}/cancel`, 'POST')
      loadReservations(state, search)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <Layout>
      <h1 className="fw-semibold mb-3">Booking monitor</h1>

      {error && <Alert variant="danger">{error}</Alert>}

      <Form onSubmit={handleSearch} className="mb-3">
        <Row className="g-2">
          <Col xs={12} md={4}>
            <Form.Select value={state} onChange={(e) => handleStateChange(e.target.value)}>
              <option value="">All states</option>
              <option value="pending">Pending</option>
              <option value="approved">Approved</option>
              <option value="done">Done</option>
              <option value="cancelled">Cancelled</option>
            </Form.Select>
          </Col>
          <Col xs={12} md={6}>
            <Form.Control
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by NIC or microgrid node"
            />
          </Col>
          <Col xs={12} md={2}>
            <Button type="submit" className="w-100">
              Search
            </Button>
          </Col>
        </Row>
      </Form>

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>NIC</th>
            <th>Microgrid node</th>
            <th>Scheduled</th>
            <th>State</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {reservations.map((reservation) => (
            <tr key={reservation.id}>
              <td>{reservation.nic}</td>
              <td>{stationNames[reservation.stationId] ?? reservation.stationId}</td>
              <td>{showTime(reservation.scheduledTime)}</td>
              <td>{reservation.state}</td>
              <td>
                {(reservation.state === 'pending' || reservation.state === 'approved') && (
                  <Button variant="danger" size="sm" onClick={() => handleCancel(reservation)}>
                    Cancel
                  </Button>
                )}
              </td>
            </tr>
          ))}
          {reservations.length === 0 && (
            <tr>
              <td colSpan={5} className="text-center text-body-secondary">
                No bookings match
              </td>
            </tr>
          )}
        </tbody>
      </Table>
    </Layout>
  )
}

export default BookingMonitor
