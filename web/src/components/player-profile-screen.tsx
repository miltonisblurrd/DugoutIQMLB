"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { EmptyState, ErrorState, Skeleton } from "@/components/feedback";
import { getPlayer } from "@/lib/api/players";
import { describeApiError } from "@/lib/errors";
import { formatBirthDate, formatCount, formatRate, formatTimestamp, playerName, teamLabel } from "@/lib/format";
import type { PlayerProfile, SeasonHitting } from "@/lib/api/schemas";

export function PlayerProfileScreen({ id }: { id: string }) {
  const profile = useQuery({
    queryKey: ["players", "profile", id],
    queryFn: () => getPlayer(id),
  });

  useEffect(() => {
    if (!profile.data) {
      return;
    }

    document.title = `${playerName(profile.data.firstName, profile.data.lastName)} · DugoutIQ`;
  }, [profile.data]);

  return (
    <div>
      <p className="text-sm">
        <Link href="/players" className="font-medium text-[var(--accent)] hover:underline">
          Players
        </Link>
      </p>

      {profile.isPending ? <ProfileSkeleton /> : null}

      {profile.isError && !profile.data ? (
        <div className="mt-6">
          <ErrorState message={describeApiError(profile.error, "profile")} onRetry={() => void profile.refetch()} />
        </div>
      ) : null}

      {profile.data ? (
        <ProfileBody
          player={profile.data}
          refreshing={profile.isFetching}
          onRefresh={() => void profile.refetch()}
        />
      ) : null}
    </div>
  );
}

function ProfileBody({
  player,
  refreshing,
  onRefresh,
}: {
  player: PlayerProfile;
  refreshing: boolean;
  onRefresh: () => void;
}) {
  const team = player.team ? teamLabel(player.team.abbreviation, player.team.name) : null;

  return (
    <div className="mt-4 space-y-6">
      <header className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">
              {playerName(player.firstName, player.lastName)}
            </h1>
            <p className="mt-2 text-sm text-[var(--muted)]">{player.position}</p>
            <p className="mt-3 text-sm">{team ?? "Team not on file"}</p>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Bats {player.bats} · Throws {player.throws}
              {player.birthDate ? ` · Born ${formatBirthDate(player.birthDate)}` : ""}
            </p>
            <p className="mt-3 text-xs text-[var(--muted)]">MLB id {player.externalId}</p>
          </div>
          <button type="button" onClick={onRefresh} className="button-secondary" disabled={refreshing}>
            {refreshing ? "Refreshing" : "Refresh"}
          </button>
        </div>
      </header>

      {player.currentSeason ? (
        <SeasonSection season={player.currentSeason} />
      ) : (
        <EmptyState title="No season is on file." detail="DugoutIQ does not have a season for this player yet." />
      )}

      <ExpectedSection player={player} />
      <PlayerInformation player={player} />
      <DataSource player={player} />
    </div>
  );
}

function SeasonSection({ season }: { season: SeasonHitting }) {
  const counting = [
    ["Games", season.games],
    ["Plate appearances", season.plateAppearances],
    ["At bats", season.atBats],
    ["Hits", season.hits],
    ["Home runs", season.homeRuns],
    ["Walks", season.walks],
    ["Strikeouts", season.strikeouts],
    ["Doubles", season.doubles],
    ["Triples", season.triples],
    ["Hit by pitch", season.hitByPitch],
    ["Sacrifice flies", season.sacrificeFlies],
  ] as const;

  const rates = [
    ["AVG", season.battingAverage],
    ["OBP", season.onBasePercentage],
    ["SLG", season.sluggingPercentage],
    ["OPS", season.ops],
  ] as const;

  return (
    <>
      <section className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
        <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:justify-between">
          <h2 className="text-lg font-semibold tracking-tight">{season.season} season</h2>
          <p className="text-xs text-[var(--muted)]">Updated {formatTimestamp(season.updatedAt)}</p>
        </div>
        <p className="mt-1 text-sm text-[var(--muted)]">{season.freshness}</p>
        <dl className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {counting.map(([label, value]) => (
            <div key={label} className="rounded-md bg-[var(--canvas)] px-3 py-3">
              <dt className="text-xs font-medium uppercase tracking-[0.08em] text-[var(--muted)]">{label}</dt>
              <dd className="mt-1 font-mono text-xl tabular-nums">{formatCount(value)}</dd>
            </div>
          ))}
        </dl>
      </section>

      <section
        className="rounded-lg border border-[var(--line)] bg-[var(--paper)] px-5 py-5"
        aria-describedby="rate-note"
      >
        <h2 className="text-lg font-semibold tracking-tight">Rate statistics</h2>
        <p id="rate-note" className="mt-1 text-sm text-[var(--muted)]">
          Calculated from persisted counting statistics.
        </p>
        <dl className="mt-5 grid grid-cols-2 gap-3 lg:grid-cols-4">
          {rates.map(([label, value]) => (
            <div key={label} className="rounded-md border border-[var(--line)] bg-white px-3 py-3">
              <dt className="text-xs font-semibold tracking-[0.12em] text-[var(--accent)]">{label}</dt>
              <dd className="mt-1 font-mono text-3xl tabular-nums">
                {value === null ? <span className="text-lg text-[var(--muted)]">Unavailable</span> : formatRate(value)}
              </dd>
            </div>
          ))}
        </dl>
      </section>
    </>
  );
}

