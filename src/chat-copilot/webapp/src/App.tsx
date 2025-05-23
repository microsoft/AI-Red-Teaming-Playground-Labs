// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { Button, FluentProvider, Subtitle1, makeStyles, shorthands, tokens } from '@fluentui/react-components';

import { ArrowCircleLeft24Regular } from '@fluentui/react-icons';
import * as React from 'react';
import { useEffect } from 'react';
import { DescriptionDialog } from './components/chat/DescriptionDialog';
import { UserSettingsMenu } from './components/header/UserSettingsMenu';
import { PluginGallery } from './components/open-api-plugins/PluginGallery';
import { BackendProbe, ChatView, Error, Loading, Login } from './components/views';
import { AuthHelper } from './libs/auth/AuthHelper';
import { useChat, useFile } from './libs/hooks';
import { AlertType } from './libs/models/AlertType';
import { useAppDispatch, useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { Feature, FeatureKeys } from './redux/features/app/AppState';
import { addAlert, setActiveUserInfo, setServiceInfo } from './redux/features/app/appSlice';
import { ConversationsState } from './redux/features/conversations/ConversationsState';
import { semanticKernelDarkTheme, semanticKernelLightTheme } from './styles';

export const useClasses = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        height: '100vh',
        width: '100%',
        ...shorthands.overflow('hidden'),
    },
    header: {
        alignItems: 'center',
        backgroundColor: tokens.colorBrandForeground2,
        color: tokens.colorNeutralForegroundOnBrand,
        display: 'flex',
        '& h1': {
            display: 'flex',
        },
        height: '48px',
        justifyContent: 'space-between',
        width: '100%',
    },
    persona: {
        marginRight: tokens.spacingHorizontalXXL,
    },
    cornerItems: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingHorizontalS),
    },
    headerContainer: {
        display: 'flex'
    }
});

enum AppState {
    ProbeForBackend,
    SettingUserInfo,
    ErrorLoadingChats,
    ErrorLoadingUserInfo,
    LoadingChats,
    Chat,
    SigningOut,
}

