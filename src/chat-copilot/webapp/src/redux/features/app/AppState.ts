// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { AuthConfig } from '../../../libs/auth/AuthHelper';
import { AlertType } from '../../../libs/models/AlertType';
import { ChallengeSettings } from '../../../libs/models/ChallengeSettings';
import { IChatUser } from '../../../libs/models/ChatUser';
import { ServiceInfo } from '../../../libs/models/ServiceInfo';
import { TokenUsage } from '../../../libs/models/TokenUsage';

// This is the default user information when authentication is set to 'None'.
// It must match what is defined in PassthroughAuthenticationHandler.cs on the backend.
export const DefaultChatUser: IChatUser = {
    id: 'c05c61eb-65e4-4223-915a-fe72b0c9ece1',
    emailAddress: 'user@contoso.com',
    fullName: 'Default User',
    online: true,
    isTyping: false,
};

export const DefaultActiveUserInfo: ActiveUserInfo = {
    id: DefaultChatUser.id,
    email: DefaultChatUser.emailAddress,
    username: DefaultChatUser.fullName,
};

export interface ActiveUserInfo {
    id: string;
    email: string;
    username: string;
}

export interface Alert {
    message: string;
    type: AlertType;
    id?: string;
    onRetry?: () => void;
}

export interface Feature {
    enabled: boolean; // Whether to show the feature in the UX
    label: string;
    inactive?: boolean; // Set to true if you don't want the user to control the visibility of this feature or there's no backend support
    description?: string;
}

export interface Setting {
    title: string;
    description?: string;
    features: FeatureKeys[];
    stackVertically?: boolean;
    learnMoreLink?: string;
}

export interface AppState {
    alerts: Alert[];
    activeUserInfo?: ActiveUserInfo;
    authConfig?: AuthConfig | null;
    tokenUsage: TokenUsage;
    features: Record<FeatureKeys, Feature>;
    settings: Setting[];
    serviceInfo: ServiceInfo;
    isMaintenance: boolean;
    challengeSettings: ChallengeSettings;
}

export enum FeatureKeys {
    DarkMode,
    SimplifiedExperience,
    PluginsPlannersAndPersonas,
    AzureContentSafety,
    AzureCognitiveSearch,
    BotAsDocs,
    MultiUserChat,
    RLHF, // Reinforcement Learning from Human Feedback

    //New features for challenges
    Documents,
    MetapromptLeak,
    Plugins,
    PluginsControl,
    HasHumanScorerOnly,
    PlanEdit,
    XssVulnerable,
    RagInput,
    BackNavigation
}

export const Features = {
    [FeatureKeys.DarkMode]: {
        enabled: false,
        label: 'Dark Mode',
    },
    [FeatureKeys.SimplifiedExperience]: {
        enabled: true,
        label: 'Simplified Chat Experience',
    },
    [FeatureKeys.PluginsPlannersAndPersonas]: {
        enabled: false,
        label: 'Plugins & Planners & Personas',
        description: 'The Plans and Persona tabs are hidden until you turn this on',
    },
    [FeatureKeys.AzureContentSafety]: {
        enabled: false,
        label: 'Azure Content Safety',
        inactive: true,
    },
    [FeatureKeys.AzureCognitiveSearch]: {
        enabled: false,
        label: 'Azure Cognitive Search',
        inactive: true,
    },
    [FeatureKeys.BotAsDocs]: {
        enabled: false,
        label: 'Export Chat Sessions',
    },
    [FeatureKeys.MultiUserChat]: {
        enabled: false,
        label: 'Live Chat Session Sharing',
        description: 'Enable multi-user chat sessions. Not available when authorization is disabled.',
    },
    [FeatureKeys.RLHF]: {
        enabled: false,
        label: 'Reinforcement Learning from Human Feedback',
        description: 'Enable users to vote on model-generated responses. For demonstration purposes only.',
        // TODO: [Issue #42] Send and store feedback in backend
    },
    //New features for challenges
    [FeatureKeys.Documents]: {
        enabled: false,
        label: 'Documents'
    },
    [FeatureKeys.MetapromptLeak]: {
        enabled: false,
        label: 'Leak the Metaprompt'
    },
    [FeatureKeys.Plugins]: { //Represent a UI to have the plugins enabled for it
        enabled: false,
        label: 'Plugins'
    },
    [FeatureKeys.PluginsControl]: {
        enabled: false,
        label: 'Turn on/off Plugins'
    },
    [FeatureKeys.HasHumanScorerOnly]: {
        enabled: false,
        label: 'Has Human Scorer Only. Will display a button to send the chat for review'
    },
    [FeatureKeys.PlanEdit]: {
        enabled: true,
        label: 'Plan Edit - Allow users to edit the plan'
    },
    [FeatureKeys.XssVulnerable]: {
        enabled: false,
        label: 'Is the application vulnerable to XSS?'
    },
    [FeatureKeys.RagInput]: {
        enabled: false,
        label: 'RAG Input - Allow users to input RAG values for the chat in a separate tab.'
    },
    [FeatureKeys.BackNavigation]: {
        enabled: false,
        label: 'Allow the user to navigate back to the previous page.',
    },
};

export const Settings = [
    {
        // Basic settings has to stay at the first index. Add all new settings to end of array.
        title: 'Basic',
        features: [FeatureKeys.DarkMode],
        stackVertically: true,
    },
    {
        title: 'Display',
        features: [FeatureKeys.SimplifiedExperience],
        stackVertically: true,
    }
];

export const initialState: AppState = {
    alerts: [],
    activeUserInfo: DefaultActiveUserInfo,
    authConfig: {} as AuthConfig,
    tokenUsage: {},
    features: Features,
    settings: Settings,
    serviceInfo: {
        memoryStore: { types: [], selectedType: '' },
        availablePlugins: [],
        version: '',
        isContentSafetyEnabled: false,
    },
    challengeSettings: {
        id: 0,
        name: "",
        description: "",
        fileUpload: false,
        metapromptLeak: false,
        plugins: false,
        pluginsControl: false,
        humanScorer: false,
        autoScorer: false,
        planEdit: true,
        xssVulnerable: false,
        backNavigation: false,
        ragInput: {
            enabled: false,
            document: "",
            template: "",
            isReadOnly: false,
            titleShort: "",
            titleLong: "",
            instruction1: "",
            instruction2: "",
            firstMessage: "",
            maxTurns: 0
        }
    },
    isMaintenance: false,
};


//Xss state is needed and is tracked outside of the app state to avoid issues with the redux store
export interface XssState{
    xssCalled: Set<string>
    current_chat_id: string
}

export const xssState: XssState = {
    xssCalled: new Set<string>(),
    current_chat_id: ""
}