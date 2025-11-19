import { Badge } from '@/components/ui/badge'

export function StatusBar() {
  return (
    <div className="h-8 bg-card border-t flex items-center justify-between px-4 text-xs text-muted-foreground">
      <div className="flex items-center gap-4">
        <span>Prische Matching Codex v0.1.0</span>
        <Badge variant="outline" className="text-xs">
          Ready
        </Badge>
      </div>
      <div className="flex items-center gap-4">
        <span>Rust Backend</span>
      </div>
    </div>
  )
}
