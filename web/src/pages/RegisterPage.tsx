import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function RegisterPage() {
  const navigate = useNavigate()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const auth = await api.register(email, password, displayName)
      localStorage.setItem('pm_token', auth.token)
      localStorage.setItem(
        'pm_user',
        JSON.stringify({
          userId: auth.userId,
          email: auth.email,
          displayName: auth.displayName,
        }),
      )
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <div className="mb-8">
        <p className="text-xs uppercase tracking-[0.2em] text-[var(--color-amber)]">Get started</p>
        <h1 className="mt-2 text-4xl text-[var(--color-moss-deep)]">PromotionMonitor</h1>
        <p className="mt-2 text-black/60">Register a product watch without changing code.</p>
      </div>
      <form onSubmit={onSubmit} className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6">
        <div className="space-y-2">
          <Label htmlFor="name">Display name</Label>
          <Input id="name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="password">Password (min 8)</Label>
          <Input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
          />
        </div>
        {error && <p className="text-sm text-[var(--color-danger)]">{error}</p>}
        <Button className="w-full" disabled={loading}>
          {loading ? 'Creating…' : 'Create account'}
        </Button>
        <p className="text-center text-sm text-black/55">
          Already registered?{' '}
          <Link className="font-medium text-[var(--color-moss)]" to="/login">
            Sign in
          </Link>
        </p>
      </form>
    </div>
  )
}
