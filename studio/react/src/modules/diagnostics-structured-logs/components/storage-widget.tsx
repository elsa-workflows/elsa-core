"use client";

import { ScrollText } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ToneBadge } from "@/components/common/status-badge";
import { useStructuredLogStorage } from "../hooks";

/** Dashboard "status" widget for `GET /diagnostics/structured-logs/storage`. Hidden on 404 or error. */
export function StructuredLogStorageWidget() {
  const query = useStructuredLogStorage();
  if (!query.data) return null;
  const { droppedWriteCount, hasStorageDiagnosticsProvider } = query.data;
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-sm">
          <ScrollText className="size-4" /> Structured log storage
        </CardTitle>
      </CardHeader>
      <CardContent className="flex items-center justify-between gap-2 text-sm">
        {hasStorageDiagnosticsProvider ? (
          <>
            <span className="text-muted-foreground">Dropped writes</span>
            <ToneBadge tone={droppedWriteCount > 0 ? "warning" : "success"} className="tabular-nums">
              {droppedWriteCount}
            </ToneBadge>
          </>
        ) : (
          <span className="text-muted-foreground">No persistent storage configured (in-memory buffer).</span>
        )}
      </CardContent>
    </Card>
  );
}
