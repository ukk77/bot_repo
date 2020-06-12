import * as React from 'react';
import { Label, ILabelStyles } from 'office-ui-fabric-react/lib/Label';
import { Pivot, PivotItem } from 'office-ui-fabric-react/lib/Pivot';
import { IStyleSet } from 'office-ui-fabric-react/lib/Styling';
import { TeachingBubbleBasicExample } from './file';

const labelStyles: Partial<IStyleSet<ILabelStyles>> = {
    root: { marginTop: 10, marginLeft: 10, background: "linear-gradient(to bottom right, white, green)", height : 720},
};

export const PivotBasicExample: React.FunctionComponent = () => {
  return (
    <Pivot aria-label="Basic Pivot Example">
      <PivotItem
        headerText="Bot Page"
        headerButtonProps={{
          'data-order': 1,
          'data-title': 'My Files Title',
        }}
      >
              <Label styles={labelStyles}>
                  <TeachingBubbleBasicExample></TeachingBubbleBasicExample>
              </Label>
      </PivotItem>
    </Pivot>
  );
};