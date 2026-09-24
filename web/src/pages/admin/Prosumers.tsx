// backoffice page to list, add, edit, deactivate and reactivate prosumers
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import Table from 'react-bootstrap/Table'
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'

type Prosumer = {
  id: string
  nic: string
  fullName: string
  phone: string
  address: string
  status: string
  deactivationRequested: boolean
}

const emptyForm = {
  nic: '',
  password: '',
  confirmPassword: '',
  fullName: '',
  phone: '',
  address: '',
}

const minPasswordLength = 8

function Prosumers() {
  const [prosumers, setProsumers] = useState<Prosumer[]>([])
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState('')

  useEffect(() => {
    loadProsumers()
  }, [])

  // loads every prosumer from the api
  async function loadProsumers() {
    try {
      setProsumers(await callApi('/api/prosumers'))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // opens an empty form for a new prosumer
  function openAdd() {
    setEditingId(null)
    setForm(emptyForm)
    setFormError('')
    setShowForm(true)
  }

  // opens the form filled with an existing prosumer, the password box is not shown for an edit
  function openEdit(prosumer: Prosumer) {
    setEditingId(prosumer.id)
    setForm({
      nic: prosumer.nic,
      password: '',
      confirmPassword: '',
      fullName: prosumer.fullName,
      phone: prosumer.phone,
      address: prosumer.address,
    })
    setFormError('')
    setShowForm(true)
  }

  // updates one field of the form
  function setField(field: keyof typeof emptyForm, value: string) {
    setForm({ ...form, [field]: value })
  }

  // checks the new prosumer form, returns an error message or empty
  function validate(): string {
    if (editingId) return ''
    if (form.password.length < minPasswordLength) return `Password must be at least ${minPasswordLength} characters`
    if (form.password !== form.confirmPassword) return 'Passwords do not match'
    return ''
  }

  // sends the new or changed prosumer to the api, a new one is created pending
  async function handleSave(e: FormEvent) {
    e.preventDefault()
    setFormError('')

    const validationError = validate()
    if (validationError) {
      setFormError(validationError)
      return
    }

    try {
      if (editingId) {
        await callApi(`/api/prosumers/${editingId}`, 'PUT', {
          nic: form.nic,
          fullName: form.fullName,
          phone: form.phone,
          address: form.address,
        })
      } else {
        await callApi('/api/prosumers/register', 'POST', form)
      }
      setShowForm(false)
      loadProsumers()
    } catch (err) {
      setFormError((err as Error).message)
    }
  }

  // deactivates a prosumer after the user confirms
  async function handleDeactivate(prosumer: Prosumer) {
    if (!window.confirm(`Deactivate ${prosumer.fullName}?`)) return
    setError('')

    try {
      await callApi(`/api/prosumers/${prosumer.id}/deactivate`, 'POST')
      loadProsumers()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // reactivates a deactivated prosumer
  async function handleReactivate(prosumer: Prosumer) {
    setError('')

    try {
      await callApi(`/api/prosumers/${prosumer.id}/reactivate`, 'POST')
      loadProsumers()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <Layout>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="fw-semibold mb-0">Prosumers</h1>
        <Button onClick={openAdd}>Add prosumer</Button>
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>Name</th>
            <th>NIC</th>
            <th>Phone</th>
            <th>Address</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {prosumers.map((prosumer) => (
            <tr key={prosumer.id}>
              <td>{prosumer.fullName}</td>
              <td>{prosumer.nic}</td>
              <td>{prosumer.phone}</td>
              <td>{prosumer.address}</td>
              <td>
                <Badge bg={prosumer.status === 'active' ? 'success' : prosumer.status === 'pending' ? 'warning' : 'secondary'}>
                  {prosumer.status}
                </Badge>
                {prosumer.deactivationRequested && (
                  <Badge bg="danger" className="ms-2">
                    deactivation requested
                  </Badge>
                )}
              </td>
              <td className="text-nowrap">
                <Button size="sm" variant="outline-secondary" className="me-2" onClick={() => openEdit(prosumer)}>
                  Edit
                </Button>
                {prosumer.status === 'active' && (
                  <Button size="sm" variant="danger" onClick={() => handleDeactivate(prosumer)}>
                    Deactivate
                  </Button>
                )}
                {prosumer.status === 'deactivated' && (
                  <Button size="sm" variant="success" onClick={() => handleReactivate(prosumer)}>
                    Reactivate
                  </Button>
                )}
              </td>
            </tr>
          ))}
          {prosumers.length === 0 && (
            <tr>
              <td colSpan={6} className="text-center text-body-secondary">
                No prosumers yet
              </td>
            </tr>
          )}
        </tbody>
      </Table>

      <Modal show={showForm} onHide={() => setShowForm(false)}>
        <Form onSubmit={handleSave}>
          <Modal.Header closeButton>
            <Modal.Title>{editingId ? 'Edit prosumer' : 'Add prosumer'}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            {formError && <Alert variant="danger">{formError}</Alert>}

            <Form.Group className="mb-3">
              <Form.Label>NIC</Form.Label>
              <Form.Control value={form.nic} onChange={(e) => setField('nic', e.target.value)} required />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Full name</Form.Label>
              <Form.Control value={form.fullName} onChange={(e) => setField('fullName', e.target.value)} required />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Phone</Form.Label>
              <Form.Control value={form.phone} onChange={(e) => setField('phone', e.target.value)} required />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Address</Form.Label>
              <Form.Control value={form.address} onChange={(e) => setField('address', e.target.value)} required />
            </Form.Group>

            {!editingId && (
              <Form.Group className="mb-3">
                <Form.Label>Password</Form.Label>
                <Form.Control
                  type="password"
                  value={form.password}
                  onChange={(e) => setField('password', e.target.value)}
                  required
                  minLength={minPasswordLength}
                />
              </Form.Group>
            )}

            {!editingId && (
              <Form.Group className="mb-3">
                <Form.Label>Confirm password</Form.Label>
                <Form.Control
                  type="password"
                  value={form.confirmPassword}
                  onChange={(e) => setField('confirmPassword', e.target.value)}
                  required
                />
              </Form.Group>
            )}

            {!editingId && (
              <Alert variant="info" className="py-2">
                New prosumers start pending, activate them from the pending activations page.
              </Alert>
            )}
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={() => setShowForm(false)}>
              Close
            </Button>
            <Button type="submit">Save</Button>
          </Modal.Footer>
        </Form>
      </Modal>
    </Layout>
  )
}

export default Prosumers
