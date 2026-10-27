import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { api, type MonitoringDetail, type StoreOffer } from '@/lib/api'
import { formatBrl } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export function MonitoringDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [detail, setDetail] = useState<MonitoringDetail | null>(null)
  const [history, setHistory] = useState<
    { observedAt: string; listedPrice?: number | null; payAtCheckout?: number | null; netAfterCashback?: number | null }[]
  >([])
  const [offers, setOffers] = useState<StoreOffer[]>([])
  const [storeLabel, setStoreLabel] = useState('')
  const [storeUrl, setStoreUrl] = useState('https://example.com/products/demo-airfryer-220')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function reload() {
    if (!id) return
    const [d, h, c] = await Promise.all([api.getMonitoring(id), api.history(id, 30), api.comparison(id)])
    setDetail(d)
    setHistory(h)
    setOffers(c)
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        await reload()
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load')
      }
    })()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  if (error) return <p className="text-[var(--color-danger)]">{error}</p>
  if (!detail) return <p className="animate-pulse text-black/55">Loading monitoring…</p>

  const latest = detail.observations.find((o) => o.success && o.calculation)
  const chartData = history.map((h) => ({
    t: new Date(h.observedAt).toLocaleString('pt-BR', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }),
    listed: h.listedPrice ?? undefined,
    checkout: h.payAtCheckout ?? undefined,
    net: h.netAfterCashback ?? undefined,
  }))

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <Link to="/" className="text-sm text-[var(--color-moss)] hover:underline">
            ← Dashboard
          </Link>
          <h1 className="mt-2 text-4xl text-[var(--color-moss-deep)]">{detail.name}</h1>
          <p className="mt-2 max-w-2xl break-all text-sm text-black/55">{detail.productUrl}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            disabled={busy}
            onClick={async () => {
              setBusy(true)
              try {
                await api.checkNow(detail.id)
                await new Promise((r) => setTimeout(r, 1500))
                await reload()
              } catch (err) {
                setError(err instanceof Error ? err.message : 'Check failed')
              } finally {
                setBusy(false)
              }
            }}
          >
            {busy ? 'Checking…' : 'Check now'}
          </Button>
          <Button
            variant="danger"
            onClick={async () => {
              if (!confirm('Delete this monitoring?')) return
              await api.deleteMonitoring(detail.id)
              navigate('/')
            }}
          >
            Delete
          </Button>
        </div>
      </div>

      {latest?.calculation ? (
        <div className="grid gap-4 md:grid-cols-2">
          <div className="rounded-2xl border border-black/5 bg-white/70 p-5">
            <p className="text-xs uppercase tracking-[0.16em] text-black/45">Pay at checkout</p>
            <p className="mt-2 font-[family-name:var(--font-display)] text-4xl text-[var(--color-moss-deep)]">
              {formatBrl(latest.calculation.payAtCheckout)}
            </p>
            <ul className="mt-4 space-y-1 text-sm text-black/70">
              <li>Listed: {formatBrl(latest.calculation.listedPrice)}</li>
              <li>Shipping: {formatBrl(latest.calculation.shipping)}</li>
              <li>Coupon: −{formatBrl(latest.calculation.couponDiscount)}</li>
            </ul>
          </div>
          <div className="rounded-2xl border border-black/5 bg-white/70 p-5">
            <p className="text-xs uppercase tracking-[0.16em] text-[var(--color-amber)]">Cashback (informational)</p>
            <p className="mt-2 font-[family-name:var(--font-display)] text-4xl">
              {formatBrl(latest.calculation.cashbackEstimate)}
            </p>
            <p className="mt-4 text-sm text-black/70">
              Net after cashback: <strong>{formatBrl(latest.calculation.netAfterCashback)}</strong>
            </p>
            <p className="mt-2 text-xs text-black/50">{latest.calculation.notes}</p>
          </div>
        </div>
      ) : (
        <div className="rounded-2xl border border-dashed border-black/15 bg-white/40 px-5 py-8 text-black/55">
          No successful offer yet. Use Check now, or wait for the schedule.
        </div>
      )}

      <div className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-2xl">Store comparison</h2>
            <p className="mt-1 text-sm text-black/50">
              Ranked by pay-at-checkout. Cashback never changes the checkout column.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Input
              placeholder="Store label"
              value={storeLabel}
              onChange={(e) => setStoreLabel(e.target.value)}
              className="w-36"
            />
            <Input
              placeholder="Product URL"
              value={storeUrl}
              onChange={(e) => setStoreUrl(e.target.value)}
              className="min-w-[220px] flex-1"
            />
            <Button
              variant="secondary"
              disabled={busy || !storeLabel}
              onClick={async () => {
                if (!id) return
                setBusy(true)
                try {
                  await api.addStore(id, {
                    label: storeLabel,
                    productUrl: storeUrl,
                    sourceKind: 0,
                    isPrimary: false,
                  })
                  setStoreLabel('')
                  await api.checkNow(id)
                  await new Promise((r) => setTimeout(r, 1500))
                  await reload()
                } catch (err) {
                  setError(err instanceof Error ? err.message : 'Failed to add store')
                } finally {
                  setBusy(false)
                }
              }}
            >
              Add store
            </Button>
          </div>
        </div>
        <div className="mt-4 overflow-x-auto">
          {offers.length === 0 ? (
            <p className="text-sm text-black/45">No comparison round yet — run Check now.</p>
          ) : (
            <table className="w-full text-left text-sm">
              <thead className="border-b border-black/5 text-xs uppercase tracking-wider text-black/45">
                <tr>
                  <th className="px-2 py-2">Store</th>
                  <th className="px-2 py-2">Listed</th>
                  <th className="px-2 py-2">Shipping</th>
                  <th className="px-2 py-2">Coupon</th>
                  <th className="px-2 py-2">Checkout</th>
                  <th className="px-2 py-2">Net after CB</th>
                </tr>
              </thead>
              <tbody>
                {offers.map((o) => (
                  <tr
                    key={o.id}
                    className={`border-b border-black/5 last:border-0 ${o.isBestPayAtCheckout ? 'bg-emerald-50/70' : ''}`}
                  >
                    <td className="px-2 py-2">
                      <div className="font-medium">{o.storeLabel}</div>
                      <div className="text-xs text-black/45">{o.success ? o.productTitle : o.errorMessage}</div>
                    </td>
                    <td className="px-2 py-2 tabular-nums">{formatBrl(o.listedPrice)}</td>
                    <td className="px-2 py-2 tabular-nums">
                      {formatBrl(o.shipping)}
                      {o.shippingSimulated ? <span className="text-xs text-black/40"> sim</span> : null}
                    </td>
                    <td className="px-2 py-2 tabular-nums">
                      {o.couponCode ? `${o.couponCode} −${formatBrl(o.couponDiscount)}` : '—'}
                    </td>
                    <td className="px-2 py-2 tabular-nums font-medium">
                      {formatBrl(o.payAtCheckout)}
                      {o.isBestPayAtCheckout ? <span className="ml-1 text-xs text-emerald-700">best</span> : null}
                    </td>
                    <td className="px-2 py-2 tabular-nums">
                      {formatBrl(o.netAfterCashback)}
                      {o.isBestNetAfterCashback ? <span className="ml-1 text-xs text-[var(--color-amber)]">best net</span> : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      <div className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h2 className="text-2xl">Price history (30 days)</h2>
        <p className="mt-1 text-sm text-black/50">Checkout vs informational net-after-cashback</p>
        <div className="mt-4 h-72 w-full">
          {chartData.length === 0 ? (
            <p className="text-sm text-black/45">No history points in this window.</p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#00000014" />
                <XAxis dataKey="t" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="listed" name="Listed" stroke="#8a8f87" strokeWidth={2} dot={false} />
                <Line type="monotone" dataKey="checkout" name="Pay at checkout" stroke="#1f6f4a" strokeWidth={2} />
                <Line
                  type="monotone"
                  dataKey="net"
                  name="Net after cashback"
                  stroke="#c47b2b"
                  strokeDasharray="4 4"
                  strokeWidth={2}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
          <h2 className="text-xl">Observations</h2>
          <ul className="mt-3 max-h-80 space-y-3 overflow-auto text-sm">
            {detail.observations.length === 0 && <li className="text-black/45">None yet.</li>}
            {detail.observations.map((o) => (
              <li key={o.id} className="rounded-lg border border-black/5 bg-white/80 px-3 py-2">
                <div className="flex justify-between gap-2">
                  <span className="font-medium">{o.success ? 'OK' : 'Failed'}</span>
                  <span className="text-xs text-black/45">{new Date(o.observedAt).toLocaleString()}</span>
                </div>
                <p className="text-black/60">{o.productTitle ?? o.errorMessage ?? o.sourceName}</p>
                {o.voltageDetected && (
                  <p className="text-xs text-black/45">
                    Voltage: {o.voltageDetected} · match: {String(o.voltageMatches)}
                  </p>
                )}
              </li>
            ))}
          </ul>
        </section>
        <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
          <h2 className="text-xl">Alerts</h2>
          <ul className="mt-3 max-h-80 space-y-3 overflow-auto text-sm">
            {detail.recentAlerts.length === 0 && <li className="text-black/45">No alerts yet.</li>}
            {detail.recentAlerts.map((a) => (
              <li key={a.id} className="rounded-lg border border-black/5 bg-white/80 px-3 py-2">
                <div className="flex justify-between gap-2">
                  <span className="font-medium">{a.subject}</span>
                  <span className="text-xs text-black/45">{a.status}</span>
                </div>
                <pre className="mt-1 whitespace-pre-wrap text-xs text-black/55">{a.body}</pre>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  )
}
