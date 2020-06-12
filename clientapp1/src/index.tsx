import React from 'react';
import ReactDOM from 'react-dom';
import { App } from './App';
import { mergeStyles, getBackgroundShade } from 'office-ui-fabric-react';

// Inject some global styles
mergeStyles({
  selectors: {
    ':global(body), :global(html), :global(#app)': {
      margin: 0,
      padding: 0
      }
    },
});

ReactDOM.render(
    <div style={{ }}>
        <App />
    </div>,

    document.getElementById('app'));
