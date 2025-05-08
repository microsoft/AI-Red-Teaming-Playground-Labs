// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { initialState, Users, UsersState } from './UsersState';

export const usersSlice = createSlice({
    name: 'users',
    initialState,
    reducers: {
        setUsers: (state: UsersState, action: PayloadAction<Users>) => {
            state.users = action.payload;
        },
    },
});

export const { setUsers } = usersSlice.actions;

export default usersSlice.reducer;
