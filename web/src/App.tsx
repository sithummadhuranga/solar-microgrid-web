import { Routes, Route } from 'react-router'
import Home from './pages/Home'
import Login from './pages/Login'
import AdminHome from './pages/AdminHome'
import OperatorHome from './pages/OperatorHome'
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
    </Routes>
  )
}

export default App
