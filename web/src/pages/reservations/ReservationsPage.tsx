// creates, updates and cancels reservations, for backoffice and grid operator
import { useState } from 'react'
import type { FormEvent } from 'react'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
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
  qrData: string | null
}

// turns a utc iso string from the api into a value the datetime-local input can show
function toLocalInput(iso: string) {
  const date = new Date(iso)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

function ReservationsPage() {
  const [nic, setNic] = useState('')
  const [stationId, setStationId] = useState('')
  const [slotId, setSlotId] = useState('')
  const [scheduledTime, setScheduledTime] = useState('')
  const [createError, setCreateError] = useState('')
  const [createMessage, setCreateMessage] = useState('')

  const [lookupId, setLookupId] = useState('')
  const [reservation, setReservation] = useState<Reservation | null>(null)
  const [editStationId, setEditStationId] = useState('')
  const [editSlotId, setEditSlotId] = useState('')
  const [editScheduledTime, setEditScheduledTime] = useState('')
  const [editError, setEditError] = useState('')
  const [editMessage, setEditMessage] = useState('')

  // creates a new reservation for the given nic
  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setCreateError('')
    setCreateMessage('')

    try {
      await callApi('/api/reservations', 'POST', {
        nic,
        stationId,
        slotId,
        scheduledTime: new Date(scheduledTime).toISOString(),
      })
      setCreateMessage('Reservation saved')
      setNic('')
      setStationId('')
      setSlotId('')
      setScheduledTime('')
    } catch (err) {
      setCreateError((err as Error).message)
    }
  }

  // loads a reservation by id into the edit form
  async function handleLookup(e: FormEvent) {
    e.preventDefault()
    setEditError('')
    setEditMessage('')
    setReservation(null)

    try {
      const result: Reservation = await callApi(`/api/reservations/${lookupId}`)
      setReservation(result)
      setEditStationId(result.stationId)
      setEditSlotId(result.slotId)
      setEditScheduledTime(toLocalInput(result.scheduledTime))
    } catch (err) {
      setEditError((err as Error).message)
    }
  }

  // saves changes to the loaded reservation, needs 12 hours notice
  async function handleUpdate(e: FormEvent) {
    e.preventDefault()
    if (!reservation) return
    setEditError('')
    setEditMessage('')

    try {
      const result: Reservation = await callApi(`/api/reservations/${reservation.id}`, 'PUT', {
        stationId: editStationId,
        slotId: editSlotId,
        scheduledTime: new Date(editScheduledTime).toISOString(),
      })
      setReservation(result)
      setEditMessage('Reservation saved')
    } catch (err) {
      setEditError((err as Error).message)
    }
  }

  // cancels the loaded reservation, needs 12 hours notice
  async function handleCancel() {
    if (!reservation) return
    if (!window.confirm('Cancel this reservation?')) return
    setEditError('')
    setEditMessage('')

    try {
      const result: Reservation = await callApi(`/api/reservations/${reservation.id}/cancel`, 'POST')
      setReservation(result)
      setEditMessage('Reservation cancelled')
    } catch (err) {
      setEditError((err as Error).message)
    }
  }

  return (
    <Layout>
      <h1 className="fw-semibold mb-4">Reservations</h1>

      <Row className="g-4">
        <Col md={6}>
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
                  <Form.Control value={nic} onChange={(e) => setNic(e.target.value)} required />
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>Microgrid node</Form.Label>
                  <Form.Control value={stationId} onChange={(e) => setStationId(e.target.value)} required />
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>Slot</Form.Label>
                  <Form.Control value={slotId} onChange={(e) => setSlotId(e.target.value)} required />
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

        <Col md={6}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title as="h2" className="h5">
                Find a reservation
              </Card.Title>

              <Form onSubmit={handleLookup} className="d-flex gap-2 mb-3">
                <Form.Control
                  placeholder="Reservation id"
                  value={lookupId}
                  onChange={(e) => setLookupId(e.target.value)}
                  required
                />
                <Button type="submit" variant="outline-primary">
                  Load
                </Button>
              </Form>

              {editError && (
                <Alert variant="danger" className="py-2">
                  {editError}
                </Alert>
              )}
              {editMessage && (
                <Alert variant="success" className="py-2">
                  {editMessage}
                </Alert>
              )}

              {reservation && (
                <Form onSubmit={handleUpdate}>
                  <p className="text-body-secondary mb-3">
                    NIC {reservation.nic}, state {reservation.state}
                  </p>
                  <Form.Group className="mb-3">
                    <Form.Label>Microgrid node</Form.Label>
                    <Form.Control
                      value={editStationId}
                      onChange={(e) => setEditStationId(e.target.value)}
                      required
                    />
                  </Form.Group>
                  <Form.Group className="mb-3">
                    <Form.Label>Slot</Form.Label>
                    <Form.Control value={editSlotId} onChange={(e) => setEditSlotId(e.target.value)} required />
                  </Form.Group>
                  <Form.Group className="mb-3">
                    <Form.Label>Scheduled time</Form.Label>
                    <Form.Control
                      type="datetime-local"
                      value={editScheduledTime}
                      onChange={(e) => setEditScheduledTime(e.target.value)}
                      required
                    />
                  </Form.Group>
                  <div className="d-flex gap-2">
                    <Button type="submit" variant="primary">
                      Save
                    </Button>
                    <Button type="button" variant="danger" onClick={handleCancel}>
                      Cancel reservation
                    </Button>
                  </div>
                </Form>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Layout>
  )
}

export default ReservationsPage
