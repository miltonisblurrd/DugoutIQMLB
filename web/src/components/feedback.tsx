export function EmptyState({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="rounded-lg border border-dashed border-[var(--line)] bg-white px-6 py-10">
      <p className="text-base font-medium text-[var(--ink)]">{title}</p>
      <p className="mt-2 max-w-xl text-sm leading-6 text-[var(--muted)]">{detail}</p>
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div role="alert" className="rounded-lg border border-[#e4c7c7] bg-[#fdf7f7] px-6 py-8">
      <p className="text-base font-medium text-[#6e2424]">{message}</p>
      {onRetry ? (
        <button type="button" onClick={onRetry} className="button-secondary mt-4">
          Try again
        </button>
      ) : null}
    </div>
  );
}

export function Skeleton({ className = "h-4 w-full" }: { className?: string }) {
  return <span className={`block animate-pulse rounded bg-[#e6ebf0] ${className}`} />;
}
