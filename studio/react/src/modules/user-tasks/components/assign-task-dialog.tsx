"use client";

import { Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { ErrorAlert } from "@/components/common/error-alert";
import { useAuth } from "@/lib/auth/provider";
import { type UserTaskParticipantSummary, participantLabel } from "../api";
import { useUserTasksApi } from "../hooks";

/**
 * Assign the task to a user or group. When the server exposes a participant directory the id can
 * be looked up by name; otherwise it is entered by hand.
 */
export function AssignTaskDialog({
  open,
  onOpenChange,
  onSubmit,
  isPending,
  participantPicker,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (assignee: UserTaskParticipantSummary, reason?: string) => Promise<unknown>;
  isPending: boolean;
  participantPicker: boolean;
}) {
  const api = useUserTasksApi();
  const { user } = useAuth();
  // The server matches assignees on the token's subject claim, so "me" is the `sub` claim, not the display name.
  const mySubject = typeof user?.claims?.sub === "string" ? user.claims.sub : undefined;
  const [kind, setKind] = useState<"user" | "group">("user");
  const [id, setId] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [provider, setProvider] = useState("");
  const [reason, setReason] = useState("");
  const [search, setSearch] = useState("");
  const [matches, setMatches] = useState<UserTaskParticipantSummary[]>([]);
  const [searchError, setSearchError] = useState<unknown>();
  const [searching, setSearching] = useState(false);

  const lookup = async () => {
    setSearching(true);
    setSearchError(undefined);
    try {
      const res = await api.searchParticipants(search, kind);
      setMatches(res.items);
    } catch (e) {
      setSearchError(e);
    } finally {
      setSearching(false);
    }
  };

  const pick = (p: UserTaskParticipantSummary) => {
    setId(p.id);
    setDisplayName(p.displayName ?? "");
    setProvider(p.provider ?? "");
    setKind(p.kind === "group" ? "group" : "user");
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Assign task</DialogTitle>
          <DialogDescription>Hand the task to a specific user or group.</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-3">
          <Field label="Kind">
            <SimpleSelect value={kind} onChange={(v) => v && setKind(v)} options={[{ value: "user", label: "User" }, { value: "group", label: "Group" }]} />
          </Field>
          {participantPicker && (
            <Field label="Look up" hint="Search the participant directory and pick a match.">
              <form
                className="flex gap-2"
                onSubmit={(e) => {
                  e.preventDefault();
                  lookup();
                }}
              >
                <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Name or id…" />
                <Button type="submit" variant="outline" disabled={searching}>
                  {searching ? <Loader2 className="animate-spin" /> : "Search"}
                </Button>
              </form>
              <ErrorAlert error={searchError} title="Lookup failed" />
              {matches.length > 0 && (
                <ul className="mt-1 max-h-40 overflow-auto rounded-md border divide-y text-sm">
                  {matches.map((m) => (
                    <li key={`${m.kind}:${m.provider}:${m.id}`}>
                      <button type="button" className="w-full text-left px-2 py-1.5 hover:bg-muted" onClick={() => pick(m)}>
                        {participantLabel(m)} <span className="text-muted-foreground text-xs">({m.kind}{m.provider ? ` · ${m.provider}` : ""})</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </Field>
          )}
          <Field label="Participant id" required htmlFor="assign-id" hint={mySubject ? "The user's subject id as issued by the identity provider." : undefined}>
            <div className="flex gap-2">
              <Input id="assign-id" value={id} onChange={(e) => setId(e.target.value)} />
              {mySubject && (
                <Button
                  type="button"
                  variant="outline"
                  className="shrink-0"
                  onClick={() => {
                    setKind("user");
                    setId(mySubject);
                    setDisplayName(user?.name ?? "");
                    setProvider("");
                  }}
                >
                  Assign to me
                </Button>
              )}
            </div>
          </Field>
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="Display name" htmlFor="assign-name">
              <Input id="assign-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
            </Field>
            <Field label="Provider" htmlFor="assign-provider" hint="Defaults to your own identity provider.">
              <Input id="assign-provider" value={provider} onChange={(e) => setProvider(e.target.value)} />
            </Field>
          </div>
          <Field label="Reason" htmlFor="assign-reason">
            <Input id="assign-reason" value={reason} onChange={(e) => setReason(e.target.value)} />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button
            disabled={isPending || !id.trim()}
            onClick={() => onSubmit({ kind, id: id.trim(), displayName: displayName.trim() || null, provider: provider.trim() || null }, reason.trim() || undefined)}
          >
            {isPending && <Loader2 className="animate-spin" data-icon="inline-start" />}
            Assign
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
