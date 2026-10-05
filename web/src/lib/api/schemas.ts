import { z } from "zod";

export const playerSearchResultSchema = z.object({
  id: z.uuid(),
  externalId: z.number().int(),
  firstName: z.string(),
  lastName: z.string(),
  position: z.string(),
  bats: z.string(),
  throws: z.string(),
  teamAbbreviation: z.string().nullable(),
  teamName: z.string().nullable(),
});

export const playerSearchResponseSchema = z.array(playerSearchResultSchema);

export const teamSummarySchema = z.object({
  id: z.uuid(),
  name: z.string(),
  abbreviation: z.string(),
  league: z.string(),
  division: z.string(),
});

export const seasonHittingSchema = z.object({
  season: z.number().int(),
  games: z.number().int(),
  plateAppearances: z.number().int(),
  atBats: z.number().int(),
  hits: z.number().int(),
  doubles: z.number().int(),
  triples: z.number().int(),
  homeRuns: z.number().int(),
  walks: z.number().int(),
  strikeouts: z.number().int(),
  hitByPitch: z.number().int(),
  sacrificeFlies: z.number().int(),
  battingAverage: z.number().nullable(),
  onBasePercentage: z.number().nullable(),
  sluggingPercentage: z.number().nullable(),
  ops: z.number().nullable(),
  updatedAt: z.string(),
  freshness: z.string(),
  rateStatSource: z.string(),
});

export const expectedHittingSchema = z.object({
  season: z.number().int(),
  expectedBattingAverage: z.number().nullable(),
  expectedSlugging: z.number().nullable(),
  expectedWoba: z.number().nullable(),
  capturedAt: z.string(),
  source: z.string(),
});

export const unavailableMetricSchema = z.object({
  name: z.string(),
  reason: z.string(),
});

export const playerProfileSchema = z.object({
  id: z.uuid(),
  externalId: z.number().int(),
  firstName: z.string(),
  lastName: z.string(),
  position: z.string(),
  bats: z.string(),
  throws: z.string(),
  birthDate: z.string().nullable(),
  team: teamSummarySchema.nullable(),
  currentSeason: seasonHittingSchema.nullable(),
  expectedHitting: expectedHittingSchema.nullable(),
  unavailableMetrics: z.array(unavailableMetricSchema),
  dataClassification: z.string(),
});

export type PlayerSearchResult = z.infer<typeof playerSearchResultSchema>;
export type PlayerProfile = z.infer<typeof playerProfileSchema>;
export type SeasonHitting = z.infer<typeof seasonHittingSchema>;
