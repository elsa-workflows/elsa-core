"use client";
import { message, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Keyboard } from "lucide-react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@elsa-workflows/react-studio-ui/dialog";
import { Tooltip, TooltipContent, TooltipTrigger } from "@elsa-workflows/react-studio-ui/tooltip";

const groups: { title: string; items: [string, string][] }[] = [
  {
    title: message("Building"),
    items: [
      ["A", message("Add an activity (after the selected one, or at the centre)")],
      [message("Double-click canvas"), message("Add an activity at that spot")],
      [message("+ on a step / connection"), message("Append or insert an activity")],
      [message("Drag from toolbox"), message("Place an activity anywhere")],
      [message("Drag port → port"), message("Connect two steps")],
    ],
  },
  {
    title: message("Editing"),
    items: [
      [message("F2 / double-click step"), "Rename"],
      ["⌘ D", "Duplicate"],
      ["⌘ C / ⌘ X / ⌘ V", "Copy, cut, paste"],
      ["⌫", message("Delete selection")],
      ["⌘ Z / ⇧ ⌘ Z", "Undo / redo"],
      ["⌘ A", message("Select all")],
      ["⌘ S", "Save"],
    ],
  },
  {
    title: message("Canvas"),
    items: [
      [message("Scroll / pinch"), message("Zoom")],
      [message("Drag empty space"), message("Pan")],
      ["⇧ + drag", message("Select several steps")],
      ["Esc", message("Close menus")],
    ],
  },
];

export function ShortcutsDialog() {
  const _t = useTranslation();
  return (
    <Dialog>
      <Tooltip>
        <TooltipTrigger render={<DialogTrigger render={<Button variant="ghost" size="icon-sm" aria-label={_t("Keyboard shortcuts")} />} />}>
          <Keyboard />
        </TooltipTrigger>
        <TooltipContent>{_t("Shortcuts")}</TooltipContent>
      </Tooltip>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{_t("Shortcuts")}</DialogTitle>
          <DialogDescription>{_t("Everything on the canvas can be done with the keyboard or the mouse.")}</DialogDescription>
        </DialogHeader>
        <div className="grid gap-5 sm:grid-cols-2">
          {groups.map((g) => (
            <div key={_t(g.title)}>
              <div className="eyebrow mb-2">{_t(g.title)}</div>
              <dl className="flex flex-col gap-1.5 text-sm">
                {g.items.map(([k, v]) => (
                  <div key={k} className="flex items-start justify-between gap-3">
                    <dd className="text-muted-foreground">{_t(v)}</dd>
                    <dt className="shrink-0 rounded border bg-muted px-1.5 py-0.5 font-mono text-[11px]">{_t(k)}</dt>
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
