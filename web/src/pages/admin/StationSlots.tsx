// backoffice page to list, add, edit and delete the booking slots of one microgrid node
// grid operators get the same list but can only change how many battery slots are free
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import Table from 'react-bootstrap/Table'
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Alert from 'react-bootstrap/Alert'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'
import { getUser } from '../../lib/auth'

type Slot = {
  id: string
  stationId: string
  startTime: string
  endTime: string
  totalSlots: number
  availableSlots: number
}

type Station = {
  id: string
  name: string
  batterySlotCount: number
  status: string
}

const emptyForm = { startTime: '', endTime: '', totalSlots: '' }

// turns a utc date from the api into the local value a datetime-local box expects
function toInputValue(iso: string) {
  const date = new Date(iso)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

// shows the slots of one node, with the slot form and the availability form
function StationSlots() {
  const { id } = useParams()
  const isBackoffice = getUser()?.role === 'Backoffice'
  const basePath = isBackoffice ? '/admin' : '/operator'
  const [station, setStation] = useState<Station | null>(null)
  const [slots, setSlots] = useState<Slot[]>([])
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState('')
  const [availabilitySlot, setAvailabilitySlot] = useState<Slot | null>(null)
  const [available, setAvailable] = useState('')

  // loads the node and its slots when the page opens or the node id changes
  useEffect(() => {
    loadStation()
    loadSlots()
  }, [id])

  // loads the node so the page can show its name
  async function loadStation() {
    try {
      setStation(await callApi(`/api/stations/${id}`))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // loads the slots of the node
  async function loadSlots() {
    try {
      setSlots(await callApi(`/api/stations/${id}/slots`))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // opens an empty form for a new slot
  function openAdd() {
    setEditingId(null)
    setForm(emptyForm)
    setFormError('')
    setShowForm(true)
  }

  // opens the form filled with an existing slot
  function openEdit(slot: Slot) {
    setEditingId(slot.id)
    setForm({
      startTime: toInputValue(slot.startTime),
      endTime: toInputValue(slot.endTime),
      totalSlots: String(slot.totalSlots),
    })
    setFormError('')
    setShowForm(true)
  }

  // sends the new or changed slot to the api, times go as utc
  async function handleSave(e: FormEvent) {
    e.preventDefault()
    setFormError('')

    const body = {
      startTime: new Date(form.startTime).toISOString(),
      endTime: new Date(form.endTime).toISOString(),
      totalSlots: Number(form.totalSlots),
    }

    try {
      if (editingId) await callApi(`/api/stations/${id}/slots/${editingId}`, 'PUT', body)
      else await callApi(`/api/stations/${id}/slots`, 'POST', body)
      setShowForm(false)
      loadSlots()
    } catch (err) {
      setFormError((err as Error).message)
    }
  }

  // opens the box to change how many battery slots are free
  function openAvailability(slot: Slot) {
    setAvailabilitySlot(slot)
    setAvailable(String(slot.availableSlots))
    setFormError('')
  }

  // sends the new free count to the api
  async function handleSaveAvailability(e: FormEvent) {
    e.preventDefault()
    setFormError('')

    try {
      await callApi(`/api/stations/${id}/slots/${availabilitySlot!.id}/availability`, 'PUT', {
        availableSlots: Number(available),
      })
      setAvailabilitySlot(null)
      loadSlots()
    } catch (err) {
      setFormError((err as Error).message)
    }
  }

  // deletes a slot after the user confirms
  async function handleDelete(slot: Slot) {
    if (!window.confirm('Delete this slot?')) return
    setError('')

    try {
      await callApi(`/api/stations/${id}/slots/${slot.id}`, 'DELETE')
      loadSlots()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <Layout>
      <Link to={`${basePath}/stations`}>Back to microgrid nodes</Link>
      <div className="d-flex justify-content-between align-items-center mt-2 mb-3">
        <h1 className="fw-semibold mb-0">Slots for {station?.name}</h1>
        {isBackoffice && (
          <Button onClick={openAdd} disabled={station?.status !== 'active'}>
            Add slot
          </Button>
        )}
      </div>

      {station && (
        <p className="text-body-secondary">
          This node has {station.batterySlotCount} battery storage slots.
          {isBackoffice && station.status !== 'active' && ' It is deactivated, so no new slots can be added.'}
        </p>
      )}

      {error && <Alert variant="danger">{error}</Alert>}

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>Start</th>
            <th>End</th>
            <th>Battery storage slots</th>
            <th>Free</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {slots.map((slot) => (
            <tr key={slot.id}>
              <td>{new Date(slot.startTime).toLocaleString()}</td>
              <td>{new Date(slot.endTime).toLocaleString()}</td>
              <td>{slot.totalSlots}</td>
              <td>{slot.availableSlots}</td>
              <td className="text-nowrap">
                <Button size="sm" variant="outline-primary" className="me-2" onClick={() => openAvailability(slot)}>
                  Availability
                </Button>
                {isBackoffice && (
                  <>
                    <Button size="sm" variant="outline-secondary" className="me-2" onClick={() => openEdit(slot)}>
                      Edit
                    </Button>
                    <Button size="sm" variant="danger" onClick={() => handleDelete(slot)}>
                      Delete
                    </Button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {slots.length === 0 && (
            <tr>
              <td colSpan={5} className="text-center text-body-secondary">
                No slots yet
              </td>
            </tr>
          )}
        </tbody>
      </Table>

      <Modal show={showForm} onHide={() => setShowForm(false)}>
        <Form onSubmit={handleSave}>
          <Modal.Header closeButton>
            <Modal.Title>{editingId ? 'Edit slot' : 'Add slot'}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            {formError && <Alert variant="danger">{formError}</Alert>}

            <Row>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Start</Form.Label>
                <Form.Control
                  type="datetime-local"
                  value={form.startTime}
                  onChange={(e) => setForm({ ...form, startTime: e.target.value })}
                  required
                />
              </Form.Group>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>End</Form.Label>
                <Form.Control
                  type="datetime-local"
                  value={form.endTime}
                  onChange={(e) => setForm({ ...form, endTime: e.target.value })}
                  required
                />
              </Form.Group>
            </Row>

            <Form.Group className="mb-3">
              <Form.Label>Battery storage slots</Form.Label>
              <Form.Control
                type="number"
                value={form.totalSlots}
                onChange={(e) => setForm({ ...form, totalSlots: e.target.value })}
                required
              />
            </Form.Group>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={() => setShowForm(false)}>
              Close
            </Button>
            <Button type="submit">Save</Button>
          </Modal.Footer>
        </Form>
      </Modal>

      <Modal show={availabilitySlot !== null} onHide={() => setAvailabilitySlot(null)}>
        <Form onSubmit={handleSaveAvailability}>
          <Modal.Header closeButton>
            <Modal.Title>Slot availability</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            {formError && <Alert variant="danger">{formError}</Alert>}

            <Form.Group>
              <Form.Label>Free battery storage slots (out of {availabilitySlot?.totalSlots})</Form.Label>
              <Form.Control type="number" value={available} onChange={(e) => setAvailable(e.target.value)} required />
            </Form.Group>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={() => setAvailabilitySlot(null)}>
              Close
            </Button>
            <Button type="submit">Save</Button>
          </Modal.Footer>
        </Form>
      </Modal>
    </Layout>
  )
}

export default StationSlots
