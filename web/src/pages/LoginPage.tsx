import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const auth = await api.login(email, password)
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
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <div className="mb-8 animate-[fadeUp_0.6s_ease]">
        <p className="text-xs uppercase tracking-[0.2em] text-[var(--color-amber)]">Welcome back</p>
        <h1 className="mt-2 text-4xl text-[var(--color-moss-deep)]">PromotionMonitor</h1>
        <p className="mt-2 text-black/60">Sign in to review watches, history, and alerts.</p>
      </div>
      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6 shadow-[0_20px_60px_-40px_rgba(20,32,27,0.45)] animate-[fadeUp_0.7s_ease]"
      >
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
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
          {loading ? 'Signing in…' : 'Sign in'}
        </Button>
        <p className="text-center text-sm text-black/55">
          New here?{' '}
          <Link className="font-medium text-[var(--color-moss)] underline-offset-2 hover:underline" to="/register">
            Create an account
          </Link>
        </p>
      </form>
      <style>{`@keyframes fadeUp { from { opacity: 0; transform: translateY(10px);} to { opacity: 1; transform: none; } }`}</style>
    </div>
  )
}
