import path from "path";
import fs from "fs";
import {namedHookWithTaskFn, PluginBase} from '@electron-forge/plugin-base';
import {ForgeMultiHookMap, ResolvedForgeConfig} from "@electron-forge/shared-types";
import debug from "debug";

interface RemoveNodeModulesFoldersPluginOptions {
    foldersToRemove: string[];
}

const d = debug('electron-forge:plugin:removeNodeModulesFoldersPlugin');

export default class RemoveNodeModulesFoldersPlugin extends PluginBase<RemoveNodeModulesFoldersPluginOptions> {
    name: 'remove-node-modules-folders-plugin';
    init(_dir: string, _config: ResolvedForgeConfig) {
        this.setDirectories(_dir);
    }
    // Where the node_modules
    private modulesDir!: string;
    private setDirectories(dir: string): void {
        this.modulesDir = path.join(dir, 'node_modules');
    }

    override getHooks(): ForgeMultiHookMap {
        return {
            postPackage: [
                namedHookWithTaskFn<'postPackage'>(async (task, forgeConfig, hookConfig) => {
                    for (const outputPath of hookConfig.outputPaths) {
                        for (const folderName of this.config.foldersToRemove) {
                            const folderPath = path.join(outputPath, 'resources', 'app', 'node_modules', folderName);
                            try {
                                if (fs.existsSync(folderPath)){
                                    fs.rmSync(folderPath, { recursive: true });
                                    d(`Removed folder: ${folderPath}`);
                                }
                            } catch (err) {
                                d(`Error removing folder ${folderPath}: ${err}`);
                            }
                        }
                    }

                }, 'Removing unneeded packages')
            ]
        }
    }

}
