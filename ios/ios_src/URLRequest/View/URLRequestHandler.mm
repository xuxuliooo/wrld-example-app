// Copyright eeGeo Ltd (2012-2015), All Rights Reserved

#include "URLRequestHandler.h"


namespace ExampleApp
{
    namespace URLRequest
    {
        namespace View
        {
            URLRequestHandler::URLRequestHandler(ExampleAppMessaging::TMessageBus& messageBus)
            : m_messageBus(messageBus)
            , m_urlRequestedCallback(this, &URLRequestHandler::OnURLRequested)
            , m_deeplinkUrlRequestedCallback(this, &URLRequestHandler::OnDeeplinkURLRequested)
            {
                m_messageBus.SubscribeUi(m_urlRequestedCallback);
                m_messageBus.SubscribeUi(m_deeplinkUrlRequestedCallback);
            }
            
            URLRequestHandler::~URLRequestHandler()
            {
                m_messageBus.UnsubscribeUi(m_deeplinkUrlRequestedCallback);
                m_messageBus.UnsubscribeUi(m_urlRequestedCallback);
            }
            
            void URLRequestHandler::OnURLRequested(const ExampleApp::URLRequest::URLRequestedMessage &message)
            {
                RequestExternalURL(message.URL());
            }
            
            void URLRequestHandler::OnDeeplinkURLRequested(const DeeplinkURLRequestedMessage& message)
            {
                RequestDeeplinkURL(message.DeeplinkURL(), message.HttpFallbackURL());
            }
            
            void URLRequestHandler::RequestExternalURL(const std::string& url)
            {
                NSString *urlLink = [NSString stringWithUTF8String:url.c_str()];
                NSString *escaped = [urlLink stringByAddingPercentEscapesUsingEncoding:NSUTF8StringEncoding];
                [[UIApplication sharedApplication] openURL:[NSURL URLWithString:escaped]];
            }
            
            void URLRequestHandler::RequestDeeplinkURL(const std::string& deeplinkUrl, const std::string& httpFallbackUrl)
            {
                std::size_t pos = deeplinkUrl.find("://");
                std::string deeplinkProtocol = deeplinkUrl.substr(0,pos);
                NSString *protocol = [NSString stringWithUTF8String:(deeplinkProtocol+"://") .c_str()];
                if([[UIApplication sharedApplication] canOpenURL:[NSURL URLWithString:protocol]] == YES)
                {
                    RequestExternalURL(deeplinkUrl);
                }
                else
                {
                    RequestExternalURL(httpFallbackUrl);
                }
            }
        }
    }
}