"use client";

import { useState } from "react";
import { CirclePlay, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Field } from "@/components/common/field";
import { isStateMachine } from "@/lib/api/types/activities";
import { useDesigner, useDesignerState } from "../context";
import { resolveContainer } from "../model/flowchart-ops";
import { selectionId } from "../store";
import { SlotChip } from "./slot-chip";

/** Properties of the selected state: name, initial flag, entry and exit activities, outgoing transitions. */
export function StatePanel({ stateName }: { stateName: string }) {
  const { store } = useDesigner();
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const readOnly = useDesignerState((s) => s.readOnly);
  const sm = resolveContainer(root, path);
  // Remounted per state by the side pane (`key={stateName}`), so a plain initial value is enough.
  const [name, setName] = useState(stateName);
  if (!isStateMachine(sm)) return null;
  const index = sm.states.findIndex((s) => s.name === stateName);
  const state = sm.states[index];
  if (!state) return null;
  const outgoing = sm.transitions.map((t, i) => ({ t, i })).filter(({ t }) => t.from === stateName);
  const incoming = sm.transitions.filter((t) => t.to === stateName).length;
  const s = store.getState;

  return (
    <div className="flex flex-col gap-5 p-3">
      <div>
        <div className="eyebrow">State</div>
        <div className="text-lg font-semibold">{state.name}</div>
      </div>
      <Field label="Name" hint="Transitions reference states by name; renaming updates them.">
        <Input value={name} readOnly={readOnly} onChange={(e) => setName(e.target.value)} onBlur={() => name.trim() && name !== stateName && s().renameState(stateName, name.trim())} onKeyDown={(e) => e.key === "Enter" && (e.target as HTMLInputElement).blur()} />
      </Field>
      <Field inline label="Initial state" hint="The state entered when the machine starts.">
        <Checkbox checked={sm.initialState === stateName} disabled={readOnly} onCheckedChange={(c) => s().setInitialState(c ? stateName : undefined)} />
      </Field>
      <SlotChip ownerId={sm.id} slot={`states.${index}.entry`} child={state.entry} label="On entry" hint="Runs each time the state is entered, before its transitions are armed." readOnly={readOnly} />
      <SlotChip ownerId={sm.id} slot={`states.${index}.exit`} child={state.exit} label="On exit" hint="Runs when a transition leaves this state, before the transition's action." readOnly={readOnly} />

      <div className="flex flex-col gap-1.5">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium">Outgoing transitions</span>
          <span className="text-xs text-muted-foreground">{incoming} incoming</span>
        </div>
        {outgoing.length === 0 && <p className="text-xs text-muted-foreground">None: the state machine completes when this state is entered and nothing leaves it.</p>}
        {outgoing.map(({ t, i }) => (
          <button key={i} type="button" className="flex items-center justify-between rounded-md border px-2 py-1.5 text-left text-sm hover:border-primary" onClick={() => s().select([selectionId.transition(i)])}>
            <span className="truncate">{t.displayName || t.name || `→ ${t.to}`}</span>
            <span className="text-xs text-muted-foreground">{t.trigger ? "trigger" : "auto"} · to {t.to}</span>
          </button>
        ))}
        {!readOnly && (
          <Button variant="outline" size="sm" className="self-start" onClick={() => s().startConnect({ activityId: `state:${stateName}`, port: "out" })}>
            <CirclePlay /> Add transition…
          </Button>
        )}
      </div>

      {!readOnly && (
        <Button variant="ghost" size="sm" className="self-start text-status-danger hover:bg-status-danger/10" onClick={() => s().removeStates([stateName])}>
          <Trash2 /> Delete state
        </Button>
      )}
    </div>
  );
}
