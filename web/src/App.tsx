// the route list, one line per page
import { Routes, Route } from 'react-router'
import Home from './pages/Home'
import Login from './pages/auth/Login'
import AdminHome from './pages/admin/AdminHome'
import OperatorHome from './pages/operator/OperatorHome'
import Stations from './pages/admin/Stations'
import StationSlots from './pages/admin/StationSlots'
import RequireRole from './components/RequireRole'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route
        path="/admin"
        element={
          <RequireRole role="Backoffice">
            <AdminHome />
          </RequireRole>
        }
      />
      <Route
        path="/admin/stations"
        element={
          <RequireRole role="Backoffice">
            <Stations />
          </RequireRole>
        }
      />
      <Route
        path="/admin/stations/:id/slots"
        element={
          <RequireRole role="Backoffice">
            <StationSlots />
          </RequireRole>
        }
      />
      <Route
        path="/operator"
        element={
          <RequireRole role="GridOperator">
            <OperatorHome />
          </RequireRole>
        }
      />
    </Routes>
  )
}

export default App
