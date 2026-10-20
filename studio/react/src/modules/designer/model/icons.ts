import {
  Activity as ActivityIcon,
  Braces,
  Clock,
  Code2,
  Database,
  FlagTriangleRight,
  GitBranch,
  GitFork,
  GitMerge,
  Globe,
  Mail,
  Play,
  Repeat,
  Square,
  Terminal,
  Timer,
  Variable,
  Workflow,
  Zap,
  type LucideIcon,
} from "lucide-react";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";

const byType: Record<string, LucideIcon> = {
  "Elsa.Start": Play,
  "Elsa.Finish": Square,
  "Elsa.If": GitBranch,
  "Elsa.FlowDecision": GitBranch,
  "Elsa.Switch": GitFork,
  "Elsa.FlowSwitch": GitFork,
  "Elsa.FlowJoin": GitMerge,
  "Elsa.Fork": GitFork,
  "Elsa.ForEach": Repeat,
  "Elsa.While": Repeat,
  "Elsa.For": Repeat,
  "Elsa.Timer": Timer,
  "Elsa.Delay": Clock,
  "Elsa.Cron": Clock,
  "Elsa.SetVariable": Variable,
  "Elsa.WriteLine": Terminal,
  "Elsa.ReadLine": Terminal,
  "Elsa.RunJavaScript": Code2,
  "Elsa.RunCSharp": Code2,
  "Elsa.RunPython": Code2,
  "Elsa.Event": Zap,
  "Elsa.SendEmail": Mail,
  "Elsa.SetOutput": FlagTriangleRight,
  "Elsa.Correlate": Braces,
};

const byCategory: Record<string, LucideIcon> = {
  HTTP: Globe,
  Http: Globe,
  Scheduling: Timer,
  Branching: GitBranch,
  "Control Flow": GitBranch,
  Looping: Repeat,
  Scripting: Code2,
  Storage: Database,
  Email: Mail,
  Console: Terminal,
  Primitives: Variable,
  Workflows: Workflow,
  Composition: Workflow,
  Flow: GitMerge,
  WorkflowDefinitionActivity: Workflow,
};

export function activityIcon(typeName: string, descriptor?: ActivityDescriptor): LucideIcon {
  if (byType[typeName]) return byType[typeName];
  if (descriptor?.kind === "Trigger") return Zap;
  if (descriptor?.category && byCategory[descriptor.category]) return byCategory[descriptor.category];
  return ActivityIcon;
}

/** Accent colour (a CSS colour value) per descriptor kind/category, used for node rails, icons and the toolbox. */
export function activityAccentColor(descriptor?: ActivityDescriptor): string {
  if (descriptor?.kind === "Trigger") return "var(--status-warning)";
  if (descriptor?.isTerminal) return "var(--status-danger)";
  switch (descriptor?.category) {
    case "Branching":
    case "Control Flow":
    case "Flow":
      return "var(--elsa-secondary)";
    case "Looping":
      return "#a855f7";
    case "Scripting":
      return "var(--status-success)";
    case "Email":
      return "#ec4899";
    case "Workflows":
    case "Composition":
    case "WorkflowDefinitionActivity":
      return "#6366f1";
    default:
      return "var(--primary)";
  }
}

/** Kept for callers that only need a text colour class; the rail colour drives the actual hue. */
export function activityAccent(): string {
  return "text-[color:var(--rail-color)]";
}
