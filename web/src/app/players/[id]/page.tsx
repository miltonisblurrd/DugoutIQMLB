import type { Metadata } from "next";
import { PlayerProfileScreen } from "@/components/player-profile-screen";

export const metadata: Metadata = {
  title: "Player",
};

export default async function PlayerProfilePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PlayerProfileScreen id={id} />;
}
