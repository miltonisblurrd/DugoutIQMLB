"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, type ReactNode } from "react";

const planned = ["Scouting", "Watchlists", "Comparisons"];

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [menuPath, setMenuPath] = useState<string | null>(null);
  const menuOpen = menuPath === pathname;
  const playersActive = pathname.startsWith("/players");

  return (
    <div className="min-h-full bg-[var(--canvas)] text-[var(--ink)]">
      <a href="#workspace" className="skip-link">
        Skip to workspace
      </a>
      <header className="sticky top-0 z-40 flex h-14 items-center justify-between border-b border-white/10 bg-[var(--sidebar)] px-4 text-white lg:hidden">
        <Link href="/players" className="text-sm font-semibold tracking-tight">
          DugoutIQ
        </Link>
        <button
          type="button"
          className="button-ghost"
          aria-expanded={menuOpen}
          aria-controls="app-nav"
          onClick={() => setMenuPath(menuOpen ? null : pathname)}
        >
          {menuOpen ? "Close" : "Menu"}
        </button>
      </header>
      {menuOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-[#0c1218]/50 lg:hidden"
          aria-label="Close navigation"
          onClick={() => setMenuPath(null)}
        />
      ) : null}
      <div className="lg:grid lg:min-h-screen lg:grid-cols-[15.5rem_minmax(0,1fr)]">
        <aside
          id="app-nav"
          className={`${menuOpen ? "fixed inset-y-0 left-0 z-40 flex w-64" : "hidden"} lg:sticky lg:top-0 lg:flex lg:h-screen lg:w-auto`}
        >
          <div className="flex h-full w-full flex-col bg-[var(--sidebar)] px-4 py-5 text-[var(--sidebar-text)]">
            <Link href="/players" className="block rounded-md px-2 py-1">
              <span className="block text-[11px] font-semibold uppercase tracking-[0.16em] text-[var(--sidebar-muted)]">
                DugoutIQ
              </span>
              <span className="mt-1 block text-sm leading-5 text-white">Baseball Operations intelligence</span>
            </Link>
            <p className="mt-3 px-2 text-[11px] font-medium uppercase tracking-[0.14em] text-[var(--sidebar-muted)]">
              Development build
            </p>
            <nav aria-label="Primary" className="mt-8">
              <ul className="space-y-1">
                <li>
                  <Link
                    href="/players"
                    aria-current={playersActive ? "page" : undefined}
                    className={`flex items-center rounded-md px-2 py-2 text-sm ${
                      playersActive
                        ? "bg-white/10 font-semibold text-white"
                        : "text-[var(--sidebar-text)] hover:bg-white/5"
                    }`}
                  >
                    Players
                  </Link>
                </li>
                {planned.map((item) => (
                  <li key={item}>
                    <span
                      aria-disabled="true"
                      className="flex items-center justify-between rounded-md px-2 py-2 text-sm text-[var(--sidebar-muted)]"
                    >
                      {item}
                      <span className="text-[10px] font-semibold uppercase tracking-[0.12em]">Planned</span>
                    </span>
                  </li>
                ))}
              </ul>
            </nav>
          </div>
        </aside>
        <main id="workspace" className="min-w-0 px-4 py-6 sm:px-8 sm:py-8">
          <div className="mx-auto w-full max-w-6xl">{children}</div>
        </main>
      </div>
    </div>
  );
}
