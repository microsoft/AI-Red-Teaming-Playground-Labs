// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface UsersState {
    users: Users;
}

export const initialState: UsersState = {
    users: {},
};

export type Users = Record<string, UserData>;

export interface UserData {
    id: string;
    displayName?: string;
    userPrincipalName?: string;
}
