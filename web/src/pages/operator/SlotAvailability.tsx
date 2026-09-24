// grid operator page to update how many battery storage slots are still free
import { useEffect, useState } from 'react'
import Table from 'react-bootstrap/Table'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import Alert from 'react-bootstrap/Alert'
import Layout from '../../components/Layout'
import { callApi } from '../../lib/api'

type Station = {
  id: string
  name: string
}

type Slot = {
  id: string
  stationId: string
  startTime: string
  endTime: string
  totalSlots: number
  availableSlots: number
}

// turns a utc iso string from the api into a readable local date and time
function showTime(iso: string) {
  return new Date(iso).toLocaleString()
}

function SlotAvailability() {
  const [stations, setStations] = useState<Station[]>([])
  const [stationId, setStationId] = useState('')
  const [slots, setSlots] = useState<Slot[]>([])
  const [edited, setEdited] = useState<Record<string, string>>({})
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    // loads the microgrid nodes when the page opens
    async function load() {
      try {
        setStations(await callApi('/api/stations'))
      } catch (err) {
        setError((err as Error).message)
      }
    }

    load()
  }, [])

  // loads the slots of the picked node
  async function loadSlots(id: string) {
    setStationId(id)
    setEdited({})
    setMessage('')
    setError('')

    if (!id) {
      setSlots([])
      return
    }

    try {
      setSlots(await callApi(`/api/stations/${id}/slots`))
    } catch (err) {
      setError((err as Error).message)
    }
  }

  // keeps what the operator typed for one slot until it is saved
  function setCount(slotId: string, value: string) {
    setEdited({ ...edited, [slotId]: value })
  }

  // sends the new free slot count to the api
  async function handleSave(slot: Slot) {
    setMessage('')
    setError('')

    try {
      await callApi(`/api/stations/${slot.stationId}/slots/${slot.id}/availability`, 'PUT', {
        availableSlots: Number(edited[slot.id] ?? slot.availableSlots),
      })
      setMessage('Availability saved')
      loadSlots(stationId)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <Layout>
      <h1 className="fw-semibold mb-3">Battery slot availability</h1>

      {error && <Alert variant="danger">{error}</Alert>}
      {message && <Alert variant="success">{message}</Alert>}

      <Form.Group className="mb-3" controlId="station">
        <Form.Label>Microgrid node</Form.Label>
        <Form.Select value={stationId} onChange={(e) => loadSlots(e.target.value)}>
          <option value="">Pick a node</option>
          {stations.map((station) => (
            <option key={station.id} value={station.id}>
              {station.name}
            </option>
          ))}
        </Form.Select>
      </Form.Group>

      {stationId && (
        <Table responsive bordered hover className="bg-white">
          <thead>
            <tr>
              <th>Start</th>
              <th>End</th>
              <th>Battery storage slots</th>
              <th>Still free</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {slots.map((slot) => (
              <tr key={slot.id}>
                <td>{showTime(slot.startTime)}</td>
                <td>{showTime(slot.endTime)}</td>
                <td>{slot.totalSlots}</td>
                <td style={{ maxWidth: '8rem' }}>
                  <Form.Control
                    type="number"
                    min={0}
                    max={slot.totalSlots}
                    value={edited[slot.id] ?? String(slot.availableSlots)}
                    onChange={(e) => setCount(slot.id, e.target.value)}
                  />
                </td>
                <td>
                  <Button size="sm" onClick={() => handleSave(slot)}>
                    Save
                  </Button>
                </td>
              </tr>
            ))}
            {slots.length === 0 && (
              <tr>
                <td colSpan={5} className="text-center text-body-secondary">
                  This node has no slots yet
                </td>
              </tr>
            )}
          </tbody>
        </Table>
      )}
    </Layout>
  )
}

export default SlotAvailability
