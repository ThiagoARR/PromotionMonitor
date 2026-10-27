import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api, type CashbackRule, type CouponRule } from '@/lib/api'
import { formatBrl } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function RulesPage() {
  const [coupons, setCoupons] = useState<CouponRule[]>([])
  const [cashback, setCashback] = useState<CashbackRule[]>([])
  const [error, setError] = useState<string | null>(null)
  const [shippingMsg, setShippingMsg] = useState<string | null>(null)
  const [couponForm, setCouponForm] = useState({ code: '', value: '30', kind: 0, confirmed: true })
  const [cashForm, setCashForm] = useState({ name: '', value: '5', kind: 1 })
  const [cep, setCep] = useState('01310-100')
  const [listed, setListed] = useState('400')

  async function reload() {
    const [c, cb] = await Promise.all([api.listCoupons(), api.listCashback()])
    setCoupons(c)
    setCashback(cb)
  }

  useEffect(() => {
    reload().catch((err) => setError(err instanceof Error ? err.message : 'Failed to load'))
  }, [])

  async function addCoupon(e: FormEvent) {
    e.preventDefault()
    await api.createCoupon({
      code: couponForm.code,
      kind: Number(couponForm.kind),
      value: Number(couponForm.value),
      isConfirmed: couponForm.confirmed,
      isActive: true,
    })
    setCouponForm({ code: '', value: '30', kind: 0, confirmed: true })
    await reload()
  }

  async function addCashback(e: FormEvent) {
    e.preventDefault()
    await api.createCashback({
      name: cashForm.name,
      kind: Number(cashForm.kind),
      value: Number(cashForm.value),
      isActive: true,
    })
    setCashForm({ name: '', value: '5', kind: 1 })
    await reload()
  }

  return (
    <div className="space-y-10">
      <div>
        <h1 className="text-4xl text-[var(--color-moss-deep)]">Coupons & cashback</h1>
        <p className="mt-2 max-w-2xl text-black/60">
          Confirmed coupons reduce pay-at-checkout. Cashback stays informational and never lowers checkout.
        </p>
      </div>
      {error && <p className="text-[var(--color-danger)]">{error}</p>}

      <section className="grid gap-8 lg:grid-cols-2">
        <div className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-5">
          <h2 className="text-2xl">Coupons</h2>
          <form onSubmit={addCoupon} className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1">
              <Label>Code</Label>
              <Input value={couponForm.code} onChange={(e) => setCouponForm({ ...couponForm, code: e.target.value })} required />
            </div>
            <div className="space-y-1">
              <Label>Value</Label>
              <Input type="number" step="0.01" value={couponForm.value} onChange={(e) => setCouponForm({ ...couponForm, value: e.target.value })} required />
            </div>
            <div className="space-y-1">
              <Label>Kind</Label>
              <select
                className="flex h-10 w-full rounded-md border border-black/10 bg-white/80 px-3 text-sm"
                value={couponForm.kind}
                onChange={(e) => setCouponForm({ ...couponForm, kind: Number(e.target.value) })}
              >
                <option value={0}>Fixed (R$)</option>
                <option value={1}>Percent</option>
              </select>
            </div>
            <div className="flex items-end gap-2 pb-2">
              <input
                id="confirmed"
                type="checkbox"
                checked={couponForm.confirmed}
                onChange={(e) => setCouponForm({ ...couponForm, confirmed: e.target.checked })}
              />
              <Label htmlFor="confirmed">Confirmed at checkout</Label>
            </div>
            <Button type="submit" className="sm:col-span-2">
              Add coupon
            </Button>
          </form>
          <ul className="space-y-2 text-sm">
            {coupons.map((c) => (
              <li key={c.id} className="flex items-center justify-between gap-2 rounded-lg border border-black/5 px-3 py-2">
                <span>
                  <strong>{c.code}</strong> · {c.kind === 1 ? `${c.value}%` : formatBrl(c.value)} ·{' '}
                  {c.isConfirmed ? 'confirmed' : 'estimate only'}
                </span>
                <Button size="sm" variant="danger" onClick={() => api.deleteCoupon(c.id).then(reload)}>
                  Delete
                </Button>
              </li>
            ))}
            {coupons.length === 0 && <li className="text-black/45">No coupons yet.</li>}
          </ul>
        </div>

        <div className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-5">
          <h2 className="text-2xl">Cashback programs</h2>
          <form onSubmit={addCashback} className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1 sm:col-span-2">
              <Label>Name</Label>
              <Input value={cashForm.name} onChange={(e) => setCashForm({ ...cashForm, name: e.target.value })} required />
            </div>
            <div className="space-y-1">
              <Label>Value</Label>
              <Input type="number" step="0.01" value={cashForm.value} onChange={(e) => setCashForm({ ...cashForm, value: e.target.value })} required />
            </div>
            <div className="space-y-1">
              <Label>Kind</Label>
              <select
                className="flex h-10 w-full rounded-md border border-black/10 bg-white/80 px-3 text-sm"
                value={cashForm.kind}
                onChange={(e) => setCashForm({ ...cashForm, kind: Number(e.target.value) })}
              >
                <option value={0}>Fixed (R$)</option>
                <option value={1}>Percent</option>
              </select>
            </div>
            <Button type="submit" className="sm:col-span-2">
              Add cashback rule
            </Button>
          </form>
          <ul className="space-y-2 text-sm">
            {cashback.map((c) => (
              <li key={c.id} className="flex items-center justify-between gap-2 rounded-lg border border-black/5 px-3 py-2">
                <span>
                  <strong>{c.name}</strong> · {c.kind === 1 ? `${c.value}%` : formatBrl(c.value)}
                </span>
                <Button size="sm" variant="danger" onClick={() => api.deleteCashback(c.id).then(reload)}>
                  Delete
                </Button>
              </li>
            ))}
            {cashback.length === 0 && <li className="text-black/45">No cashback rules yet.</li>}
          </ul>
        </div>
      </section>

      <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h2 className="text-2xl">CEP shipping quote</h2>
        <p className="mt-1 text-sm text-black/55">Uses simulated provider when Shipping API credentials are empty.</p>
        <div className="mt-4 flex flex-wrap items-end gap-3">
          <div className="space-y-1">
            <Label>CEP</Label>
            <Input value={cep} onChange={(e) => setCep(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Listed price</Label>
            <Input type="number" value={listed} onChange={(e) => setListed(e.target.value)} />
          </div>
          <Button
            type="button"
            onClick={async () => {
              try {
                const q = await api.quoteShipping(cep, Number(listed))
                setShippingMsg(
                  q.success
                    ? `${formatBrl(q.amount)} via ${q.provider}${q.simulated ? ' (simulated)' : ''}`
                    : `Failed: ${q.errorMessage ?? 'unknown'}`,
                )
              } catch (err) {
                setShippingMsg(err instanceof Error ? err.message : 'Quote failed')
              }
            }}
          >
            Quote
          </Button>
        </div>
        {shippingMsg && <p className="mt-3 text-sm">{shippingMsg}</p>}
      </section>
    </div>
  )
}
