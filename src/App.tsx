import { AppShell } from '@/components/layout/AppShell'
import { useTheme } from '@/lib/hooks/useTheme'

function App() {
  useTheme() // 테마 적용

  return <AppShell />
}

export default App
