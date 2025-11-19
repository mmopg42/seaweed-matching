import { useState } from 'react'
import { TitleBar } from './TitleBar'
import { Sidebar } from './Sidebar'
import { StatusBar } from './StatusBar'
import { Dashboard } from '@/pages/Dashboard'
import { Monitoring } from '@/pages/Monitoring'
import { NIR } from '@/pages/NIR'
import { Settings } from '@/pages/Settings'

export function AppShell() {
  const [currentPage, setCurrentPage] = useState('dashboard')

  const renderPage = () => {
    switch (currentPage) {
      case 'dashboard':
        return <Dashboard />
      case 'monitoring':
        return <Monitoring />
      case 'nir':
        return <NIR />
      case 'settings':
        return <Settings />
      default:
        return <Dashboard />
    }
  }

  return (
    <div className="h-screen flex flex-col overflow-hidden">
      <TitleBar />
      <div className="flex-1 flex overflow-hidden">
        <Sidebar currentPage={currentPage} onPageChange={setCurrentPage} />
        <main className="flex-1 overflow-auto">
          {renderPage()}
        </main>
      </div>
      <StatusBar />
    </div>
  )
}
