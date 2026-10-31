// Reproducible #8854 CPU measurement; no timing assertion that depends on CI hardware.
import { createServer } from "vite";
const server = await createServer({ configFile: false, optimizeDeps: { noDiscovery: true, entries: [] }, server: { middlewareMode: true }, appType: "custom" });
try {
  const { parseServerTypes, typedCompletions } = await server.ssrLoadModule("/dotnet/modules/Elsa.ReactStudio.ActivityEditors/Client/type-completions.ts");
  const types = parseServerTypes("declare class Customer {\nName: string;\nAge: number;\n}");
  const context = { variables: Array.from({ length: 3000 }, (_, index) => ({ id: String(index), name: `value${index}`, typeName: "Customer" })),
    inputs: Array.from({ length: 3000 }, (_, index) => ({ name: `Input${index}`, type: "String" })),
    activities: Array.from({ length: 10000 }, (_, index) => ({ name: `Activity${index}`, outputs: ["Result", "Value"] })) };
  for (const language of ["csharp", "javascript", "python"]) {
    const times = [];
    let count;
    for (let iteration = 0; iteration < 6; iteration++) {
      const start = performance.now();
      count = typedCompletions(context, language, types).length;
      if (iteration) times.push(performance.now() - start);
    }
    times.sort((a, b) => a - b);
    console.log(`${language}: ${count} completions, median ${times[2].toFixed(1)} ms (3000 variables, 3000 inputs, 10000 activities; five measured runs)`);
  }
} finally { await server.close(); }
