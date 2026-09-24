// backoffice page to list, add, edit, deactivate, activate and delete microgrid nodes
// grid operators get the same list read only, with a link to each node's slots
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router'
import Table from 'react-bootstrap/Table'
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'
import { getUser } from '../../lib/auth'

type Station = {
  id: string
  name: string
  address: string
  latitude: number
  longitude: number
  capacityKwh: number
  batterySlotCount: number
  openingTime: string
  closingTime: string
  status: string
}

const emptyForm = {
  name: '',
  address: '',
  latitude: '',
  longitude: '',
  capacityKwh: '',
  batterySlotCount: '',
  openingTime: '06:00',
  closingTime: '18:00',
}

// shows the node list, and the add and edit form for backoffice users
function Stations() {
  const isBackoffice = getUser()?.role === 'Backoffice'
  const basePath = isBackoffice ? '/admin' : '/operator'
  const [stations, setStations] = useState<Station[]>([])
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState('')
  const [confirming, setConfirming] = useState<{ station: Station; action: 'deactivate' | 'delete' } | null>(null)
  const [confirmError, setConfirmError] = useState('')

  // loads the nodes once when the page opens
  useEffect(() => {
    loadStations()
  }, [])

  // loads every station from the api
  async function loadStations() {
    try {
      setStations(await callApi('/api/stations'))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // opens an empty form for a new station
  function openAdd() {
    setEditingId(null)
    setForm(emptyForm)
    setFormError('')
    setShowForm(true)
  }

  // opens the form filled with an existing station
  function openEdit(station: Station) {
    setEditingId(station.id)
    setForm({
      name: station.name,
      address: station.address,
      latitude: String(station.latitude),
      longitude: String(station.longitude),
      capacityKwh: String(station.capacityKwh),
      batterySlotCount: String(station.batterySlotCount),
      openingTime: station.openingTime,
      closingTime: station.closingTime,
    })
    setFormError('')
    setShowForm(true)
  }

  // updates one field of the form
  function setField(field: keyof typeof emptyForm, value: string) {
    setForm({ ...form, [field]: value })
  }

  // sends the new or changed station to the api
  async function handleSave(e: FormEvent) {
    e.preventDefault()
    setFormError('')

    const body = {
      ...form,
      latitude: Number(form.latitude),
      longitude: Number(form.longitude),
      capacityKwh: Number(form.capacityKwh),
      batterySlotCount: Number(form.batterySlotCount),
    }

    try {
      if (editingId) await callApi(`/api/stations/${editingId}`, 'PUT', body)
      else await callApi('/api/stations', 'POST', body)
      setShowForm(false)
      loadStations()
    } catch (err) {
      setFormError((err as Error).message)
    }
  }

  // opens the box that asks the user to confirm the deactivation
  function handleDeactivate(station: Station) {
    setConfirmError('')
    setConfirming({ station, action: 'deactivate' })
  }

  // opens the box that asks the user to confirm the delete
  function handleDelete(station: Station) {
    setConfirmError('')
    setConfirming({ station, action: 'delete' })
  }

  // deactivates or deletes the station once the user clicks yes
  async function confirmAction() {
    const { station, action } = confirming!
    setConfirmError('')

    try {
      if (action === 'deactivate') await callApi(`/api/stations/${station.id}/deactivate`, 'POST')
      else await callApi(`/api/stations/${station.id}`, 'DELETE')
      setConfirming(null)
      loadStations()
    } catch (err) {
      setConfirmError((err as Error).message)
    }
  }

  // makes a deactivated station active again
  async function handleActivate(station: Station) {
    setError('')

    try {
      await callApi(`/api/stations/${station.id}/activate`, 'POST')
      loadStations()
    } catch (err) {
      setError((err as Error).message)
    }
  }


  return (
    <Layout>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="fw-semibold mb-0">Microgrid nodes</h1>
        {isBackoffice && <Button onClick={openAdd}>Add node</Button>}
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>Name</th>
            <th>Address</th>
            <th>GPS location</th>
            <th>Capacity (kW/h)</th>
            <th>Battery storage slots</th>
            <th>Schedule</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {stations.map((station) => (
            <tr key={station.id}>
              <td>{station.name}</td>
              <td>{station.address}</td>
              <td>
                {station.latitude}, {station.longitude}
              </td>
              <td>{station.capacityKwh}</td>
              <td>{station.batterySlotCount}</td>
              <td>
                {station.openingTime} to {station.closingTime}
              </td>
              <td>
                <Badge bg={station.status === 'active' ? 'success' : 'secondary'}>{station.status}</Badge>
              </td>
              <td className="text-nowrap">
                <Link to={`${basePath}/stations/${station.id}/slots`} className="btn btn-sm btn-outline-primary me-2">
                  Slots
                </Link>
                {isBackoffice && (
                  <>
                    <Button size="sm" variant="outline-secondary" className="me-2" onClick={() => openEdit(station)}>
                      Edit
                    </Button>
                    {station.status === 'active' ? (
                      <Button size="sm" variant="danger" onClick={() => handleDeactivate(station)}>
                        Deactivate
                      </Button>
                    ) : (
                      <Button size="sm" variant="success" onClick={() => handleActivate(station)}>
                        Activate
                      </Button>
                    )}
                    <Button size="sm" variant="outline-danger" className="ms-2" onClick={() => handleDelete(station)}>
                      Delete
                    </Button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {stations.length === 0 && (
            <tr>
              <td colSpan={8} className="text-center text-body-secondary">
                No nodes yet
              </td>
            </tr>
          )}
        </tbody>
      </Table>

      <Modal show={showForm} onHide={() => setShowForm(false)}>
        <Form onSubmit={handleSave}>
          <Modal.Header closeButton>
            <Modal.Title>{editingId ? 'Edit node' : 'Add node'}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            {formError && <Alert variant="danger">{formError}</Alert>}

            <Form.Group className="mb-3">
              <Form.Label>Name</Form.Label>
              <Form.Control value={form.name} onChange={(e) => setField('name', e.target.value)} required />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Address</Form.Label>
              <Form.Control value={form.address} onChange={(e) => setField('address', e.target.value)} required />
            </Form.Group>

            <Row>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Latitude</Form.Label>
                <Form.Control
                  type="number"
                  step="any"
                  value={form.latitude}
                  onChange={(e) => setField('latitude', e.target.value)}
                  required
                />
              </Form.Group>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Longitude</Form.Label>
                <Form.Control
                  type="number"
                  step="any"
                  value={form.longitude}
                  onChange={(e) => setField('longitude', e.target.value)}
                  required
                />
              </Form.Group>
            </Row>

            <Row>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Capacity (kW/h)</Form.Label>
                <Form.Control
                  type="number"
                  step="any"
                  value={form.capacityKwh}
                  onChange={(e) => setField('capacityKwh', e.target.value)}
                  required
                />
              </Form.Group>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Battery storage slots</Form.Label>
                <Form.Control
                  type="number"
                  value={form.batterySlotCount}
                  onChange={(e) => setField('batterySlotCount', e.target.value)}
                  required
                />
              </Form.Group>
            </Row>

            <Row>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Opening time</Form.Label>
                <Form.Control
                  type="time"
                  value={form.openingTime}
                  onChange={(e) => setField('openingTime', e.target.value)}
                  required
                />
              </Form.Group>
              <Form.Group as={Col} className="mb-3">
                <Form.Label>Closing time</Form.Label>
                <Form.Control
                  type="time"
                  value={form.closingTime}
                  onChange={(e) => setField('closingTime', e.target.value)}
                  required
                />
              </Form.Group>
            </Row>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={() => setShowForm(false)}>
              Close
            </Button>
            <Button type="submit">Save</Button>
          </Modal.Footer>
        </Form>
      </Modal>

      <Modal show={confirming !== null} onHide={() => setConfirming(null)}>
        <Modal.Header closeButton>
          <Modal.Title>{confirming?.action === 'delete' ? 'Delete node' : 'Deactivate node'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {confirmError && <Alert variant="danger">{confirmError}</Alert>}
          Are you sure you want to {confirming?.action} {confirming?.station.name}?
          {confirming?.action === 'delete' && ' This cannot be undone.'}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setConfirming(null)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={confirmAction}>
            Yes
          </Button>
        </Modal.Footer>
      </Modal>
    </Layout>
  )
}

export default Stations
