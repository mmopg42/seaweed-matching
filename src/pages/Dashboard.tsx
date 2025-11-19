import { useState, useEffect } from 'react'
import { invoke } from '@tauri-apps/api/core'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Monitor, Files, Activity } from 'lucide-react'

interface FolderStats {
  nir_count: number
  nir2_count: number
  normal_count: number
  normal2_count: number
  cam1_count: number
  cam2_count: number
  cam3_count: number
  cam4_count: number
  cam5_count: number
  cam6_count: number
}

export function Dashboard() {
  const [stats, setStats] = useState<FolderStats | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const result = await invoke<FolderStats>('get_folder_stats', {
          nirFolder: null,
          nir2Folder: null,
          normalFolder: null,
          normal2Folder: null,
          cam1Folder: null,
          cam2Folder: null,
          cam3Folder: null,
          cam4Folder: null,
          cam5Folder: null,
          cam6Folder: null,
        })
        setStats(result)
      } catch (error) {
        console.error('Failed to fetch folder stats:', error)
      } finally {
        setLoading(false)
      }
    }

    fetchStats()
    // Refresh stats every 5 seconds
    const interval = setInterval(fetchStats, 5000)
    return () => clearInterval(interval)
  }, [])

  const totalFiles = stats
    ? stats.nir_count +
      stats.nir2_count +
      stats.normal_count +
      stats.normal2_count +
      stats.cam1_count +
      stats.cam2_count +
      stats.cam3_count +
      stats.cam4_count +
      stats.cam5_count +
      stats.cam6_count
    : 0

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <p className="text-muted-foreground">
          Welcome to Prische Matching Codex
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">
              NIR Files
            </CardTitle>
            <Monitor className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? '...' : stats ? stats.nir_count + stats.nir2_count : 0}
            </div>
            <p className="text-xs text-muted-foreground">
              NIR spectrum files
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">
              Total Files
            </CardTitle>
            <Files className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? '...' : totalFiles}
            </div>
            <p className="text-xs text-muted-foreground">
              Files across all folders
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">
              Status
            </CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? 'Loading...' : 'Ready'}
            </div>
            <p className="text-xs text-muted-foreground">
              System operational
            </p>
          </CardContent>
        </Card>
      </div>

      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium">NIR</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-xl font-bold">{stats.nir_count}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium">NIR2</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-xl font-bold">{stats.nir2_count}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium">Normal</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-xl font-bold">{stats.normal_count}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium">Normal2</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-xl font-bold">{stats.normal2_count}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium">Cameras</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-xl font-bold">
                {stats.cam1_count +
                  stats.cam2_count +
                  stats.cam3_count +
                  stats.cam4_count +
                  stats.cam5_count +
                  stats.cam6_count}
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
