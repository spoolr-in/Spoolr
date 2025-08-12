/**
 * @license
 * Copyright 2025 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */
import { DetectedIde, IDEConnectionStatus, getIdeDisplayName, getIdeInstaller, } from '@google/gemini-cli-core';
import { CommandKind, } from './types.js';
import { SettingScope } from '../../config/settings.js';
function getIdeStatusMessage(ideClient) {
    const connection = ideClient.getConnectionStatus();
    switch (connection.status) {
        case IDEConnectionStatus.Connected:
            return {
                messageType: 'info',
                content: `🟢 Connected to ${ideClient.getDetectedIdeDisplayName()}`,
            };
        case IDEConnectionStatus.Connecting:
            return {
                messageType: 'info',
                content: `🟡 Connecting...`,
            };
        default: {
            let content = `🔴 Disconnected`;
            if (connection?.details) {
                content += `: ${connection.details}`;
            }
            return {
                messageType: 'error',
                content,
            };
        }
    }
}
export const ideCommand = (config) => {
    if (!config || !config.getIdeModeFeature()) {
        return null;
    }
    const ideClient = config.getIdeClient();
    const currentIDE = ideClient.getCurrentIde();
    if (!currentIDE || !ideClient.getDetectedIdeDisplayName()) {
        return {
            name: 'ide',
            description: 'manage IDE integration',
            kind: CommandKind.BUILT_IN,
            action: () => ({
                type: 'message',
                messageType: 'error',
                content: `IDE integration is not supported in your current environment. To use this feature, run Gemini CLI in one of these supported IDEs: ${Object.values(DetectedIde)
                    .map((ide) => getIdeDisplayName(ide))
                    .join(', ')}`,
            }),
        };
    }
    const ideSlashCommand = {
        name: 'ide',
        description: 'manage IDE integration',
        kind: CommandKind.BUILT_IN,
        subCommands: [],
    };
    const statusCommand = {
        name: 'status',
        description: 'check status of IDE integration',
        kind: CommandKind.BUILT_IN,
        action: () => {
            const { messageType, content } = getIdeStatusMessage(ideClient);
            return {
                type: 'message',
                messageType,
                content,
            };
        },
    };
    const installCommand = {
        name: 'install',
        description: `install required IDE companion for ${ideClient.getDetectedIdeDisplayName()}`,
        kind: CommandKind.BUILT_IN,
        action: async (context) => {
            const installer = getIdeInstaller(currentIDE);
            if (!installer) {
                context.ui.addItem({
                    type: 'error',
                    text: `No installer is available for ${ideClient.getDetectedIdeDisplayName()}. Please install the IDE companion manually from its marketplace.`,
                }, Date.now());
                return;
            }
            context.ui.addItem({
                type: 'info',
                text: `Installing IDE companion...`,
            }, Date.now());
            const result = await installer.install();
            if (result.success) {
                config.setIdeMode(true);
                context.services.settings.setValue(SettingScope.User, 'ideMode', true);
            }
            context.ui.addItem({
                type: result.success ? 'info' : 'error',
                text: result.message,
            }, Date.now());
        },
    };
    const enableCommand = {
        name: 'enable',
        description: 'enable IDE integration',
        kind: CommandKind.BUILT_IN,
        action: async (context) => {
            context.services.settings.setValue(SettingScope.User, 'ideMode', true);
            await config.setIdeModeAndSyncConnection(true);
            const { messageType, content } = getIdeStatusMessage(ideClient);
            context.ui.addItem({
                type: messageType,
                text: content,
            }, Date.now());
        },
    };
    const disableCommand = {
        name: 'disable',
        description: 'disable IDE integration',
        kind: CommandKind.BUILT_IN,
        action: async (context) => {
            context.services.settings.setValue(SettingScope.User, 'ideMode', false);
            await config.setIdeModeAndSyncConnection(false);
            const { messageType, content } = getIdeStatusMessage(ideClient);
            context.ui.addItem({
                type: messageType,
                text: content,
            }, Date.now());
        },
    };
    const ideModeEnabled = config.getIdeMode();
    if (ideModeEnabled) {
        ideSlashCommand.subCommands = [
            disableCommand,
            statusCommand,
            installCommand,
        ];
    }
    else {
        ideSlashCommand.subCommands = [
            enableCommand,
            statusCommand,
            installCommand,
        ];
    }
    return ideSlashCommand;
};
//# sourceMappingURL=ideCommand.js.map