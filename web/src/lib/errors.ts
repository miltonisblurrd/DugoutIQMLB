import { ApiError } from "@/lib/api/client";

export function describeApiError(error: unknown, scope: "search" | "profile"): string {
  if (error instanceof ApiError) {
    if (error.status === 400) {
      return "Enter a valid player name.";
    }

    if (error.status === 404) {
      return "Player could not be found.";
    }

    if (error.status === 0 || error.status === 503) {
      return "DugoutIQ API is currently unavailable.";
    }
  }

  if (scope === "profile") {
    return "Something went wrong while loading this player.";
  }

  return "Something went wrong while searching for players.";
}
