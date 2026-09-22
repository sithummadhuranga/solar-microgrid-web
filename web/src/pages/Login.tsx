import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router'
import Container from 'react-bootstrap/Container'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Alert from 'react-bootstrap/Alert'
import { callApi } from '../api'
import { saveLogin } from '../auth'

type LoginResult = {
  token: string
  id: string
  fullName: string
  role: string
}

// web login, for backoffice and grid operator only, prosumers use the mobile app
function Login() {
  const [identifier, setIdentifier] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const navigate = useNavigate()

  // sends the login details to the api and sends the user to their home page
  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')

    try {
      const result: LoginResult = await callApi('/api/auth/login', 'POST', {
        identifier,
        password,
        platform: 'web',
      })

      saveLogin(result.token, {
        id: result.id,
        fullName: result.fullName,
        role: result.role,
      })

      navigate(result.role === 'Backoffice' ? '/admin' : '/operator')
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <Container style={{ maxWidth: '400px' }} className="mt-5">
      <h1>Log in</h1>

      {error && <Alert variant="danger">{error}</Alert>}

      <Form onSubmit={handleSubmit}>
        <Form.Group className="mb-3">
          <Form.Label>Email</Form.Label>
          <Form.Control
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
            required
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>Password</Form.Label>
          <Form.Control
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </Form.Group>

        <Button type="submit" variant="primary">
          Log in
        </Button>
      </Form>
    </Container>
  )
}

export default Login
