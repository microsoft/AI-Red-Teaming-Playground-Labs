// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body2, Title1 } from "@fluentui/react-text";
import { SortControlled } from "./table";

const styles: React.CSSProperties = {
        marginTop: "var(--spacingVerticalS)",
      };

function App() {
  return (
    <div className="App">
      <div className="container">
        <Title1>AI Red Teaming Playground Labs</Title1>
        <div style={styles}>
          <Body2>Welcome to the AI Red Teaming Playground Labs. You will find below the challenges that are available. You can try a challenge and come back here once the challenge is completed.</Body2>
        </div>
        <SortControlled></SortControlled>
      </div>
    </div>
  );
}

export default App;
