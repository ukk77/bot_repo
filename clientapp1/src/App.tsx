import React from 'react';
import { Stack, Text, Link, FontWeights } from 'office-ui-fabric-react';

import { PivotBasicExample } from './navdoc';

import logo from './fabric.png';

const boldStyle = {
  root: { fontWeight: FontWeights.semibold }
};

export const App: React.FunctionComponent = () => {
    return (      
            <PivotBasicExample></PivotBasicExample>
        )
};
