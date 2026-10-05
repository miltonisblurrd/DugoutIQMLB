export function playerName(firstName: string, lastName: string): string {
  return `${firstName} ${lastName}`;
}

export function teamLabel(abbreviation: string | null, name: string | null): string | null {
  if (abbreviation && name) {
    return `${name} (${abbreviation})`;
  }

  return name ?? abbreviation;
}

export function formatCount(value: number): string {
  return new Intl.NumberFormat("en-US").format(value);
}

/** Display a domain rate. This does not recompute AVG, OBP, SLG, or OPS. */
export function formatRate(value: number): string {
  const fixed = value.toFixed(3);
  return fixed.startsWith("0.") ? fixed.slice(1) : fixed;
}

export function formatBirthDate(isoDate: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
  if (!match) {
    return isoDate;
  }

  const date = new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
  return new Intl.DateTimeFormat("en-US", {
    month: "long",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

export function formatTimestamp(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return `${new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }).format(date)} UTC`;
}
