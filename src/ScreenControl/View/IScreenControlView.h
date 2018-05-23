// Copyright eeGeo Ltd (2012-2015), All Rights Reserved

#pragma once

namespace ExampleApp
{
    namespace ScreenControl
    {
        namespace View
        {
            typedef float IScreenControlViewPosition;
            class IScreenControlView
            {
            public:
                virtual ~IScreenControlView() { };

                virtual void SetOnScreenStateToIntermediateValue(float value) = 0;
                virtual void SetFullyOnScreen() = 0;
                virtual void SetFullyOffScreen() = 0;
            };
            class IMovableScreenControlView : public virtual IScreenControlView
            {
            public:
                virtual void SetOnScreenPosition(IScreenControlViewPosition position) = 0;
            };
        }
    }
}
