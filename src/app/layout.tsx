import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { loadStudioConfig } from "@/config/load";
import { Providers } from "./providers";

// Configuration is resolved per request, so nothing is prerendered at build time.
export const dynamic = "force-dynamic";

const geistSans = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });
const geistMono = Geist_Mono({ variable: "--font-geist-mono", subsets: ["latin"] });

export async function generateMetadata(): Promise<Metadata> {
  const config = await loadStudioConfig();
  return { title: { default: config.title, template: `%s · ${config.title}` } };
}

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  // Resolved per request so the configuration file / env can change without a rebuild.
  const config = await loadStudioConfig();
  return (
    <html
      lang="en"
      suppressHydrationWarning
      data-elsa-theme={config.ui.theme}
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-background text-foreground">
        <Providers config={config}>{children}</Providers>
      </body>
    </html>
  );
}
