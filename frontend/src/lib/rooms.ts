"use client";

import { useCallback, useEffect, useState } from "react";
import { errorMessage, get } from "./api";
import type { AiAnalysis, InspectionDetails, RoomDetail } from "./types";

export const roomUrl = (inspectionId: string, roomId: string) => `/api/agent/inspections/${inspectionId}/rooms/${roomId}`;

const running = (a: AiAnalysis | null | undefined) => a?.status === "Pending" || a?.status === "Processing";

/** True while any AI job for the room (description, defects, comparison) is still running. */
export const hasRunningAnalysis = (r: RoomDetail) =>
  running(r.latestAnalysis) || r.defects.some((d) => running(d.latestAnalysis)) || running(r.comparison?.latestAnalysis);

async function fetchAll(inspectionId: string): Promise<[InspectionDetails, RoomDetail[]]> {
  const details = await get<InspectionDetails>(`/api/agent/inspections/${inspectionId}`);
  const rooms = [...details.rooms].sort((a, b) => a.sequence - b.sequence);
  return [details, await Promise.all(rooms.map((r) => get<RoomDetail>(roomUrl(inspectionId, r.id))))];
}

/** Loads an inspection and the full detail of every room, in sequence order. */
export function useInspectionRooms(inspectionId: string) {
  const [inspection, setInspection] = useState<InspectionDetails | null>(null);
  const [rooms, setRooms] = useState<RoomDetail[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [details, all] = await fetchAll(inspectionId);
      setInspection(details);
      setRooms(all);
      setError(null);
    } catch (e) { setError(errorMessage(e)); }
  }, [inspectionId]);

  useEffect(() => {
    let cancelled = false;
    fetchAll(inspectionId).then(
      ([details, all]) => { if (!cancelled) { setInspection(details); setRooms(all); setError(null); } },
      (e) => { if (!cancelled) setError(errorMessage(e)); },
    );
    return () => { cancelled = true; };
  }, [inspectionId]);

  const setRoom = useCallback((room: RoomDetail) => setRooms((rs) => rs?.map((r) => (r.id === room.id ? room : r)) ?? rs), []);
  const reloadRoom = useCallback(async (roomId: string) => { setRoom(await get<RoomDetail>(roomUrl(inspectionId, roomId))); }, [inspectionId, setRoom]);

  return { inspection, rooms, error, load, setRoom, reloadRoom };
}
