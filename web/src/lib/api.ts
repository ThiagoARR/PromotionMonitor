const API_BASE = import.meta.env.VITE_API_URL ?? ''

export type AuthResponse = {
  userId: string
  email: string
  displayName: string
  token: string
}

export type MonitoringListItem = {
  id: string
  name: string
  productUrl: string
  modelName?: string | null
  voltageFilter: number
  targetPrice?: number | null
  maxPrice: number
  scheduleMinutes: number
  sourceKind: number
  isActive: boolean
  demoMode: boolean
  lastCheckedAt?: string | null
  latestPayAtCheckout?: number | null
  latestListedPrice?: number | null
  latestVoltageMatches?: boolean | null
  latestStatus?: string | null
}

export type PriceCalculation = {
  listedPrice: number
  shipping: number
  couponDiscount: number
  payAtCheckout: number
  cashbackEstimate: number
  netAfterCashback: number
  notes: string
}

export type Observation = {
  id: string
  observedAt: string
  sourceName: string
  success: boolean
  productTitle?: string | null
  listedPrice?: number | null
  voltageDetected?: string | null
  voltageMatches?: boolean | null
  errorMessage?: string | null
  calculation?: PriceCalculation | null
}

export type MonitoringDetail = {
  id: string
  name: string
  productUrl: string
  modelName?: string | null
  voltageFilter: number
  targetPrice?: number | null
  maxPrice: number
  cep?: string | null
  scheduleMinutes: number
  sourceKind: number
  isActive: boolean
  demoMode: boolean
  lastCheckedAt?: string | null
  observations: Observation[]
  recentAlerts: {
    id: string
    channel: number
    status: string
    subject: string
    body: string
    createdAt: string
    deliveredAt?: string | null
  }[]
}

export type DashboardSummary = {
  activeMonitorings: number
  observationsLast24h: number
  alertsLast24h: number
  monitorings: MonitoringListItem[]
}

function authHeaders(): HeadersInit {
  const token = localStorage.getItem('pm_token')
  return token
    ? { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }
    : { 'Content-Type': 'application/json' }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { ...authHeaders(), ...(init?.headers ?? {}) },
  })
  if (res.status === 401) {
    localStorage.removeItem('pm_token')
    localStorage.removeItem('pm_user')
    if (!path.includes('/auth/login') && !path.includes('/auth/register')) {
      window.location.href = '/login'
    }
    throw new Error('Unauthorized')
  }
  if (!res.ok) {
    let message = `Request failed (${res.status})`
    try {
      const body = await res.json()
      message = body.error ?? body.title ?? message
    } catch {
      /* ignore */
    }
    throw new Error(message)
  }
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const api = {
  register: (email: string, password: string, displayName: string) =>
    request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, displayName }),
    }),
  login: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  me: () => request<{ id: string; email: string; displayName: string }>('/api/auth/me'),
  dashboard: () => request<DashboardSummary>('/api/dashboard'),
  getMonitoring: (id: string) => request<MonitoringDetail>(`/api/monitorings/${id}`),
  createMonitoring: (body: Record<string, unknown>) =>
    request<{ id: string }>('/api/monitorings', { method: 'POST', body: JSON.stringify(body) }),
  updateMonitoring: (id: string, body: Record<string, unknown>) =>
    request<{ id: string }>(`/api/monitorings/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteMonitoring: (id: string) =>
    request<void>(`/api/monitorings/${id}`, { method: 'DELETE' }),
  checkNow: (id: string) =>
    request<{ queued: boolean }>(`/api/monitorings/${id}/check-now`, { method: 'POST' }),
  history: (id: string, days = 30) =>
    request<
      {
        observedAt: string
        listedPrice?: number | null
        payAtCheckout?: number | null
        cashbackEstimate?: number | null
        netAfterCashback?: number | null
      }[]
    >(`/api/monitorings/${id}/history?days=${days}`),
}
