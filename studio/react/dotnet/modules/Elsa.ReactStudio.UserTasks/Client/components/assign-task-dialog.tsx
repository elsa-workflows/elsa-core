"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { useAuth } from "@elsa-workflows/react-studio-auth";
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
  const _t = useTranslation();
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
          <DialogTitle>{_t("Assign task")}</DialogTitle>
          <DialogDescription>{_t("Hand the task to a specific user or group.")}</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-3">
          <Field label={_t("Kind")}>
            <SimpleSelect value={kind} onChange={(v) => v && setKind(v)} options={[{ value: "user", label: _t("User") }, { value: "group", label: _t("Group") }]} />
          </Field>
          {participantPicker && (
            <Field label={_t("Look up")} hint={_t("Search the participant directory and pick a match.")}>
              <form
                className="flex gap-2"
                onSubmit={(e) => {
                  e.preventDefault();
                  lookup();
                }}
              >
                <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder={_t("Name or id…")} />
                <Button type="submit" variant="outline" disabled={searching}>
                  {searching ? <Loader2 className="animate-spin" /> : _t("Search")}
                </Button>
              </form>
              <ErrorAlert error={searchError} title={_t("Lookup failed")} />
              {matches.length > 0 && (
                <ul className="mt-1 max-h-40 overflow-auto rounded-md border divide-y text-sm">
                  {matches.map((m) => (
                    <li key={`${m.kind}:${m.provider}:${m.id}`}>
                      <button type="button" className="w-full text-left px-2 py-1.5 hover:bg-muted" onClick={() => pick(m)}>
                        {participantLabel(m)} <span className="text-muted-foreground text-xs">({m.kind}{m.provider ? _t(" · {{value0}}", { value0: m.provider }) : ""})</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </Field>
          )}
          <Field label={_t("Participant id")} required htmlFor="assign-id" hint={mySubject ? _t("The user's subject id as issued by the identity provider.") : undefined}>
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
                  {_t("Assign to me")}</Button>
              )}
            </div>
          </Field>
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label={_t("Display name")} htmlFor="assign-name">
              <Input id="assign-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
            </Field>
            <Field label={_t("Provider")} htmlFor="assign-provider" hint={_t("Defaults to your own identity provider.")}>
              <Input id="assign-provider" value={provider} onChange={(e) => setProvider(e.target.value)} />
            </Field>
          </div>
          <Field label={_t("Reason")} htmlFor="assign-reason">
            <Input id="assign-reason" value={reason} onChange={(e) => setReason(e.target.value)} />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            {_t("Cancel")}</Button>
          <Button
            disabled={isPending || !id.trim()}
            onClick={() => onSubmit({ kind, id: id.trim(), displayName: displayName.trim() || null, provider: provider.trim() || null }, reason.trim() || undefined)}
          >
            {isPending && <Loader2 className="animate-spin" data-icon="inline-start" />}
            {_t("Assign")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