function ExpectedSection({ player }: { player: PlayerProfile }) {
  const expected = player.expectedHitting;
  const metrics = expected
    ? [
        ["Expected AVG", expected.expectedBattingAverage],
        ["Expected SLG", expected.expectedSlugging],
        ["Expected wOBA", expected.expectedWoba],
      ].filter((entry): entry is [string, number] => entry[1] !== null)
    : [];

  return (
    <section className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
      <h2 className="text-lg font-semibold tracking-tight">Expected statistics</h2>
      <p className="mt-1 text-sm text-[var(--muted)]">Provider observations stored by DugoutIQ.</p>
      {metrics.length > 0 ? (
        <dl className="mt-5 grid grid-cols-2 gap-3 lg:grid-cols-3">
          {metrics.map(([label, value]) => (
            <div key={label} className="rounded-md bg-[var(--canvas)] px-3 py-3">
              <dt className="text-xs font-medium uppercase tracking-[0.08em] text-[var(--muted)]">{label}</dt>
              <dd className="mt-1 font-mono text-2xl tabular-nums">{formatRate(value)}</dd>
            </div>
          ))}
        </dl>
      ) : (
        <p className="mt-4 text-sm">No expected statistics are on file.</p>
      )}
      {expected ? (
        <p className="mt-4 text-xs text-[var(--muted)]">Captured {formatTimestamp(expected.capturedAt)}</p>
      ) : null}
      {player.unavailableMetrics.length > 0 ? (
        <ul className="mt-4 space-y-1 border-t border-[var(--line)] pt-4 text-sm text-[var(--muted)]">
          {player.unavailableMetrics.map((metric) => (
            <li key={metric.name}>
              {metric.name}: {metric.reason}
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

function PlayerInformation({ player }: { player: PlayerProfile }) {
  const rows = [
    ["Birth date", player.birthDate ? formatBirthDate(player.birthDate) : "Not on file"],
    ["Position", player.position],
    ["Bats", player.bats],
    ["Throws", player.throws],
    ["Team", player.team ? teamLabel(player.team.abbreviation, player.team.name) ?? "Not on file" : "Not on file"],
    ["League", player.team?.league ?? "Not on file"],
    ["Division", player.team?.division ?? "Not on file"],
    ["MLB id", String(player.externalId)],
  ];

  return (
    <section className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
      <h2 className="text-lg font-semibold tracking-tight">Player information</h2>
      <dl className="mt-4 grid gap-x-8 gap-y-3 sm:grid-cols-2">
        {rows.map(([label, value]) => (
          <div key={label} className="grid grid-cols-[8.5rem_minmax(0,1fr)] gap-3 border-b border-[var(--line)] py-2">
            <dt className="text-sm text-[var(--muted)]">{label}</dt>
            <dd className="text-sm font-medium">{value}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function DataSource({ player }: { player: PlayerProfile }) {
  const rows = [
    ["Player and season data", "MLB Stats API"],
    ["Persistent application data", "DugoutIQ / SQL Server"],
    ["Rate statistics", player.currentSeason?.rateStatSource ?? "Calculated by DugoutIQ"],
    ["Expected statistics", player.expectedHitting?.source ?? "Provider observations"],
  ];

  return (
    <section className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
      <h2 className="text-sm font-semibold uppercase tracking-[0.12em] text-[var(--muted)]">Data</h2>
      <dl className="mt-3 space-y-2">
        {rows.map(([label, value]) => (
          <div key={label} className="grid gap-1 sm:grid-cols-[16rem_minmax(0,1fr)] sm:gap-4">
            <dt className="text-sm text-[var(--muted)]">{label}</dt>
            <dd className="text-sm">{value}</dd>
          </div>
        ))}
      </dl>
      <p className="mt-4 text-sm leading-6 text-[var(--muted)]">{player.dataClassification}</p>
    </section>
  );
}

function ProfileSkeleton() {
  return (
    <div className="mt-4 space-y-6" aria-busy="true" aria-live="polite">
      <p className="sr-only">Loading player profile</p>
      <section className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="mt-3 h-4 w-24" />
        <Skeleton className="mt-3 h-4 w-64" />
      </section>
      <section className="rounded-lg border border-[var(--line)] bg-white px-5 py-5">
        <Skeleton className="h-6 w-32" />
        <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
          {["a", "b", "c", "d", "e", "f", "g", "h"].map((key) => (
            <Skeleton key={key} className="h-16" />
          ))}
        </div>
      </section>
      <section className="rounded-lg border border-[var(--paper)] bg-[var(--paper)] px-5 py-5">
        <Skeleton className="h-6 w-40" />
        <div className="mt-5 grid grid-cols-2 gap-3 lg:grid-cols-4">
          {["avg", "obp", "slg", "ops"].map((key) => (
            <Skeleton key={key} className="h-20" />
          ))}
        </div>
      </section>
    </div>
  );
}
