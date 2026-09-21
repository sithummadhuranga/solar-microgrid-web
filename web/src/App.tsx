import { Routes, Route } from 'react-router'
import Container from 'react-bootstrap/Container'

function Home() {
  return (
    <Container>
      <h1>Solar Microgrid</h1>
    </Container>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
    </Routes>
  )
}

export default App
