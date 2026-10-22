/** Route patterns owned by this module. Kept apart from `index.tsx` so components can import them without a cycle. */
export const WorkflowInstanceRoutes = {
  list: "/workflows/instances",
  view: "/workflows/instances/:id/view",
} as const;
