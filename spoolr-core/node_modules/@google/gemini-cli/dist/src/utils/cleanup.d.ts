/**
 * @license
 * Copyright 2025 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */
export declare function registerCleanup(fn: () => void): void;
export declare function runExitCleanup(): void;
export declare function cleanupCheckpoints(): Promise<void>;
