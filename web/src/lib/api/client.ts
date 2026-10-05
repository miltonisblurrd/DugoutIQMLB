import type { z } from "zod";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number) {
    super("Request failed");
    this.name = "ApiError";
    this.status = status;
  }
}

export function apiBaseUrl(): string {
  const value = process.env.NEXT_PUBLIC_DUGOUTIQ_API_URL;
  if (!value) {
    throw new Error("NEXT_PUBLIC_DUGOUTIQ_API_URL is not set.");
  }

  return value.replace(/\/$/, "");
}

export async function apiGet<T>(path: string, schema: z.ZodType<T>): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl()}${path}`, {
      headers: { Accept: "application/json" },
    });
  } catch {
    throw new ApiError(0);
  }

  if (!response.ok) {
    throw new ApiError(response.status);
  }

  let body: unknown;
  try {
    body = await response.json();
  } catch {
    throw new ApiError(response.status);
  }

  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    throw new ApiError(response.status);
  }

  return parsed.data;
}
