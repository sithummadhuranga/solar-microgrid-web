// backoffice page to list, add and edit backoffice and grid operator users
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

type StaffUser = {
  id: string
  email: string
  fullName: string
  role: string
  status: string
}

const emptyForm = {
  email: '',
  password: '',
  fullName: '',
  role: 'GridOperator',
}

function Users() {
  const [users, setUsers] = useState<StaffUser[]>([])
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState('')

  useEffect(() => {
    loadUsers()
  }, [])

  // loads every backoffice and grid operator user from the api
  async function loadUsers() {
    try {
      setUsers(await callApi('/api/users'))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // opens an empty form for a new user
  function openAdd() {
    setEditingId(null)
    setForm(emptyForm)
    setFormError('')
    setShowForm(true)
  }

  // opens the form filled with an existing user, the password box starts empty
  function openEdit(user: StaffUser) {
    setEditingId(user.id)
    setForm({ email: user.email, password: '', fullName: user.fullName, role: user.role })
    setFormError('')
    setShowForm(true)
  }

  // updates one field of the form
  function setField(field: keyof typeof emptyForm, value: string) {
    setForm({ ...form, [field]: value })
  }

  // sends the new or changed user to the api
  async function handleSave(e: FormEvent) {
    e.preventDefault()
    setFormError('')

    try {
      if (editingId) await callApi(`/api/users/${editingId}`, 'PUT', form)
      else await callApi('/api/users', 'POST', form)
      setShowForm(false)
      loadUsers()
    } catch (err) {
      setFormError((err as Error).message)
    }
  }

  return (
    <Layout>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="fw-semibold mb-0">Web users</h1>
        <Button onClick={openAdd}>Add user</Button>
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <td>{user.fullName}</td>
              <td>{user.email}</td>
              <td>{user.role === 'GridOperator' ? 'Grid Operator' : user.role}</td>
              <td>
                <Badge bg={user.status === 'active' ? 'success' : 'secondary'}>{user.status}</Badge>
              </td>
              <td className="text-nowrap">
                <Button size="sm" variant="outline-secondary" onClick={() => openEdit(user)}>
                  Edit
                </Button>
              </td>
            </tr>
          ))}
          {users.length === 0 && (
            <tr>
              <td colSpan={5} className="text-center text-body-secondary">
                No users yet
              </td>
            </tr>
          )}
        </tbody>
      </Table>

      <Modal show={showForm} onHide={() => setShowForm(false)}>
        <Form onSubmit={handleSave}>
          <Modal.Header closeButton>
            <Modal.Title>{editingId ? 'Edit user' : 'Add user'}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            {formError && <Alert variant="danger">{formError}</Alert>}

            <Form.Group className="mb-3">
              <Form.Label>Full name</Form.Label>
              <Form.Control value={form.fullName} onChange={(e) => setField('fullName', e.target.value)} required />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Email</Form.Label>
              <Form.Control
                type="email"
                value={form.email}
                onChange={(e) => setField('email', e.target.value)}
                required
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Role</Form.Label>
              <Form.Select value={form.role} onChange={(e) => setField('role', e.target.value)}>
                <option value="GridOperator">Grid Operator</option>
                <option value="Backoffice">Backoffice</option>
              </Form.Select>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Password{editingId ? ' (leave blank to keep it)' : ''}</Form.Label>
              <Form.Control
                type="password"
                value={form.password}
                onChange={(e) => setField('password', e.target.value)}
                required={!editingId}
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
    </Layout>
  )
}

export default Users
