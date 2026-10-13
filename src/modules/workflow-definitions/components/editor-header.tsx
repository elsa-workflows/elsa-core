"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import {
  ArrowLeft,
  Check,
  ChevronRight,
  CloudUpload,
  Download,
  FileUp,
  Loader2,
  MoreHorizontal,
  Play,
  Rocket,
  Save,
  SlidersHorizontal,
  Undo2,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import { useWorkflowEditor } from "../editor-context";
import { useDesignerState } from "@/modules/designer";
import { WorkflowDefinitionRoutes } from "../routes";
import { IssuesPopover } from "./issues-popover";
import { ShortcutsDialog } from "./shortcuts-dialog";

export interface EditorViewState {
  view: "designer" | "code";
  showToolbox: boolean;
  showProperties: boolean;
  showMinimap: boolean;
  autoSave: boolean;
  /** Call-context mode: the workflow in a box with its callers and callees around it. */
  showContext: boolean;
}

export interface EditorHeaderProps {
  state: EditorViewState;
  onChange: (patch: Partial<EditorViewState>) => void;
  saving: boolean;
  viewingOldVersion: boolean;
  canPublish: boolean;
  canRetract: boolean;
  canExecute: boolean;
  onSave: () => void;
  onPublish: () => void;
  onUnpublish: () => void;
  onRun: () => void;
  onSaveAs: () => void;
  onExport: () => void;
  onImport: () => void;
  onBackToLatest: () => void;
  /** Shown when the editor was opened from another workflow (a step's "open target" or the call-context view). */
  backTo?: { label: string; onClick: () => void };
}

