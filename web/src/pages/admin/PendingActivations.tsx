// backoffice page to see and activate prosumers waiting for activation
import { useEffect, useState } from 'react'
import Table from 'react-bootstrap/Table'
import Button from 'react-bootstrap/Button'
import Alert from 'react-bootstrap/Alert'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'

type Prosumer = {
  id: string
  nic: string
  fullName: string
  phone: string
  address: string
}

function PendingActivations() {
  const [prosumers, setProsumers] = useState<Prosumer[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    loadPending()
  }, [])

  // loads prosumers waiting for activation from the api
  async function loadPending() {
    try {
      setProsumers(await callApi('/api/prosumers/pending'))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // activates a prosumer so they can log in
  async function handleActivate(prosumer: Prosumer) {
    setError('')

    try {
      await callApi(`/api/prosumers/${prosumer.id}/activate`, 'POST')
      loadPending()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <Layout>
      <h1 className="fw-semibold mb-3">Pending activations</h1>

      {error && <Alert variant="danger">{error}</Alert>}

      <Table responsive bordered hover className="bg-white">
        <thead>
          <tr>
            <th>Name</th>
            <th>NIC</th>
            <th>Phone</th>
            <th>Address</th>
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
                <Button size="sm" onClick={() => handleActivate(prosumer)}>
                  Activate
                </Button>
              </td>
            </tr>
          ))}
          {prosumers.length === 0 && (
            <tr>
              <td colSpan={5} className="text-center text-body-secondary">
                Nothing waiting for activation
              </td>
            </tr>
          )}
        </tbody>
      </Table>
    </Layout>
  )
}

export default PendingActivations
