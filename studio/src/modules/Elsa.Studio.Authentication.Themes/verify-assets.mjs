import { readFile, stat } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(fileURLToPath(import.meta.url));
const imageRoot = join(root, "wwwroot", "images");
const budget = JSON.parse(
  await readFile(join(imageRoot, "asset-budget.json"), "utf8"),
);

let measuredTotal = 0;

for (const [fileName, limits] of Object.entries(budget.assets)) {
  if (/^(?:https?:)?\/\//i.test(fileName)) {
    throw new Error(`Asset must be same-origin: ${fileName}`);
  }

  const { size } = await stat(join(imageRoot, fileName));
  measuredTotal += size;

  if (size > limits.maximumBytes) {
    throw new Error(
      `${fileName} is ${size} bytes; budget is ${limits.maximumBytes} bytes`,
    );
  }
}

if (measuredTotal > budget.total.maximumBytes) {
  throw new Error(
    `Theme assets total ${measuredTotal} bytes; budget is ${budget.total.maximumBytes} bytes`,
  );
}

console.log(
  `Verified ${Object.keys(budget.assets).length} same-origin assets (${measuredTotal} bytes).`,
);
