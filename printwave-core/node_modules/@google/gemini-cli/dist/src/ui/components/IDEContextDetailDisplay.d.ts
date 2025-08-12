/**
 * @license
 * Copyright 2025 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */
import { type IdeContext } from '@google/gemini-cli-core';
interface IDEContextDetailDisplayProps {
    ideContext: IdeContext | undefined;
    detectedIdeDisplay: string | undefined;
}
export declare function IDEContextDetailDisplay({ ideContext, detectedIdeDisplay, }: IDEContextDetailDisplayProps): import("react/jsx-runtime").JSX.Element | null;
export {};
