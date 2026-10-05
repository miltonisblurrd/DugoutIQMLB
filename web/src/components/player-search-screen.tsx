"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { FormEvent, useState } from "react";
import { EmptyState, ErrorState, Skeleton } from "@/components/feedback";
import { searchPlayers } from "@/lib/api/players";
import { describeApiError } from "@/lib/errors";
import { playerName, teamLabel } from "@/lib/format";

export function PlayerSearchScreen() {
  const [draft, setDraft] = useState("");
  const [submitted, setSubmitted] = useState<string | null>(null);
  const search = useQuery({
    queryKey: ["players", "search", submitted],
    queryFn: () => searchPlayers(submitted ?? ""),
    enabled: submitted !== null,
  });

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const next = draft.trim();
    if (next === submitted) {
      void search.refetch();
      return;
    }

    setSubmitted(next);
  }

  return (
    <div>
      <header className="max-w-3xl">
        <h1 className="text-2xl font-semibold tracking-tight">Players</h1>
        <p className="mt-2 text-sm leading-6 text-[var(--muted)]">
          Search MLB players and open a DugoutIQ profile built from official baseball data.
        </p>
      </header>

      <form onSubmit={onSubmit} className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="min-w-0 flex-1">
          <label htmlFor="player-search" className="text-sm font-medium">
            Player name
          </label>
          <input
            id="player-search"
            name="q"
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            autoComplete="off"
            placeholder="Aaron Judge"
            className="field mt-1.5"
          />
        </div>
        <button type="submit" className="button-primary">
          {search.isFetching ? "Searching" : "Search"}
        </button>
      </form>

      <section aria-live="polite" aria-busy={search.isFetching} className="mt-8">
        {submitted === null ? (
          <EmptyState
            title="Search for an MLB player to begin."
            detail="Examples: Aaron Judge, Shohei Ohtani, Mookie Betts. These are search examples, not stored DugoutIQ records."
          />
        ) : null}

        {submitted !== null && search.isPending ? <SearchSkeleton /> : null}

        {search.isError ? (
          <ErrorState message={describeApiError(search.error, "search")} onRetry={() => void search.refetch()} />
        ) : null}

        {search.isSuccess && search.data.length === 0 ? (
          <EmptyState title="No players matched that name." detail="Try a longer or more specific name." />
        ) : null}

        {search.isSuccess && search.data.length > 0 ? (
          <ul className="divide-y divide-[var(--line)] overflow-hidden rounded-lg border border-[var(--line)] bg-white">
            {search.data.map((player) => {
              const team = teamLabel(player.teamAbbreviation, player.teamName);
              return (
                <li key={player.id}>
                  <article className="flex flex-col gap-4 px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-5">
                    <div className="min-w-0">
                      <h2 className="text-base font-semibold tracking-tight">
                        {playerName(player.firstName, player.lastName)}
                      </h2>
                      <p className="mt-1 text-sm text-[var(--muted)]">
                        {player.position} · Bats {player.bats} · Throws {player.throws}
                      </p>
                      <p className="mt-1 text-sm">{team ?? "Team not on file"}</p>
                    </div>
                    <Link href={`/players/${player.id}`} className="button-secondary shrink-0">
                      View profile
                    </Link>
                  </article>
                </li>
              );
            })}
          </ul>
        ) : null}
      </section>
    </div>
  );
}

function SearchSkeleton() {
  return (
    <ul className="divide-y divide-[var(--line)] overflow-hidden rounded-lg border border-[var(--line)] bg-white">
      {["one", "two", "three"].map((key) => (
        <li key={key} className="px-5 py-4">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="mt-2 h-4 w-56" />
          <Skeleton className="mt-2 h-4 w-32" />
        </li>
      ))}
    </ul>
  );
}
