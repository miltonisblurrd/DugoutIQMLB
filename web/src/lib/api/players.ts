import { apiGet } from "@/lib/api/client";
import { playerProfileSchema, playerSearchResponseSchema } from "@/lib/api/schemas";

export function searchPlayers(query: string) {
  const path = `/api/v1/players/search?q=${encodeURIComponent(query)}`;
  return apiGet(path, playerSearchResponseSchema);
}

export function getPlayer(id: string) {
  return apiGet(`/api/v1/players/${encodeURIComponent(id)}`, playerProfileSchema);
}
