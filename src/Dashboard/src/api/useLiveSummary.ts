import { useEffect, useState } from 'react'
import { LiveSummary } from '@/api/types.ts'

export const useLiveSummary = () => {
  const [summary, setSummary] = useState<LiveSummary | null>(null)
  const [error, setError] = useState<unknown>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        const res = await fetch('http://localhost:17891/api/latest', {
          cache: 'no-store',
        })
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`)
        }
        const json = (await res.json()) as LiveSummary
        if (!cancelled) {
          setSummary(json)
          setError(null)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err)
        }
      }
    }
    load()
    const id = window.setInterval(load, 5000)
    return () => {
      cancelled = true
      window.clearInterval(id)
    }
  }, [])

  return { summary, error }
}