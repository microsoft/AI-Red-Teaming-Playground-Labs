// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface IAsk {
    input: string;
    variables?: IAskVariables[];
}

export interface IAskVariables {
    key: string;
    value: string;
}
