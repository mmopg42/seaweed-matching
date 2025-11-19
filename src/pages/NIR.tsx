import { useState } from 'react'
import { invoke } from '@tauri-apps/api/core'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Activity, FileText, Trash2 } from 'lucide-react'

interface SpectrumAnalysisResult {
  file_path: string
  has_valid_regions: boolean
  regions: Array<{
    x_start: number
    x_end: number
    y_range: number
  }>
  action: 'Move' | 'Delete'
}

export function NIR() {
  const [txtFilePath, setTxtFilePath] = useState('')
  const [moveFolder, setMoveFolder] = useState('')
  const [deleteFolder, setDeleteFolder] = useState('')
  const [result, setResult] = useState<SpectrumAnalysisResult | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSelectFile = async () => {
    try {
      const selected = await invoke<string | null>('select_file', {
        filters: [{ name: 'Text Files', extensions: ['txt'] }],
      })
      if (selected) {
        setTxtFilePath(selected)
      }
    } catch (error) {
      console.error('Failed to select file:', error)
    }
  }

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

  const handleAnalyze = async () => {
    if (!txtFilePath) {
      alert('Please select a NIR spectrum file')
      return
    }

    setLoading(true)
    try {
      const analysisResult = await invoke<SpectrumAnalysisResult>('analyze_nir_spectrum', {
        filePath: txtFilePath,
      })
      setResult(analysisResult)
    } catch (error) {
      console.error('Failed to analyze spectrum:', error)
      alert(`Failed to analyze: ${error}`)
    } finally {
      setLoading(false)
    }
  }

  const handleProcess = async () => {
    if (!txtFilePath || !moveFolder) {
      alert('Please select file and move folder')
      return
    }

    setLoading(true)
    try {
      const processResult = await invoke<SpectrumAnalysisResult>('process_nir_file', {
        txtFilePath,
        moveFolder,
      })
      setResult(processResult)
      alert(`File processed: ${processResult.action}`)
    } catch (error) {
      console.error('Failed to process file:', error)
      alert(`Failed to process: ${error}`)
    } finally {
      setLoading(false)
    }
  }

  const handlePrune = async () => {
    if (!deleteFolder) {
      alert('Please select delete folder')
      return
    }

    setLoading(true)
    try {
      const count = await invoke<number>('prune_nir_files', {
        targetGroups: [],
        keepCount: 10,
        deleteFolder,
      })
      alert(`Pruned ${count} NIR file bundles`)
    } catch (error) {
      console.error('Failed to prune files:', error)
      alert(`Failed to prune: ${error}`)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-3xl font-bold">NIR Spectrum Analysis</h1>
        <p className="text-muted-foreground">Analyze and process NIR spectrum files</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            File Selection
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="txt_file">NIR Spectrum File (.txt)</Label>
            <div className="flex gap-2">
              <Input
                id="txt_file"
                value={txtFilePath}
                onChange={(e) => setTxtFilePath(e.target.value)}
                placeholder="Select .txt file"
              />
              <Button onClick={handleSelectFile}>Browse</Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="move_folder">Move Folder</Label>
            <div className="flex gap-2">
              <Input
                id="move_folder"
                value={moveFolder}
                onChange={(e) => setMoveFolder(e.target.value)}
                placeholder="Select move destination"
              />
              <Button onClick={() => handleSelectFolder(setMoveFolder)}>Browse</Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="delete_folder">Delete Folder</Label>
            <div className="flex gap-2">
              <Input
                id="delete_folder"
                value={deleteFolder}
                onChange={(e) => setDeleteFolder(e.target.value)}
                placeholder="Select delete destination"
              />
              <Button onClick={() => handleSelectFolder(setDeleteFolder)}>Browse</Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            Actions
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex gap-2">
            <Button onClick={handleAnalyze} disabled={loading || !txtFilePath}>
              Analyze Spectrum
            </Button>
            <Button onClick={handleProcess} disabled={loading || !txtFilePath || !moveFolder}>
              Process File
            </Button>
            <Button
              onClick={handlePrune}
              disabled={loading || !deleteFolder}
              variant="destructive"
              className="flex items-center gap-2"
            >
              <Trash2 className="h-4 w-4" />
              Prune Old Files
            </Button>
          </div>

          {result && (
            <div className="mt-4 p-4 bg-muted rounded-md">
              <h3 className="font-semibold mb-2">Analysis Result</h3>
              <div className="space-y-1 text-sm">
                <p>
                  <span className="font-medium">File:</span> {result.file_path}
                </p>
                <p>
                  <span className="font-medium">Valid Regions:</span>{' '}
                  {result.has_valid_regions ? 'Yes' : 'No'}
                </p>
                <p>
                  <span className="font-medium">Action:</span>{' '}
                  <span
                    className={
                      result.action === 'Move' ? 'text-green-600' : 'text-red-600'
                    }
                  >
                    {result.action}
                  </span>
                </p>
                {result.regions.length > 0 && (
                  <div className="mt-2">
                    <p className="font-medium">Regions Found:</p>
                    <ul className="list-disc list-inside">
                      {result.regions.map((region, index) => (
                        <li key={index}>
                          X: {region.x_start.toFixed(2)} - {region.x_end.toFixed(2)}, Y
                          Range: {region.y_range.toFixed(4)}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
