import { useEffect } from 'react';
import { listen, UnlistenFn } from '@tauri-apps/api/event';

export interface FileChangeEvent {
  path: string;
  kind: string;
}

/**
 * Hook to listen for file system changes from Tauri backend
 * @param onFileChange - Callback function when file changes are detected
 * @param enabled - Whether the listener is active (default: true)
 */
export function useFileWatcher(
  onFileChange: (event: FileChangeEvent) => void,
  enabled = true
) {
  useEffect(() => {
    if (!enabled) return;

    let unlisten: UnlistenFn | undefined;

    // Setup listener for file-changed events from Rust backend
    const setupListener = async () => {
      unlisten = await listen<FileChangeEvent>('file-changed', (event) => {
        onFileChange(event.payload);
      });
    };

    setupListener();

    // Cleanup listener on unmount or when disabled
    return () => {
      if (unlisten) {
        unlisten();
      }
    };
  }, [onFileChange, enabled]);
}
