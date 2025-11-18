import { invoke } from '@tauri-apps/api/core'
import { FileInfo } from '@/types/file'

export async function listFiles(path: string): Promise<FileInfo[]> {
  return invoke('list_files', { path })
}

export async function deleteFiles(paths: string[]): Promise<void> {
  return invoke('delete_files', { paths })
}
