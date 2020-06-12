import * as React from 'react';
import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';
import { TeachingBubble } from 'office-ui-fabric-react/lib/TeachingBubble';
import { useBoolean } from '@uifabric/react-hooks';
import Iframe from 'react-iframe'


export const TeachingBubbleBasicExample: React.FunctionComponent = () => {
    const [teachingBubbleVisible, { toggle: toggleTeachingBubbleVisible }] = useBoolean(false);

    return (
        <div>
            <DefaultButton
                id="targetButton"
                onClick={toggleTeachingBubbleVisible}
                text={teachingBubbleVisible ? 'Hide Bot' : 'Show Bot'}
            />

            {teachingBubbleVisible && (
                <TeachingBubble
                    target="#targetButton"
                    onDismiss={toggleTeachingBubbleVisible}
                    headline="Helpdesk bot"
                >
                    <Iframe url='https://webchat.botframework.com/embed/qnasrv-bot?s=WHVoH8p8su8.aPUC49SVwUbiGD_cu_nJhT9q3xyLSKoVotwriQWEO6E'
                        title="Helpdesk support"
                        width="300px"
                        height="300px"
                    />
                </TeachingBubble>
            )}
        </div>
    );
};