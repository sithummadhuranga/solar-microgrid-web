// creates reservations and lists the pending ones for backoffice and grid operator to approve
import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Alert from 'react-bootstrap/Alert'
import Table from 'react-bootstrap/Table'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'

type Station = {
  id: string
  name: string
}

type Slot = {
  id: string
  startTime: string
  endTime: string
  availableSlots: number
  totalSlots: number
}

type PendingReservation = {
  id: string
  nic: string
  stationName: string
  slotStart: string
  slotEnd: string
  scheduledTime: string
}

// cuts a utc iso string from the api down to date and minutes
function shortTime(iso: string) {
  return iso.slice(0, 16).replace('T', ' ')
}

// shows the new reservation form and the list of pending reservations to approve
function ReservationsPage() {
  const [nic, setNic] = useState('')
  const [stations, setStations] = useState<Station[]>([])
  const [slots, setSlots] = useState<Slot[]>([])
  const [stationId, setStationId] = useState('')
  const [slotId, setSlotId] = useState('')
  const [scheduledTime, setScheduledTime] = useState('')
  const [createError, setCreateError] = useState('')
  const [createMessage, setCreateMessage] = useState('')

  const [pending, setPending] = useState<PendingReservation[]>([])
  const [listError, setListError] = useState('')
  const [listMessage, setListMessage] = useState('')

  // loads the pending reservations, called on open and after every approve or cancel
  const loadPending = useCallback(() => {
    callApi('/api/reservations/pending')
      .then(setPending)
      .catch((err) => setListError((err as Error).message))
  }, [])

  // loads the nodes for the dropdown and the pending list when the page opens
  useEffect(() => {
    callApi('/api/stations')
      .then(setStations)
      .catch((err) => setCreateError((err as Error).message))
    loadPending()
  }, [loadPending])

  // loads the slots of the picked node into the slot dropdown
  function handleStationChange(id: string) {
    setStationId(id)
    setSlotId('')
    setSlots([])
    if (!id) return

    callApi(`/api/stations/${id}/slots`)
      .then(setSlots)
      .catch((err) => setCreateError((err as Error).message))
  }

  // creates a new reservation for the given nic
  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setCreateError('')
    setCreateMessage('')

    const cleanNic = nic.trim()
    if (!cleanNic || !stationId || !slotId || !scheduledTime) {
      setCreateError('Fill in all fields')
      return
    }

    try {
      await callApi('/api/reservations', 'POST', {
        nic: cleanNic,
        stationId,
        slotId,
        scheduledTime: new Date(scheduledTime).toISOString(),
      })
      setCreateMessage('Reservation saved, it now waits for approval')
      setNic('')
      setStationId('')
      setSlotId('')
      setSlots([])
      setScheduledTime('')
      loadPending()
    } catch (err) {
      setCreateError((err as Error).message)
    }
  }

  // approves one pending reservation and generates its qr code
  async function handleApprove(id: string) {
    setListError('')
    setListMessage('')

    try {
      await callApi(`/api/reservations/${id}/approve`, 'POST')
      setListMessage('Reservation approved')
      loadPending()
    } catch (err) {
      setListError((err as Error).message)
    }
  }

  // cancels one pending reservation, needs 12 hours notice
  async function handleCancel(id: string) {
    if (!window.confirm('Cancel this reservation?')) return
    setListError('')
    setListMessage('')

    try {
      await callApi(`/api/reservations/${id}/cancel`, 'POST')
      setListMessage('Reservation cancelled')
      loadPending()
    } catch (err) {
      setListError((err as Error).message)
    }
  }

  return (
    <Layout>
      <h1 className="fw-semibold mb-4">Reservations</h1>

      <Row className="g-4">
        <Col lg={5}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title as="h2" className="h5">
                New reservation
              </Card.Title>

              {createError && (
                <Alert variant="danger" className="py-2">
                  {createError}
                </Alert>
              )}
              {createMessage && (
                <Alert variant="success" className="py-2">
                  {createMessage}
                </Alert>
              )}

              <Form onSubmit={handleCreate}>
                <Form.Group className="mb-3">
                  <Form.Label>NIC</Form.Label>
                  <Form.Control value={nic} maxLength={20} onChange={(e) => setNic(e.target.value)} required />
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>Microgrid node</Form.Label>
                  <Form.Select value={stationId} onChange={(e) => handleStationChange(e.target.value)} required>
                    <option value="">Pick a node</option>
                    {stations.map((station) => (
                      <option key={station.id} value={station.id}>
                        {station.name}
                      </option>
                    ))}
                  </Form.Select>
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>Slot</Form.Label>
                  <Form.Select value={slotId} onChange={(e) => setSlotId(e.target.value)} required>
                    <option value="">Pick a slot</option>
                    {slots.map((slot) => (
                      <option key={slot.id} value={slot.id}>
                        {shortTime(slot.startTime)} to {shortTime(slot.endTime)} UTC, free {slot.availableSlots} of{' '}
                        {slot.totalSlots}
                      </option>
                    ))}
                  </Form.Select>
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>Scheduled time</Form.Label>
                  <Form.Control
                    type="datetime-local"
                    value={scheduledTime}
                    onChange={(e) => setScheduledTime(e.target.value)}
                    required
                  />
                </Form.Group>
                <Button type="submit" variant="primary">
                  Save
                </Button>
              </Form>
            </Card.Body>
          </Card>
        </Col>

        <Col lg={7}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title as="h2" className="h5">
                Waiting for approval
              </Card.Title>

              {listError && (
                <Alert variant="danger" className="py-2">
                  {listError}
                </Alert>
              )}
              {listMessage && (
                <Alert variant="success" className="py-2">
                  {listMessage}
                </Alert>
              )}

              {pending.length === 0 ? (
                <p className="text-body-secondary mb-0">No reservations are waiting for approval</p>
              ) : (
                <Table responsive hover className="align-middle mb-0">
                  <thead>
                    <tr>
                      <th>NIC</th>
                      <th>Microgrid node</th>
                      <th>Slot (UTC)</th>
                      <th>Scheduled (UTC)</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {pending.map((item) => (
                      <tr key={item.id}>
                        <td>{item.nic}</td>
                        <td>{item.stationName}</td>
                        <td>
                          {shortTime(item.slotStart)} to {shortTime(item.slotEnd)}
                        </td>
                        <td>{shortTime(item.scheduledTime)}</td>
                        <td className="text-end text-nowrap">
                          <Button size="sm" variant="success" className="me-2" onClick={() => handleApprove(item.id)}>
                            Approve
                          </Button>
                          <Button size="sm" variant="outline-danger" onClick={() => handleCancel(item.id)}>
                            Cancel
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Layout>
  )
}

export default ReservationsPage
