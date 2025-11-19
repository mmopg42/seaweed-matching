import { useState } from 'react'
import { invoke } from '@tauri-apps/api/core'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Monitor, Play, Square } from 'lucide-react'

export function Monitoring() {
  const [monitorPath, setMonitorPath] = useState('')
  const [movePath, setMovePath] = useState('')
  const [isMonitoring, setIsMonitoring] = useState(false)
  const [loading, setLoading] = useState(false)

  const handleSelectFolder = async (setter: (path: string) => void) => {
    try {
      const selected = await invoke<string | null>('select_folder')
      if (selected) {
        setter(selected)
      }
    } catch (error) {
      console.error('Failed to select folder:', error)
    }
  }

  const handleStartMonitoring = async () => {
    if (!monitorPath || !movePath) {
      alert('Please select both folders')
      return
    }

    setLoading(true)
    try {
      await invoke('start_file_watcher', {
        watchPath: monitorPath,
      })
      setIsMonitoring(true)
      alert('File monitoring started')
    } catch (error) {
      console.error('Failed to start monitoring:', error)
      alert('Failed to start monitoring')
    } finally {
      setLoading(false)
    }
  }

  const handleStopMonitoring = async () => {
    setLoading(true)
    try {
      await invoke('stop_file_watcher')
      setIsMonitoring(false)
      alert('File monitoring stopped')
    } catch (error) {
      console.error('Failed to stop monitoring:', error)
      alert('Failed to stop monitoring')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-3xl font-bold">File Monitoring</h1>
        <p className="text-muted-foreground">Monitor folders for file changes</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Monitor className="h-5 w-5" />
            Folder Monitoring Settings
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="monitor_path">Monitor Folder</Label>
            <div className="flex gap-2">
              <Input
                id="monitor_path"
                value={monitorPath}
                onChange={(e) => setMonitorPath(e.target.value)}
                placeholder="Select folder to monitor"
              />
              <Button onClick={() => handleSelectFolder(setMonitorPath)}>Browse</Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="move_path">Move Folder</Label>
            <div className="flex gap-2">
              <Input
                id="move_path"
                value={movePath}
                onChange={(e) => setMovePath(e.target.value)}
                placeholder="Select destination folder"
              />
              <Button onClick={() => handleSelectFolder(setMovePath)}>Browse</Button>
            </div>
          </div>

          <div className="flex gap-2 pt-4">
            {!isMonitoring ? (
              <Button
                onClick={handleStartMonitoring}
                disabled={loading || !monitorPath || !movePath}
                className="flex items-center gap-2"
              >
                <Play className="h-4 w-4" />
                Start Monitoring
              </Button>
            ) : (
              <Button
                onClick={handleStopMonitoring}
                disabled={loading}
                variant="destructive"
                className="flex items-center gap-2"
              >
                <Square className="h-4 w-4" />
                Stop Monitoring
              </Button>
            )}
          </div>

          {isMonitoring && (
            <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-md">
              <p className="text-sm text-green-800 dark:text-green-200 font-medium">
                File monitoring is active
              </p>
              <p className="text-xs text-green-600 dark:text-green-400 mt-1">
                Watching: {monitorPath}
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
