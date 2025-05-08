// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { FluentProvider, teamsLightTheme } from '@fluentui/react-components';

import App from './App';

import './App.css';

import { createRoot } from 'react-dom/client';
const container = document.getElementById('root');
const root = createRoot(container!);

root.render(
    <FluentProvider theme={teamsLightTheme}>
    <App />
  </FluentProvider>
)