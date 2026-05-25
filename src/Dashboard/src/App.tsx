import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Layout from './components/layout/Layout'
import ThemeProvider from './components/providers/ThemeProvider'

// Pages
import Dashboard from './pages/Dashboard'

export default function App() {
  return (
    <ThemeProvider>
        <BrowserRouter basename={import.meta.env.BASE_URL}>
          <Routes>

            {/* Main App Routes (with layout) */}
            <Route path="/" element={<Layout />}>
              <Route index element={<Dashboard />} />

            </Route>
          </Routes>
        </BrowserRouter>
    </ThemeProvider>
  )
}
