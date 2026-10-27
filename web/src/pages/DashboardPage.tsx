import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, type DashboardSummary } from '@/lib/api'
import { formatBrl } from '@/lib/utils'
import { Button } from '@/components/ui/button'

const voltageLabel: Record<number, string> = {
  0: 'Any',
  110: '110V',
  220: '220V',
  110220: 'Dual',
}

export function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const summary = await api.dashboard()
        if (!cancelled) setData(summary)
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load')
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  if (loading) {
    return <p className="text-black/55 animate-pulse">Loading dashboard…</p>
  }

  if (error) {
    return <p className="text-[var(--color-danger)]">{error}</p>
  }

  if (!data) return null

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-4xl text-[var(--color-moss-deep)]">Your watches</h1>
          <p className="mt-2 max-w-xl text-black/60">
            Track listed price, pay-at-checkout, and cashback separately — cashback never reduces checkout.
          </p>
        </div>
        <Button asChild>
          <Link to="/monitorings/new">New monitoring</Link>
        </Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        {[
          ['Active', data.activeMonitorings],
          ['Checks (24h)', data.observationsLast24h],
          ['Alerts (24h)', data.alertsLast24h],
        ].map(([label, value], i) => (
          <div
            key={label as string}
            className="rounded-xl border border-black/5 bg-white/60 px-5 py-4"
            style={{ animation: `fadeUp 0.5s ease ${i * 0.08}s both` }}
          >
            <p className="text-xs uppercase tracking-[0.16em] text-black/45">{label}</p>
            <p className="mt-2 font-[family-name:var(--font-display)] text-3xl">{value}</p>
          </div>
        ))}
      </div>

      {data.monitorings.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-black/15 bg-white/40 px-6 py-12 text-center">
          <h2 className="text-2xl">No products yet</h2>
          <p className="mt-2 text-black/55">Register a URL, voltage filter, and max price to start.</p>
          <Button className="mt-6" asChild>
            <Link to="/monitorings/new">Add first watch</Link>
          </Button>
        </div>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/5 bg-black/[0.02] text-xs uppercase tracking-wider text-black/45">
              <tr>
                <th className="px-4 py-3 font-medium">Product</th>
                <th className="hidden px-4 py-3 font-medium md:table-cell">Voltage</th>
                <th className="px-4 py-3 font-medium">Pay at checkout</th>
                <th className="hidden px-4 py-3 font-medium sm:table-cell">Max</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {data.monitorings.map((m) => (
                <tr key={m.id} className="border-b border-black/5 last:border-0 hover:bg-[var(--color-fog)]/50">
                  <td className="px-4 py-3">
                    <Link className="font-medium text-[var(--color-moss-deep)] hover:underline" to={`/monitorings/${m.id}`}>
                      {m.name}
                    </Link>
                    <p className="mt-0.5 truncate text-xs text-black/45">{m.modelName ?? m.productUrl}</p>
                  </td>
                  <td className="hidden px-4 py-3 md:table-cell">{voltageLabel[m.voltageFilter] ?? m.voltageFilter}</td>
                  <td className="px-4 py-3 tabular-nums">{formatBrl(m.latestPayAtCheckout)}</td>
                  <td className="hidden px-4 py-3 tabular-nums sm:table-cell">{formatBrl(m.maxPrice)}</td>
                  <td className="px-4 py-3">
                    <span
                      className={`inline-flex rounded-full px-2 py-0.5 text-xs ${
                        m.latestStatus === 'ok'
                          ? 'bg-emerald-100 text-emerald-800'
                          : m.latestStatus === 'error'
                            ? 'bg-red-100 text-red-800'
                            : 'bg-black/5 text-black/55'
                      }`}
                    >
                      {m.isActive ? m.latestStatus ?? 'pending' : 'paused'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <style>{`@keyframes fadeUp { from { opacity: 0; transform: translateY(8px);} to { opacity: 1; transform: none; } }`}</style>
    </div>
  )
}
