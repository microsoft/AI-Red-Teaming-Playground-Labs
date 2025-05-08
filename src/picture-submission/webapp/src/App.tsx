// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { FluentProvider, makeStyles, tokens } from '@fluentui/react-components';
import './App.css';
import { Header } from './components/Header';
import { MainView } from './components/MainView';
import { semanticKernelLightTheme } from './styles';


export const useClasses = makeStyles({
  root: {
    backgroundColor: tokens.colorNeutralBackground4,
  }
})


function App() {
  const classes = useClasses();
  return (
    <FluentProvider theme={semanticKernelLightTheme}>
      <div className={"App " + classes.root}>
        <Header></Header>
        <div className='main-app'>
          <MainView></MainView>
        </div>
      </div>
    </FluentProvider>
  );
}

export default App;
