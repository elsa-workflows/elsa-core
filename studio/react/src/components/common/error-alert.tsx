import { AlertCircle } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { ApiError } from "@/lib/api/http";

export function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.problem.errors) {
      return Object.entries(error.problem.errors)
        .map(([k, v]) => `${k}: ${v.join(", ")}`)
        .join("; ");
    }
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return String(error);
}

export function ErrorAlert({ error, title = "Something went wrong" }: { error: unknown; title?: string }) {
  if (!error) return null;
  return (
    <Alert variant="destructive">
      <AlertCircle />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription>{describeError(error)}</AlertDescription>
    </Alert>
  );
}
