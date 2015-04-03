// Copyright eeGeo Ltd (2012-2015), All Rights Reserved

#pragma once

#include <jni.h>
#include "AndroidNativeState.h"
#include "InitialExperienceModuleBase.h"
#include "Types.h"
#include "BidirectionalBus.h"
#include "Menu.h"
#include "SearchResultMenu.h"

namespace ExampleApp
{
    namespace InitialExperience
    {
        namespace SdkModel
        {
            class AndroidInitialExperienceModule : public InitialExperienceModuleBase, private Eegeo::NonCopyable
            {
                AndroidNativeState& m_nativeState;
                ExampleAppMessaging::TMessageBus& m_messageBus;

            public:
                AndroidInitialExperienceModule(
                    AndroidNativeState& m_nativeState,
                    PersistentSettings::IPersistentSettingsModel& persistentSettings,
                    ExampleAppMessaging::TMessageBus& messageBus
                );

                ~AndroidInitialExperienceModule();

            protected:

                std::vector<IInitialExperienceStep*> CreateSteps(WorldAreaLoader::SdkModel::IWorldAreaLoaderModel& worldAreaLoaderModel,
                        Menu::View::IMenuViewModel& searchMenuViewModelControl,
                        SearchResultMenu::View::ISearchResultMenuViewModel& searchResultMenuViewModel) const;
            };
        }
    }
}
