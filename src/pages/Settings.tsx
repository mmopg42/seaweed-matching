import { useState, useEffect } from 'react'
import { invoke } from '@tauri-apps/api/core'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface AppConfig {
  nir_folder?: string
  nir2_folder?: string
  normal_folder?: string
  normal2_folder?: string
  cam1_folder?: string
  cam2_folder?: string
  cam3_folder?: string
  cam4_folder?: string
  cam5_folder?: string
  cam6_folder?: string
  move_folder?: string
  delete_folder?: string
}

export function Settings() {
  const [config, setConfig] = useState<AppConfig>({})
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    const loadConfig = async () => {
      try {
        const result = await invoke<AppConfig>('get_config')
        setConfig(result)
      } catch (error) {
        console.error('Failed to load config:', error)
      } finally {
        setLoading(false)
      }
    }

    loadConfig()
  }, [])

  const handleSave = async () => {
    setSaving(true)
    try {
      await invoke('set_config', { config })
      alert('Settings saved successfully!')
    } catch (error) {
      console.error('Failed to save config:', error)
      alert('Failed to save settings')
    } finally {
      setSaving(false)
    }
  }

  const handleFolderSelect = async (field: keyof AppConfig) => {
    try {
      const selected = await invoke<string | null>('select_folder')
      if (selected) {
        setConfig((prev) => ({ ...prev, [field]: selected }))
      }
    } catch (error) {
      console.error('Failed to select folder:', error)
    }
  }

  if (loading) {
    return (
      <div className="p-6">
        <h1 className="text-3xl font-bold mb-6">Settings</h1>
        <p>Loading settings...</p>
      </div>
    )
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Settings</h1>
        <p className="text-muted-foreground">Configure folder paths and application settings</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>NIR Folders</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="nir_folder">NIR Folder</Label>
            <div className="flex gap-2">
              <Input
                id="nir_folder"
                value={config.nir_folder || ''}
                onChange={(e) => setConfig({ ...config, nir_folder: e.target.value })}
                placeholder="Select NIR folder"
              />
              <Button onClick={() => handleFolderSelect('nir_folder')}>Browse</Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="nir2_folder">NIR2 Folder</Label>
            <div className="flex gap-2">
              <Input
                id="nir2_folder"
                value={config.nir2_folder || ''}
                onChange={(e) => setConfig({ ...config, nir2_folder: e.target.value })}
                placeholder="Select NIR2 folder"
              />
              <Button onClick={() => handleFolderSelect('nir2_folder')}>Browse</Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Normal Folders</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="normal_folder">Normal Folder</Label>
            <div className="flex gap-2">
              <Input
                id="normal_folder"
                value={config.normal_folder || ''}
                onChange={(e) => setConfig({ ...config, normal_folder: e.target.value })}
                placeholder="Select Normal folder"
              />
              <Button onClick={() => handleFolderSelect('normal_folder')}>Browse</Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="normal2_folder">Normal2 Folder</Label>
            <div className="flex gap-2">
              <Input
                id="normal2_folder"
                value={config.normal2_folder || ''}
                onChange={(e) => setConfig({ ...config, normal2_folder: e.target.value })}
                placeholder="Select Normal2 folder"
              />
              <Button onClick={() => handleFolderSelect('normal2_folder')}>Browse</Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Camera Folders</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {[1, 2, 3, 4, 5, 6].map((num) => (
            <div key={num} className="space-y-2">
              <Label htmlFor={`cam${num}_folder`}>Camera {num} Folder</Label>
              <div className="flex gap-2">
                <Input
                  id={`cam${num}_folder`}
                  value={config[`cam${num}_folder` as keyof AppConfig] as string || ''}
                  onChange={(e) =>
                    setConfig({ ...config, [`cam${num}_folder`]: e.target.value })
                  }
                  placeholder={`Select Camera ${num} folder`}
                />
                <Button onClick={() => handleFolderSelect(`cam${num}_folder` as keyof AppConfig)}>
                  Browse
                </Button>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Action Folders</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="move_folder">Move Folder</Label>
            <div className="flex gap-2">
              <Input
                id="move_folder"
                value={config.move_folder || ''}
                onChange={(e) => setConfig({ ...config, move_folder: e.target.value })}
                placeholder="Folder for files to move"
              />
              <Button onClick={() => handleFolderSelect('move_folder')}>Browse</Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="delete_folder">Delete Folder</Label>
            <div className="flex gap-2">
              <Input
                id="delete_folder"
                value={config.delete_folder || ''}
                onChange={(e) => setConfig({ ...config, delete_folder: e.target.value })}
                placeholder="Folder for files to delete"
              />
              <Button onClick={() => handleFolderSelect('delete_folder')}>Browse</Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save Settings'}
        </Button>
      </div>
    </div>
  )
}
