import * as React from 'react'
import { cn } from '@/lib/utils'

export const Input = React.forwardRef<HTMLInputElement, React.ComponentProps<'input'>>(
  ({ className, type, ...props }, ref) => (
    <input
      type={type}
      className={cn(
        'flex h-10 w-full rounded-md border border-black/10 bg-white/80 px-3 py-2 text-sm shadow-sm placeholder:text-black/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-moss)]',
        className,
      )}
      ref={ref}
      {...props}
    />
  ),
)
Input.displayName = 'Input'
