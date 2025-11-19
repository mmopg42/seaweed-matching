import { useState, useEffect, ImgHTMLAttributes } from 'react';
import { useIntersectionObserver } from '@/lib/hooks/useIntersectionObserver';
import { cn } from '@/lib/utils';
import { Skeleton } from './skeleton';

interface LazyImageProps extends Omit<ImgHTMLAttributes<HTMLImageElement>, 'src'> {
  src: string;
  alt: string;
  placeholderSrc?: string;
  rootMargin?: string;
  threshold?: number;
  onLoad?: () => void;
  onError?: () => void;
  skeletonClassName?: string;
}

/**
 * Lazy loading image component with Intersection Observer
 * Only loads image when it enters the viewport
 */
export function LazyImage({
  src,
  alt,
  placeholderSrc,
  rootMargin = '50px',
  threshold = 0.01,
  className,
  skeletonClassName,
  onLoad,
  onError,
  ...props
}: LazyImageProps) {
  const [imageSrc, setImageSrc] = useState<string | undefined>(placeholderSrc);
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);

  const { ref, isIntersecting } = useIntersectionObserver({
    threshold,
    rootMargin,
    freezeOnceVisible: true,
  });

  useEffect(() => {
    if (isIntersecting && !imageLoaded && !imageError) {
      const img = new Image();

      img.onload = () => {
        setImageSrc(src);
        setImageLoaded(true);
        onLoad?.();
      };

      img.onerror = () => {
        setImageError(true);
        onError?.();
      };

      img.src = src;
    }
  }, [isIntersecting, src, imageLoaded, imageError, onLoad, onError]);

  if (imageError) {
    return (
      <div
        ref={ref as React.RefObject<HTMLDivElement>}
        className={cn(
          'flex items-center justify-center bg-muted text-muted-foreground',
          className
        )}
      >
        <span className="text-sm">Failed to load</span>
      </div>
    );
  }

  if (!imageLoaded || !imageSrc) {
    return (
      <div ref={ref as React.RefObject<HTMLDivElement>} className={cn('relative', className)}>
        <Skeleton className={cn('absolute inset-0', skeletonClassName)} />
        {placeholderSrc && (
          <img
            src={placeholderSrc}
            alt={alt}
            className={cn('blur-sm', className)}
            {...props}
          />
        )}
      </div>
    );
  }

  return (
    <img
      ref={ref as React.RefObject<HTMLImageElement>}
      src={imageSrc}
      alt={alt}
      className={cn(
        'transition-opacity duration-300',
        imageLoaded ? 'opacity-100' : 'opacity-0',
        className
      )}
      {...props}
    />
  );
}

/**
 * Lazy background image component
 */
interface LazyBackgroundProps {
  src: string;
  children?: React.ReactNode;
  className?: string;
  rootMargin?: string;
}

export function LazyBackground({
  src,
  children,
  className,
  rootMargin = '50px',
}: LazyBackgroundProps) {
  const [loaded, setLoaded] = useState(false);
  const { ref, isIntersecting } = useIntersectionObserver({
    threshold: 0.01,
    rootMargin,
    freezeOnceVisible: true,
  });

  useEffect(() => {
    if (isIntersecting && !loaded) {
      const img = new Image();
      img.onload = () => setLoaded(true);
      img.src = src;
    }
  }, [isIntersecting, src, loaded]);

  return (
    <div
      ref={ref as React.RefObject<HTMLDivElement>}
      className={cn('transition-all duration-500', className)}
      style={{
        backgroundImage: loaded ? `url(${src})` : 'none',
        backgroundColor: loaded ? 'transparent' : 'hsl(var(--muted))',
      }}
    >
      {children}
    </div>
  );
}
