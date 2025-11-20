import { useVirtualizer } from '@tanstack/react-virtual';
import { useRef, ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface VirtualListProps<T> {
  items: T[];
  height: number;
  itemHeight: number;
  renderItem: (item: T, index: number) => ReactNode;
  className?: string;
  overscan?: number;
}

/**
 * Virtual scrolling list component for large datasets
 * Uses @tanstack/react-virtual for efficient rendering
 */
export function VirtualList<T>({
  items,
  height,
  itemHeight,
  renderItem,
  className,
  overscan = 5,
}: VirtualListProps<T>) {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: items.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => itemHeight,
    overscan,
  });

  const virtualItems = virtualizer.getVirtualItems();

  return (
    <div
      ref={parentRef}
      className={cn('overflow-auto', className)}
      style={{ height: `${height}px` }}
    >
      <div
        style={{
          height: `${virtualizer.getTotalSize()}px`,
          width: '100%',
          position: 'relative',
        }}
      >
        {virtualItems.map((virtualItem: any) => (
          <div
            key={virtualItem.key}
            data-index={virtualItem.index}
            ref={virtualizer.measureElement}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              transform: `translateY(${virtualItem.start}px)`,
            }}
          >
            {renderItem(items[virtualItem.index], virtualItem.index)}
          </div>
        ))}
      </div>
    </div>
  );
}

/**
 * Virtual grid component for image galleries
 */
interface VirtualGridProps<T> {
  items: T[];
  height: number;
  columnCount: number;
  rowHeight: number;
  renderItem: (item: T, index: number) => ReactNode;
  className?: string;
}

export function VirtualGrid<T>({
  items,
  height,
  columnCount,
  rowHeight,
  renderItem,
  className,
}: VirtualGridProps<T>) {
  const parentRef = useRef<HTMLDivElement>(null);
  const rowCount = Math.ceil(items.length / columnCount);

  const virtualizer = useVirtualizer({
    count: rowCount,
    getScrollElement: () => parentRef.current,
    estimateSize: () => rowHeight,
    overscan: 2,
  });

  const virtualRows = virtualizer.getVirtualItems();

  return (
    <div
      ref={parentRef}
      className={cn('overflow-auto', className)}
      style={{ height: `${height}px` }}
    >
      <div
        style={{
          height: `${virtualizer.getTotalSize()}px`,
          width: '100%',
          position: 'relative',
        }}
      >
        {virtualRows.map((virtualRow: any) => {
          const startIndex = virtualRow.index * columnCount;
          const rowItems = items.slice(startIndex, startIndex + columnCount);

          return (
            <div
              key={virtualRow.key}
              data-index={virtualRow.index}
              ref={virtualizer.measureElement}
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                transform: `translateY(${virtualRow.start}px)`,
              }}
            >
              <div className="grid" style={{ gridTemplateColumns: `repeat(${columnCount}, 1fr)` }}>
                {rowItems.map((item, colIndex) =>
                  renderItem(item, startIndex + colIndex)
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
