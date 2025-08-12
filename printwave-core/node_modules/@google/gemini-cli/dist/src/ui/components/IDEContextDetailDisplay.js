import { jsxs as _jsxs, jsx as _jsx } from "react/jsx-runtime";
import { Box, Text } from 'ink';
import path from 'node:path';
import { Colors } from '../colors.js';
export function IDEContextDetailDisplay({ ideContext, detectedIdeDisplay, }) {
    const openFiles = ideContext?.workspaceState?.openFiles;
    if (!openFiles || openFiles.length === 0) {
        return null;
    }
    return (_jsxs(Box, { flexDirection: "column", marginTop: 1, borderStyle: "round", borderColor: Colors.AccentCyan, paddingX: 1, children: [_jsxs(Text, { color: Colors.AccentCyan, bold: true, children: [detectedIdeDisplay ? detectedIdeDisplay : 'IDE', " Context (ctrl+e to toggle)"] }), openFiles.length > 0 && (_jsxs(Box, { flexDirection: "column", marginTop: 1, children: [_jsx(Text, { bold: true, children: "Open files:" }), openFiles.map((file) => (_jsxs(Text, { children: ["- ", path.basename(file.path), file.isActive ? ' (active)' : ''] }, file.path)))] }))] }));
}
//# sourceMappingURL=IDEContextDetailDisplay.js.map