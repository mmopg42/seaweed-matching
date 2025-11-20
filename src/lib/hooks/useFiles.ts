import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { listFiles, deleteFiles } from '@/lib/tauri/commands'
import { toast } from 'sonner'

export function useFiles(path: string) {
  return useQuery({
    queryKey: ['files', path],
    queryFn: () => listFiles(path),
    enabled: !!path,
  })
}

export function useDeleteFiles() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteFiles,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['files'] })
      toast.success('Files deleted successfully')
    },
    onError: (error: unknown) => {
      toast.error(`Failed to delete files: ${error}`)
    },
  })
}
