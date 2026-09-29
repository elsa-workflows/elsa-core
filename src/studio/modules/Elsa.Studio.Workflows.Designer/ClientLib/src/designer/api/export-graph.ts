import {DataUri} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export type ExportGraphFormat = 'png' | 'jpeg' | 'svg';

export interface ExportGraphOptions {
    fileName: string;
    format: ExportGraphFormat;
    padding?: number;
}

// Downloads the graph's content as an image. The file name is expected without an extension; the one matching the
// format is appended here so that the downloaded file always advertises the format the user actually asked for.
export function exportGraph(graphId: string, options: ExportGraphOptions) {
    const graph = graphBindings[graphId]?.graph;

    if (!graph)
        return;

    const {format, padding} = options;
    const download = (dataUri: string) => DataUri.downloadDataUri(dataUri, `${options.fileName}.${format}`);

    switch (format) {
        case 'png':
            graph.toPNG(download, {padding});
            break;
        case 'jpeg':
            // Not graph.exportJPEG(): that helper delegates to toPNG() in @antv/x6-plugin-export, which would write
            // PNG bytes into a .jpeg file. toJPEG() encodes as image/jpeg.
            graph.toJPEG(download, {padding});
            break;
        case 'svg':
            // Vector output has no raster canvas to pad, so the padding is not applicable here.
            graph.toSVG(svg => download(DataUri.svgToDataUrl(svg)));
            break;
        default:
            throw new Error(`Unsupported export format: ${format}.`);
    }
}
