import { useEffect } from 'react'
import { useThemeStore } from '@/store/themeStore'

function applyTheme(theme: 'light' | 'dark' | 'system') {
  const root = window.document.documentElement
  root.classList.remove('light', 'dark')

  if (theme === 'system') {
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light'
    root.classList.add(systemTheme)
    console.log('[ThemeProvider] Applied system theme:', systemTheme)
  } else {
    root.classList.add(theme)
    console.log('[ThemeProvider] Applied theme:', theme)
  }
}

export default function ThemeProvider({ children }: { children: React.ReactNode }) {
  const theme = useThemeStore((state) => state.theme)

  // Apply theme on mount and when it changes
  useEffect(() => {
    applyTheme(theme)
  }, [theme])

  // Initial application on mount
  useEffect(() => {
    // Force apply theme on first render
    const storedTheme = localStorage.getItem('theme-storage')
    if (storedTheme) {
      try {
        const parsed = JSON.parse(storedTheme)
        applyTheme(parsed.state?.theme || 'system')
      } catch (e) {
        applyTheme('system')
      }
    } else {
      applyTheme('system')
    }
  }, [])

  return <>{children}</>
}
