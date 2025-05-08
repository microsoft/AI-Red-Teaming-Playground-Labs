// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button } from '@fluentui/react-components';
import { FormEvent } from 'react';
import PluginIcon from '../../../assets/plugin-icons/add-plugin.png';
import { usePlugins } from '../../../libs/hooks/usePlugins';
import { AlertType } from '../../../libs/models/AlertType';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { FeatureKeys } from '../../../redux/features/app/AppState';
import { addAlert } from '../../../redux/features/app/appSlice';
import { Plugin } from '../../../redux/features/plugins/PluginsState';
import { disconnectPlugin } from '../../../redux/features/plugins/pluginsSlice';
import { PluginConnector } from '../PluginConnector';
import { BaseCard } from './BaseCard';

interface PluginCardProps {
    plugin: Plugin;
    isHosted: boolean;
}

export const PluginCard: React.FC<PluginCardProps> = ({ plugin, isHosted }) => {
    const { name, publisher, enabled, authRequirements, apiProperties, description } = plugin;
    const dispatch = useAppDispatch();

    const { setPluginStateAsync } = usePlugins();
    const { features } = useAppSelector((state: RootState) => state.app);
    const { selectedId} = useAppSelector((state: RootState) => state.conversations);

    const onDisconnectClick = (event: FormEvent) => {
        event.preventDefault();
        if (!isHosted) {
            dispatch(disconnectPlugin(name));
        } else {
            setPluginStateAsync(selectedId, name, false)
                .then(() => {
                    dispatch(addAlert({ message: `${name} disabled!`, type: AlertType.Success }));
                })
                .catch((error: Error) => {
                    dispatch(addAlert({ message: error.message, type: AlertType.Error }));
                });
        }
    };

    return (
        <BaseCard
            image={PluginIcon}
            header={`${name}`}
            secondaryText={`${enabled ? 'Enabled':'Disabled'}`}
            description={description}
            action={
                enabled ? (
                    <Button
                        data-testid="disconnectPluginButton"
                        aria-label="Disconnect plugin"
                        appearance="secondary"
                        onClick={onDisconnectClick}
                        disabled={!features[FeatureKeys.PluginsControl].enabled}
                    >
                        Disable
                    </Button>
                ) : (
                    features[FeatureKeys.PluginsControl].enabled ? (
                       <PluginConnector
                            name={name}
                            icon={PluginIcon}
                            publisher={publisher}
                            authRequirements={authRequirements}
                            apiProperties={apiProperties}
                            isHosted={isHosted}
                            />
                    ) : (
                        <Button
                            data-testid="connectPluginButton"
                            aria-label="Connect plugin"
                            appearance="secondary"
                            disabled
                        >
                            Enable
                        </Button>
                    )
                )
            }
        />
    );
};
