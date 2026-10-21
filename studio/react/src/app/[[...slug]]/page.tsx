import { StudioRouter } from "@/components/shell/studio-router";

/**
 * Every studio page goes through this catch-all. The module registry decides which component
 * renders for the path, so enabling or disabling a module in configuration is enough to add or
 * remove its routes: no file-system route per module.
 */
export default async function StudioPage({ params }: { params: Promise<{ slug?: string[] }> }) {
  const { slug } = await params;
  const path = "/" + (slug ?? []).join("/");
  return <StudioRouter path={path} />;
}
