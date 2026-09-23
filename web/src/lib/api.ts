// calls the api, adds the token header, and throws the server message if it fails
const API_URL = import.meta.env.VITE_API_URL

export async function callApi(path: string, method = 'GET', body: unknown = null) {
  const token = localStorage.getItem('token')

  const res = await fetch(API_URL + path, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body ? JSON.stringify(body) : null,
  })

  const text = await res.text()
  const data = text ? JSON.parse(text) : null

  // only a signed in call can mean an expired session, a login attempt with no token is just a wrong password
  if (res.status === 401 && token) {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    window.location.href = '/login'
  }

  if (!res.ok) throw new Error(data?.message ?? text)

  return data
}
