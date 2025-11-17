import { useState } from 'react'
import { Minus, Square, X } from 'lucide-react'
import { appWindow } from '@tauri-apps/api/window'
import { Button } from '@/components/ui/button'

export function TitleBar() {
  const [isMaximized, setIsMaximized] = useState(false)

  const minimize = () => appWindow.minimize()
  const toggleMaximize = async () => {
    await appWindow.toggleMaximize()
    setIsMaximized(!isMaximized)
  }
  const close = () => appWindow.close()

  return (
    <div
      data-tauri-drag-region
      className="h-12 bg-background border-b flex items-center justify-between px-4"
    >
      <div className="flex items-center gap-2" data-tauri-drag-region>
        <img src="/prische_logo.png" alt="Prische" className="h-6" />
        <span className="font-semibold text-sm">Matching Codex</span>
      </div>

      <div className="flex items-center">
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          onClick={minimize}
        >
          <Minus className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          onClick={toggleMaximize}
        >
          <Square className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 hover:bg-destructive hover:text-destructive-foreground"
          onClick={close}
        >
          <X className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}
