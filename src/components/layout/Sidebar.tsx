import { Home, Monitor, Activity, Settings } from 'lucide-react'
import { cn } from '@/lib/utils/cn'
import { Button } from '@/components/ui/button'

interface SidebarProps {
  currentPage: string
  onPageChange: (page: string) => void
}

const menuItems = [
  { id: 'dashboard', icon: Home, label: 'Dashboard' },
  { id: 'monitoring', icon: Monitor, label: 'Monitoring' },
  { id: 'nir', icon: Activity, label: 'NIR' },
  { id: 'settings', icon: Settings, label: 'Settings' },
]

export function Sidebar({ currentPage, onPageChange }: SidebarProps) {
  return (
    <div className="w-64 bg-card border-r flex flex-col">
      <nav className="flex-1 p-4 space-y-2">
        {menuItems.map((item) => (
          <Button
            key={item.id}
            variant={currentPage === item.id ? 'secondary' : 'ghost'}
            className={cn(
              "w-full justify-start",
              currentPage === item.id && "bg-prische-600 text-white hover:bg-prische-700"
            )}
            onClick={() => onPageChange(item.id)}
          >
            <item.icon className="mr-2 h-4 w-4" />
            {item.label}
          </Button>
        ))}
      </nav>
    </div>
  )
}
