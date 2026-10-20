"use client";

import { Keyboard } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

const groups: { title: string; items: [string, string][] }[] = [
  {
    title: "Building",
    items: [
      ["A", "Add an activity (after the selected one, or at the centre)"],
      ["Double-click canvas", "Add an activity at that spot"],
      ["+ on a step / connection", "Append or insert an activity"],
      ["Drag from toolbox", "Place an activity anywhere"],
      ["Drag port → port", "Connect two steps"],
    ],
  },
  {
    title: "Editing",
    items: [
      ["F2 / double-click step", "Rename"],
      ["⌘ D", "Duplicate"],
      ["⌘ C / ⌘ X / ⌘ V", "Copy, cut, paste"],
      ["⌫", "Delete selection"],
      ["⌘ Z / ⇧ ⌘ Z", "Undo / redo"],
      ["⌘ A", "Select all"],
      ["⌘ S", "Save"],
    ],
  },
  {
    title: "Canvas",
    items: [
      ["Scroll / pinch", "Zoom"],
      ["Drag empty space", "Pan"],
      ["⇧ + drag", "Select several steps"],
      ["Esc", "Close menus"],
    ],
  },
];

export function ShortcutsDialog() {
  return (
    <Dialog>
      <Tooltip>
        <TooltipTrigger render={<DialogTrigger render={<Button variant="ghost" size="icon-sm" aria-label="Keyboard shortcuts" />} />}>
          <Keyboard />
        </TooltipTrigger>
        <TooltipContent>Shortcuts</TooltipContent>
      </Tooltip>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Shortcuts</DialogTitle>
          <DialogDescription>Everything on the canvas can be done with the keyboard or the mouse.</DialogDescription>
        </DialogHeader>
        <div className="grid gap-5 sm:grid-cols-2">
          {groups.map((g) => (
            <div key={g.title}>
              <div className="eyebrow mb-2">{g.title}</div>
              <dl className="flex flex-col gap-1.5 text-sm">
                {g.items.map(([k, v]) => (
                  <div key={k} className="flex items-start justify-between gap-3">
                    <dd className="text-muted-foreground">{v}</dd>
                    <dt className="shrink-0 rounded border bg-muted px-1.5 py-0.5 font-mono text-[11px]">{k}</dt>
                  </div>
                ))}
              </dl>
            </div>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  );
}
