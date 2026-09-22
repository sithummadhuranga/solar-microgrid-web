import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router'
import Container from 'react-bootstrap/Container'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Alert from 'react-bootstrap/Alert'
import { callApi } from '../lib/api'
import { saveLogin } from '../lib/auth'

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
    <Container
      fluid
      className="min-vh-100 d-flex align-items-center justify-content-center bg-body-tertiary"
    >
      <Row className="w-100">
        <Col xs={12} sm={8} md={5} lg={4} className="mx-auto">
          <Card className="shadow-sm border-0">
            <Card.Header className="bg-primary text-white text-center py-3">
              <span className="fs-5 fw-semibold">Solar Microgrid</span>
            </Card.Header>
            <Card.Body className="p-4">
              {error && <Alert variant="danger">{error}</Alert>}

              <Form onSubmit={handleSubmit}>
                <Form.Group className="mb-3">
                  <Form.Label>Email</Form.Label>
                  <Form.Control
                    value={identifier}
                    onChange={(e) => setIdentifier(e.target.value)}
                    required
                    autoFocus
                  />
                </Form.Group>

                <Form.Group className="mb-4">
                  <Form.Label>Password</Form.Label>
                  <Form.Control
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                </Form.Group>

                <Button type="submit" variant="primary" className="w-100">
                  Log in
                </Button>
              </Form>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  )
}

export default Login