/** Breadcrumb + identity on the left, view controls in the middle, save/publish/run on the right. */
export function EditorHeader(p: EditorHeaderProps) {
  const { definition, readOnly, isDirty } = useWorkflowEditor();
  const showDataFlow = useDesignerState((s) => s.showDataFlow);
  const setShowDataFlow = useDesignerState((s) => s.setShowDataFlow);
  return (
    <>
      <nav
        className="flex min-w-0 items-center gap-1 text-sm"
        aria-label="Breadcrumb"
      >
        {p.backTo && (
          <Tooltip>
            <TooltipTrigger render={<Button variant="outline" size="xs" className="mr-1 shrink-0 gap-1" onClick={p.backTo.onClick} />}>
              <ArrowLeft />
              <span className="max-w-40 truncate">{p.backTo.label}</span>
            </TooltipTrigger>
            <TooltipContent>Back to the workflow you came from</TooltipContent>
          </Tooltip>
        )}
        <Link
          href={WorkflowDefinitionRoutes.list}
          className="hidden text-muted-foreground hover:text-foreground sm:inline"
        >
          Definitions
        </Link>
        <ChevronRight className="hidden size-3.5 text-muted-foreground sm:inline" />
        <InlineName />
        <Badge variant="outline" className="ml-1 shrink-0 font-mono">
          v{definition.version}
        </Badge>
        {definition.isPublished ? (
          <Badge className="shrink-0 border-transparent bg-status-success/10 text-status-success">
            Published
          </Badge>
        ) : (
          <Badge variant="secondary" className="shrink-0">
            Draft
          </Badge>
        )}
        {p.viewingOldVersion && (
          <Badge variant="outline" className="shrink-0 text-status-warning">
            older version
          </Badge>
        )}
        {readOnly && !p.viewingOldVersion && (
          <Badge variant="outline" className="shrink-0">
            Read-only
          </Badge>
        )}
      </nav>

      <div className="mx-auto flex items-center gap-1">
        <Tabs
          value={p.state.view}
          onValueChange={(v) =>
            p.onChange({ view: v as EditorViewState["view"] })
          }
        >
          <TabsList className="h-8">
            <TabsTrigger value="designer" className="text-xs">
              Designer
            </TabsTrigger>
            <TabsTrigger value="code" className="text-xs">
              Code
            </TabsTrigger>
          </TabsList>
        </Tabs>
        {p.state.view === "designer" && (
          <DropdownMenu>
            <Tooltip>
              <TooltipTrigger
                render={
                  <DropdownMenuTrigger
                    render={
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        aria-label="View options"
                      />
                    }
                  />
                }
              >
                <SlidersHorizontal />
              </TooltipTrigger>
              <TooltipContent>View</TooltipContent>
            </Tooltip>
            <DropdownMenuContent align="center" className="w-56">
              <DropdownMenuGroup>
                <DropdownMenuLabel className="eyebrow">
                  Panels
                </DropdownMenuLabel>
                {!readOnly && (
                  <DropdownMenuCheckboxItem
                    checked={p.state.showToolbox}
                    onCheckedChange={(c) => p.onChange({ showToolbox: !!c })}
                  >
                    Activity toolbox
                  </DropdownMenuCheckboxItem>
                )}
                <DropdownMenuCheckboxItem
                  checked={p.state.showProperties}
                  onCheckedChange={(c) => p.onChange({ showProperties: !!c })}
                >
                  Properties pane
                </DropdownMenuCheckboxItem>
                <DropdownMenuCheckboxItem
                  checked={p.state.showMinimap}
                  onCheckedChange={(c) => p.onChange({ showMinimap: !!c })}
                >
                  Mini-map
                </DropdownMenuCheckboxItem>
                <DropdownMenuCheckboxItem checked={showDataFlow} onCheckedChange={(c) => setShowDataFlow(!!c)}>
                  Data flow overlay
                </DropdownMenuCheckboxItem>
                <DropdownMenuCheckboxItem checked={p.state.showContext} onCheckedChange={(c) => p.onChange({ showContext: !!c })}>
                  Call context (callers &amp; callees)
                </DropdownMenuCheckboxItem>
              </DropdownMenuGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
        {p.state.view === "designer" && <ShortcutsDialog />}
      </div>

      <div className="flex shrink-0 items-center gap-1.5">
        {!readOnly && p.state.view === "designer" && <IssuesPopover />}
        {p.viewingOldVersion && (
          <Button variant="outline" size="sm" onClick={p.onBackToLatest}>
            Back to latest
          </Button>
        )}
        {!readOnly && (
          <>
            <Tooltip>
              <TooltipTrigger
                render={
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={p.onSave}
                    disabled={p.saving || !isDirty}
                    className={cn(
                      "gap-1.5",
                      isDirty ? "text-status-warning" : "text-muted-foreground",
                    )}
                  />
                }
              >
                {p.saving ? (
                  <Loader2 className="animate-spin" />
                ) : isDirty ? (
                  <Save />
                ) : (
                  <Check className="text-status-success" />
                )}
                <span className="hidden md:inline">
                  {p.saving ? "Saving…" : isDirty ? "Unsaved changes" : "Saved"}
                </span>
              </TooltipTrigger>
              <TooltipContent>
                {p.state.autoSave
                  ? "Auto-save is on · ⌘S saves now"
                  : "Auto-save is off · ⌘S saves"}
              </TooltipContent>
            </Tooltip>
            {p.canPublish && (
              <Button size="sm" onClick={p.onPublish} disabled={p.saving}>
                <Rocket /> Publish
              </Button>
            )}
          </>
        )}
        {p.canExecute && definition.isPublished && (
          <Button
            variant="outline"
            size="sm"
            onClick={p.onRun}
            className="hidden lg:inline-flex"
          >
            <Play /> Run
          </Button>
        )}
        <DropdownMenu>
          <DropdownMenuTrigger
            render={<Button variant="ghost" size="icon-sm" aria-label="More" />}
          >
            <MoreHorizontal />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            {p.canExecute && definition.isPublished && (
              <DropdownMenuItem onClick={p.onRun} className="lg:hidden">
                <Play /> Run
              </DropdownMenuItem>
            )}
            {!readOnly && (
              <>
                <DropdownMenuCheckboxItem
                  checked={p.state.autoSave}
                  onCheckedChange={(c) => p.onChange({ autoSave: !!c })}
                >
                  Auto-save
                </DropdownMenuCheckboxItem>
                <DropdownMenuItem onClick={p.onSaveAs}>
                  <CloudUpload /> Save as…
                </DropdownMenuItem>
              </>
            )}
            {p.canRetract && definition.isPublished && !p.viewingOldVersion && (
              <DropdownMenuItem onClick={p.onUnpublish}>
                <Undo2 /> Unpublish
              </DropdownMenuItem>
            )}
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={p.onExport}>
              <Download /> Export JSON
            </DropdownMenuItem>
            {!readOnly && (
              <DropdownMenuItem onClick={p.onImport}>
                <FileUp /> Import JSON…
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </>
  );
}

/** The workflow name doubles as an inline editor: click to rename. */
function InlineName() {
  const { definition, updateDefinition, readOnly } = useWorkflowEditor();
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(definition.name ?? "");
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (editing) {
      ref.current?.focus();
      ref.current?.select();
    }
  }, [editing]);
  const commit = () => {
    const name = value.trim();
    if (name && name !== definition.name) updateDefinition({ name });
    setEditing(false);
  };
  if (editing) {
    return (
      <input
        ref={ref}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onBlur={commit}
        onKeyDown={(e) => {
          if (e.key === "Enter") commit();
          if (e.key === "Escape") setEditing(false);
        }}
        className="h-7 min-w-40 max-w-md rounded border border-primary bg-background px-1.5 text-sm font-semibold outline-none"
        aria-label="Workflow name"
      />
    );
  }
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <button
            type="button"
            disabled={readOnly}
            onClick={() => {
              setValue(definition.name ?? "");
              setEditing(true);
            }}
            className="min-w-0 truncate rounded px-1 text-sm font-semibold tracking-tight hover:bg-accent disabled:hover:bg-transparent"
          />
        }
      >
        {definition.name ?? (
          <span className="italic text-muted-foreground">(unnamed)</span>
        )}
      </TooltipTrigger>
      <TooltipContent>
        {readOnly ? "Workflow name" : "Click to rename"}
      </TooltipContent>
    </Tooltip>
  );
}
