import { useCallback, useMemo, useRef, DependencyList } from 'react';

/**
 * Debounced callback hook
 * Delays invoking the callback until after delay milliseconds have elapsed
 */
export function useDebouncedCallback<T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): (...args: Parameters<T>) => void {
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  return useCallback(
    (...args: Parameters<T>) => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      timeoutRef.current = setTimeout(() => {
        callback(...args);
      }, delay);
    },
    [callback, delay]
  );
}

/**
 * Throttled callback hook
 * Only invokes the callback at most once per every wait milliseconds
 */
export function useThrottledCallback<T extends (...args: any[]) => any>(
  callback: T,
  wait: number
): (...args: Parameters<T>) => void {
  const lastCallRef = useRef<number>(0);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  return useCallback(
    (...args: Parameters<T>) => {
      const now = Date.now();
      const timeSinceLastCall = now - lastCallRef.current;

      if (timeSinceLastCall >= wait) {
        lastCallRef.current = now;
        callback(...args);
      } else {
        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
        }

        timeoutRef.current = setTimeout(() => {
          lastCallRef.current = Date.now();
          callback(...args);
        }, wait - timeSinceLastCall);
      }
    },
    [callback, wait]
  );
}

/**
 * Memoized expensive computation hook
 * Only recomputes when dependencies change
 */
export function useMemoizedValue<T>(
  factory: () => T,
  deps: DependencyList
): T {
  return useMemo(factory, deps);
}

/**
 * Stable callback hook that doesn't change reference
 * Useful for callbacks passed to memoized components
 */
export function useStableCallback<T extends (...args: any[]) => any>(
  callback: T
): T {
  const callbackRef = useRef(callback);

  // Update ref on each render so we always have the latest callback
  callbackRef.current = callback;

  // Return a stable callback that calls the current ref
  return useCallback(
    ((...args) => callbackRef.current(...args)) as T,
    []
  );
}

/**
 * Deep comparison memo hook
 * Uses JSON.stringify for deep comparison (use with caution for large objects)
 */
export function useDeepMemo<T>(value: T): T {
  const ref = useRef<T>(value);
  const signalRef = useRef<number>(0);

  const currentStringified = JSON.stringify(value);
  const refStringified = JSON.stringify(ref.current);

  if (currentStringified !== refStringified) {
    ref.current = value;
    signalRef.current += 1;
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  return useMemo(() => ref.current, [signalRef.current]);
}

/**
 * Memoized array hook
 * Only updates when array items change (shallow comparison)
 */
export function useMemoizedArray<T>(array: T[]): T[] {
  return useMemo(
    () => array,
    // eslint-disable-next-line react-hooks/exhaustive-deps
    array
  );
}

/**
 * Previous value hook
 * Returns the previous value of a state or prop
 */
export function usePrevious<T>(value: T): T | undefined {
  const ref = useRef<T | undefined>(undefined);

  useMemo(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}
