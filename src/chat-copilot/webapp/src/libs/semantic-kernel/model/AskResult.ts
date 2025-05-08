// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IChatMessage } from '../../models/ChatMessage';

export interface IAskResult {
    message: IChatMessage;
    variables: ContextVariable[];
}

export interface ContextVariable {
    key: string;
    value: string;
}