const App = () => {
    const classes = useClasses();

    const [appState, setAppState] = React.useState(AppState.ProbeForBackend);
    const [showDescription, setShowDescription] = React.useState(true);
    const dispatch = useAppDispatch();

    const { instance, inProgress } = useMsal();
    const { features, isMaintenance, challengeSettings } = useAppSelector((state: RootState) => state.app);
    const { conversations } = useAppSelector((state: RootState) => state);
    const isAuthenticated = useIsAuthenticated();

    const chat = useChat();
    const file = useFile();

    useEffect(() => {
        const key = `dontShowAgain-${challengeSettings.id}`;
        const result = localStorage.getItem(key);
        if (result && result === 'true') {
            setShowDescription(false);
        } else {
            setShowDescription(true);
        }

        //Update the title of webpage
        if (document.title !== challengeSettings.name && challengeSettings.name !== "") {
            document.title = challengeSettings.name + " - Chat Copilot";
        }
    }, [challengeSettings]);

    useEffect(() => {
        if (isMaintenance && appState !== AppState.ProbeForBackend) {
            setAppState(AppState.ProbeForBackend);
            return;
        }

        if (isAuthenticated && appState === AppState.SettingUserInfo) {
            const account = instance.getActiveAccount();
            if (!account) {
                setAppState(AppState.ErrorLoadingUserInfo);
            } else {
                dispatch(
                    setActiveUserInfo({
                        id: `${account.localAccountId}.${account.tenantId}`,
                        email: account.username, // username is the email address
                        username: account.name ?? account.username,
                    }),
                );

                // Privacy disclaimer for internal Microsoft users
                if (account.username.split('@')[1] === 'microsoft.com') {
                    dispatch(
                        addAlert({
                            message:
                                'By using Chat Copilot, you agree to protect sensitive data, not store it in chat, and allow chat history collection for service improvements. This tool is for internal use only.',
                            type: AlertType.Info,
                        }),
                    );
                }

                setAppState(AppState.LoadingChats);
            }
        }

        if ((isAuthenticated || !AuthHelper.isAuthAAD()) && appState === AppState.LoadingChats) {
            void Promise.all([
                // Load all chats from memory
                chat
                    .loadChats()
                    .then(() => {
                        setAppState(AppState.Chat);
                    })
                    .catch(() => {
                        setAppState(AppState.ErrorLoadingChats);
                    }),

                // Check if content safety is enabled
                file.getContentSafetyStatus(),

                // Load service information
                chat.getServiceInfo().then((serviceInfo) => {
                    if (serviceInfo) {
                        dispatch(setServiceInfo(serviceInfo));
                    }
                }),
            ]);
        }

        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [instance, inProgress, isAuthenticated, appState, isMaintenance]);

    const content = <Chat classes={classes} appState={appState} setAppState={setAppState} challengeName={challengeSettings.name} description={challengeSettings.description} features={features} showDescription={showDescription} backNavigation={challengeSettings.backNavigation}  conversations={conversations} />;
    return (
        <FluentProvider
            className="app-container"
            theme={features[FeatureKeys.DarkMode].enabled ? semanticKernelDarkTheme : semanticKernelLightTheme}
        >
            {AuthHelper.isAuthAAD() ? (
                <>
                    <UnauthenticatedTemplate>
                        <div className={classes.container}>
                            <div className={classes.header}>
                                <Subtitle1 as="h1">{challengeSettings.name}</Subtitle1>
                            </div>
                            {appState === AppState.SigningOut && <Loading text="Signing you out..." />}
                            {appState !== AppState.SigningOut && <Login />}
                        </div>
                    </UnauthenticatedTemplate>
                    <AuthenticatedTemplate>{content}</AuthenticatedTemplate>
                </>
            ) : (
                content
            )}
        </FluentProvider>
    );
};

const Chat = ({
    classes,
    appState,
    setAppState,
    challengeName,
    description,
    features,
    showDescription,
    backNavigation,
    conversations,
}: {
    classes: ReturnType<typeof useClasses>;
    appState: AppState;
    setAppState: (state: AppState) => void;
    challengeName: string;
    description: string;
    features: Record<FeatureKeys, Feature>;
    showDescription: boolean;
    backNavigation: boolean;
    conversations: ConversationsState;
}) => {
    const onBackendFound = React.useCallback(() => {
        setAppState(
            AuthHelper.isAuthAAD()
                ? // if AAD is enabled, we need to set the active account before loading chats
                AppState.SettingUserInfo
                : // otherwise, we can load chats immediately
                AppState.LoadingChats,
        );
    }, [setAppState]);

    const loadingChatText = () => {
        if (conversations.total === 0) {
            return "Loading chats...";
        }
        return `Loading chats... ${conversations.loadedCount}/${conversations.total}`;
    }

    return (
        <div className={classes.container}>
            <div className={classes.header}>
                <div className={classes.headerContainer}>
                    {backNavigation && (
                        <Button
                            style={{ color: 'white' }}
                            appearance="transparent"
                            onClick={() => { history.back()}}
                            icon={<ArrowCircleLeft24Regular color="white" />}>
                            Exit
                        </Button>
                    )}
                    <Subtitle1 as="h1">{challengeName}</Subtitle1>
                </div>
                {description !== "" && (
                    <>
                        <DescriptionDialog description={description} open={appState === AppState.Chat && showDescription} />
                    </>
                )}
                {appState > AppState.SettingUserInfo && (
                    <div className={classes.cornerItems}>
                        <div className={classes.cornerItems}>
                            {features[FeatureKeys.Plugins].enabled && (
                                <>
                                    <PluginGallery />
                                </>
                            )}
                            <UserSettingsMenu
                                setLoadingState={() => {
                                    setAppState(AppState.SigningOut);
                                }}
                            />
                        </div>
                    </div>
                )}

            </div>
            {appState === AppState.ProbeForBackend && <BackendProbe onBackendFound={onBackendFound} />}
            {appState === AppState.SettingUserInfo && (
                <Loading text={'Hang tight while we fetch your information...'} />
            )}
            {appState === AppState.ErrorLoadingUserInfo && (
                <Error text={'Unable to load user info. Please try signing out and signing back in.'} />
            )}
            {appState === AppState.ErrorLoadingChats && (
                <Error text={'Unable to load chats. Please try refreshing the page.'} />
            )}
            {appState === AppState.LoadingChats && <Loading text={loadingChatText()} />}
            {appState === AppState.Chat && <ChatView />}
        </div>
    );
};

export default App;
