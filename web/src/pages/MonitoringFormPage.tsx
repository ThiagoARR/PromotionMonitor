import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function MonitoringFormPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState({
    name: '',
    productUrl: 'https://example.com/products/demo-airfryer-110',
    modelName: 'Air Fryer Crisp 4L',
    voltageFilter: 110,
    targetPrice: '',
    maxPrice: '450',
    cep: '',
    scheduleMinutes: 60,
    sourceKind: 0,
    demoMode: true,
  })

  function set<K extends keyof typeof form>(key: K, value: (typeof form)[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const created = await api.createMonitoring({
        name: form.name,
        productUrl: form.productUrl,
        modelName: form.modelName || null,
        voltageFilter: Number(form.voltageFilter),
        targetPrice: form.targetPrice ? Number(form.targetPrice) : null,
        maxPrice: Number(form.maxPrice),
        cep: form.cep || null,
        scheduleMinutes: Number(form.scheduleMinutes),
        sourceKind: Number(form.sourceKind),
        demoMode: form.demoMode,
      })
      navigate(`/monitorings/${created.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-4xl text-[var(--color-moss-deep)]">New monitoring</h1>
        <p className="mt-2 text-black/60">
          Point at a product URL, set voltage + max price, and PromotionMonitor will collect history on schedule.
        </p>
      </div>
      <form onSubmit={onSubmit} className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6">
        <div className="space-y-2">
          <Label htmlFor="name">Name</Label>
          <Input id="name" value={form.name} onChange={(e) => set('name', e.target.value)} required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="url">Product URL</Label>
          <Input id="url" value={form.productUrl} onChange={(e) => set('productUrl', e.target.value)} required />
          <p className="text-xs text-black/45">
            Demo mock keys: demo-airfryer-110, demo-airfryer-220, demo-airfryer-bivolt, demo-notebook, demo-phone.
            Use a URL containing &quot;fail&quot; to simulate source failure.
          </p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="model">Model (optional)</Label>
            <Input id="model" value={form.modelName} onChange={(e) => set('modelName', e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="voltage">Voltage filter</Label>
            <select
              id="voltage"
              className="flex h-10 w-full rounded-md border border-black/10 bg-white/80 px-3 text-sm"
              value={form.voltageFilter}
              onChange={(e) => set('voltageFilter', Number(e.target.value))}
            >
              <option value={0}>Any</option>
              <option value={110}>110V</option>
              <option value={220}>220V</option>
              <option value={110220}>Dual / Bivolt</option>
            </select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="max">Max price (BRL)</Label>
            <Input
              id="max"
              type="number"
              step="0.01"
              min="0.01"
              value={form.maxPrice}
              onChange={(e) => set('maxPrice', e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="target">Target price (optional)</Label>
            <Input
              id="target"
              type="number"
              step="0.01"
              value={form.targetPrice}
              onChange={(e) => set('targetPrice', e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cep">CEP (optional)</Label>
            <Input id="cep" value={form.cep} onChange={(e) => set('cep', e.target.value)} placeholder="01310-100" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="schedule">Schedule (minutes)</Label>
            <Input
              id="schedule"
              type="number"
              min={1}
              max={10080}
              value={form.scheduleMinutes}
              onChange={(e) => set('scheduleMinutes', Number(e.target.value))}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="source">Source</Label>
            <select
              id="source"
              className="flex h-10 w-full rounded-md border border-black/10 bg-white/80 px-3 text-sm"
              value={form.sourceKind}
              onChange={(e) => set('sourceKind', Number(e.target.value))}
            >
              <option value={0}>Mock catalog</option>
              <option value={1}>HTML / JSON-LD</option>
            </select>
          </div>
          <div className="flex items-end gap-2 pb-2">
            <input
              id="demo"
              type="checkbox"
              checked={form.demoMode}
              onChange={(e) => set('demoMode', e.target.checked)}
            />
            <Label htmlFor="demo">Demo mode (no live retailer fetch)</Label>
          </div>
        </div>
        {error && <p className="text-sm text-[var(--color-danger)]">{error}</p>}
        <div className="flex gap-3">
          <Button type="submit" disabled={loading}>
            {loading ? 'Saving…' : 'Create & check'}
          </Button>
          <Button type="button" variant="secondary" onClick={() => navigate('/')}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  )
}
