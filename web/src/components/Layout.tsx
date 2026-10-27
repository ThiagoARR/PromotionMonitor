import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'

export function Layout() {
  const navigate = useNavigate()
  const user = (() => {
    try {
      return JSON.parse(localStorage.getItem('pm_user') ?? 'null') as {
        displayName?: string
        email?: string
      } | null
    } catch {
      return null
    }
  })()

  function logout() {
    localStorage.removeItem('pm_token')
    localStorage.removeItem('pm_user')
    navigate('/login')
  }

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-black/5 bg-[var(--color-sand)]/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
          <Link to="/" className="group flex items-baseline gap-2">
            <span className="font-[family-name:var(--font-display)] text-2xl font-bold text-[var(--color-moss-deep)] transition group-hover:text-[var(--color-moss)]">
              PromotionMonitor
            </span>
            <span className="hidden text-xs uppercase tracking-[0.18em] text-black/45 sm:inline">
              Phase 2
            </span>
          </Link>
          <nav className="flex items-center gap-2 text-sm">
            <NavLink
              to="/"
              className={({ isActive }) =>
                `rounded-md px-3 py-1.5 ${isActive ? 'bg-black/5 font-medium' : 'text-black/60 hover:text-black'}`
              }
            >
              Dashboard
            </NavLink>
            <NavLink
              to="/monitorings/new"
              className={({ isActive }) =>
                `rounded-md px-3 py-1.5 ${isActive ? 'bg-black/5 font-medium' : 'text-black/60 hover:text-black'}`
              }
            >
              New watch
            </NavLink>
            <NavLink
              to="/rules"
              className={({ isActive }) =>
                `rounded-md px-3 py-1.5 ${isActive ? 'bg-black/5 font-medium' : 'text-black/60 hover:text-black'}`
              }
            >
              Rules
            </NavLink>
            <span className="hidden text-black/50 md:inline">{user?.displayName ?? user?.email}</span>
            <Button variant="secondary" size="sm" onClick={logout}>
              Sign out
            </Button>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
