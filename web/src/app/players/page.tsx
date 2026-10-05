import type { Metadata } from "next";
import { PlayerSearchScreen } from "@/components/player-search-screen";

export const metadata: Metadata = {
  title: "Players",
};

export default function PlayersPage() {
  return <PlayerSearchScreen />;
}
