export { useTheme } from './useTheme';
export { useFiles } from './useFiles';
export { useFileWatcher } from './useFileWatcher';
export type { FileChangeEvent } from './useFileWatcher';

// Performance optimization hooks
export { useIntersectionObserver } from './useIntersectionObserver';
export {
  useDebouncedCallback,
  useThrottledCallback,
  useMemoizedValue,
  useStableCallback,
  useDeepMemo,
  useMemoizedArray,
  usePrevious,
} from './useMemoization';
