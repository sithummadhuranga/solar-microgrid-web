// the route list, one line per page
import { Routes, Route } from 'react-router'
import Home from './pages/Home'
import Login from './pages/auth/Login'
import AdminHome from './pages/admin/AdminHome'
import OperatorHome from './pages/operator/OperatorHome'
import ReservationsPage from './pages/reservations/ReservationsPage'
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
        path="/operator"
        element={
          <RequireRole role="GridOperator">
            <OperatorHome />
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
    </Routes>
  )
}

export default App
