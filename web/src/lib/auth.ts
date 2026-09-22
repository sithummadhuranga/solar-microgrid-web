// saves, reads and clears the logged in user and token in localStorage
export type LoggedInUser = {
  id: string
  fullName: string
  role: string
}

// saves the token and user after a successful login
export function saveLogin(token: string, user: LoggedInUser) {
  localStorage.setItem('token', token)
  localStorage.setItem('user', JSON.stringify(user))
}

// reads the logged in user, or null if nobody is logged in
export function getUser(): LoggedInUser | null {
  const raw = localStorage.getItem('user')
  return raw ? JSON.parse(raw) : null
}

// clears the saved login
export function logout() {
  localStorage.removeItem('token')
  localStorage.removeItem('user')
}
