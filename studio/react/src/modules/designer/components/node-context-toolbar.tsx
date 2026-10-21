"use client";

import { NodeToolbar, Position } from "@xyflow/react";
import { ArrowUpRight, Copy, Link2, Pencil, Plus, Settings2, Trash2, Zap } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuGroup, DropdownMenuItem, DropdownMenuLabel, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { Activity } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import { useDesigner } from "../context";
import type { ActivityPorts } from "../model/ports";

/** Floating actions above the selected node (Camunda's context pad / n8n's node actions). */
export function NodeContextToolbar({ activity, descriptor, ports, visible }: { activity: Activity; descriptor?: ActivityDescriptor; ports: ActivityPorts; visible: boolean }) {
  const { store, activityLink } = useDesigner();
  const s = store.getState;
  const canStart = activity.customProperties?.canStartWorkflow === true;
  const link = activityLink?.(activity);
  return (
    <NodeToolbar isVisible={visible} position={Position.Top} offset={8} className="flex items-center gap-0.5 rounded-lg border bg-popover p-0.5 shadow-md">
      {ports.outPorts.length === 1 && (
        <Action
          label="Add next activity (A)"
          onClick={(e) => s().openQuickAdd({ screen: { x: e.clientX, y: e.clientY + 12 }, source: { activityId: activity.id, port: ports.outPorts[0].name } })}
        >
          <Plus />
        </Action>
      )}
      {ports.outPorts.length > 1 && (
        <PortMenu label="Add next activity after…" icon={<Plus />} ports={ports} onPick={(port, e) => s().openQuickAdd({ screen: { x: e.clientX, y: e.clientY + 12 }, source: { activityId: activity.id, port } })} />
      )}
      {ports.outPorts.length === 1 && (
        <Action label="Connect to an existing step" onClick={() => s().startConnect({ activityId: activity.id, port: ports.outPorts[0].name })}>
          <Link2 />
        </Action>
      )}
      {ports.outPorts.length > 1 && (
        <PortMenu label="Connect to an existing step from…" icon={<Link2 />} ports={ports} onPick={(port) => s().startConnect({ activityId: activity.id, port })} />
      )}
      <Action label="Settings" onClick={() => s().requestSettings()}>
        <Settings2 />
      </Action>
      {link && (
        <Action label={link.label} onClick={() => link.onOpen()}>
          <ArrowUpRight />
        </Action>
      )}
      <Action label="Rename (F2)" onClick={() => s().startRename(activity.id)}>
        <Pencil />
      </Action>
      <Action label="Duplicate (⌘D)" onClick={() => s().duplicate(activity.id)}>
        <Copy />
      </Action>
      {descriptor?.kind === "Trigger" && (
        <Action
          label={canStart ? "Starts the workflow (click to turn off)" : "Make this trigger start the workflow"}
          active={canStart}
          onClick={() => s().updateActivity(activity.id, (a) => ({ ...a, customProperties: { ...a.customProperties, canStartWorkflow: !canStart } }))}
        >
          <Zap />
        </Action>
      )}
      <Action label="Delete (⌫)" destructive onClick={() => s().removeActivities([activity.id])}>
        <Trash2 />
      </Action>
    </NodeToolbar>
  );
}

function PortMenu({ label, icon, ports, onPick }: { label: string; icon: React.ReactNode; ports: ActivityPorts; onPick: (port: string, e: React.MouseEvent) => void }) {
  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger render={<DropdownMenuTrigger render={<Button variant="ghost" size="icon-xs" aria-label={label} onClick={(e) => e.stopPropagation()} />} />}>{icon}</TooltipTrigger>
        <TooltipContent>{label}</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="start" onClick={(e) => e.stopPropagation()}>
        <DropdownMenuGroup>
          <DropdownMenuLabel className="eyebrow">Outcome</DropdownMenuLabel>
          {ports.outPorts.map((port) => (
            <DropdownMenuItem key={port.name} onClick={(e) => onPick(port.name, e)}>
              <span className="port-dot bg-primary ring-0" /> {port.displayName}
            </DropdownMenuItem>
          ))}
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function Action({ label, onClick, children, destructive, active }: { label: string; onClick: (e: React.MouseEvent) => void; children: React.ReactNode; destructive?: boolean; active?: boolean }) {
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Button
            variant="ghost"
            size="icon-xs"
            aria-label={label}
            className={destructive ? "hover:bg-status-danger/15 hover:text-status-danger" : active ? "bg-status-warning/15 text-status-warning" : undefined}
            onClick={(e) => {
              e.stopPropagation();
              onClick(e);
            }}
          />
        }
      >
        {children}
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}
