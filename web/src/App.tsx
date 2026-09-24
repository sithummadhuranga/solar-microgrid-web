// the route list, one line per page
import { Routes, Route } from 'react-router'
import Home from './pages/Home'
import Login from './pages/auth/Login'
import AdminHome from './pages/admin/AdminHome'
import OperatorHome from './pages/operator/OperatorHome'
import Stations from './pages/admin/Stations'
import StationSlots from './pages/admin/StationSlots'
import Users from './pages/admin/Users'
import Prosumers from './pages/admin/Prosumers'
import PendingActivations from './pages/admin/PendingActivations'
import ReservationsPage from './pages/reservations/ReservationsPage'
import BookingMonitor from './pages/operator/BookingMonitor'
import SlotAvailability from './pages/operator/SlotAvailability'
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
        path="/admin/users"
        element={
          <RequireRole role="Backoffice">
            <Users />
          </RequireRole>
        }
      />
      <Route
        path="/admin/prosumers"
        element={
          <RequireRole role="Backoffice">
            <Prosumers />
          </RequireRole>
        }
      />
      <Route
        path="/admin/pending-activations"
        element={
          <RequireRole role="Backoffice">
            <PendingActivations />
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
      <Route
        path="/operator/stations"
        element={
          <RequireRole role="GridOperator">
            <Stations />
          </RequireRole>
        }
      />
      <Route
        path="/operator/stations/:id/slots"
        element={
          <RequireRole role="GridOperator">
            <StationSlots />
          </RequireRole>
        }
      />
      <Route
        path="/admin/reservations"
        element={
          <RequireRole role="Backoffice">
            <ReservationsPage />
          </RequireRole>
        }
      />
      <Route
        path="/operator/reservations"
        element={
          <RequireRole role="GridOperator">
            <ReservationsPage />
          </RequireRole>
        }
      />
      <Route
        path="/operator/bookings"
        element={
          <RequireRole role="GridOperator">
            <BookingMonitor />
          </RequireRole>
        }
      />
      <Route
        path="/operator/slots"
        element={
          <RequireRole role="GridOperator">
            <SlotAvailability />
          </RequireRole>
        }
      />
    </Routes>
  )
}

export default App
