import { AppShell } from '@/components/layout/AppShell'
import { Dashboard } from '@/pages/Dashboard'
import { useTheme } from '@/lib/hooks/useTheme'

function App() {
  useTheme() // 테마 적용

  return (
    <AppShell>
      <Dashboard />
    </AppShell>
  )
}

export default App
