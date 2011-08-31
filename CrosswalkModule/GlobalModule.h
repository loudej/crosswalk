
#pragma once

#include "CrosswalkModule.h"
#include "HttpApplicationStoredContext.h"

class GlobalModule : public CGlobalModule
{
    ICrosswalkModulePtr _crosswalkModule;
    HTTP_MODULE_ID _moduleId;
    IHttpServer* _httpServer;

public:
    GlobalModule(ICrosswalkModule* crosswalkModule, HTTP_MODULE_ID moduleId, IHttpServer* httpServer)
    {
        _crosswalkModule = crosswalkModule;
        _moduleId = moduleId;
        _httpServer = httpServer;
    }

    void Terminate()
    {
        delete this;
    }

    // GL_APPLICATION_START 
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationStart(
        IN IHttpApplicationStartProvider  *  pProvider)
    {
        UNREFERENCED_PARAMETER( pProvider );

        HRESULT hr = S_OK;

       /* _HR(_crosswalkModule->InitializeRuntime());

        IHttpApplication* httpApplication = pProvider->GetApplication();
        
        PCWSTR applicationPhysicalPath = httpApplication->GetApplicationPhysicalPath();
        PCWSTR appConfigPath = httpApplication->GetAppConfigPath();
        PCWSTR applicationId = httpApplication->GetApplicationId();

        _HR(_crosswalkModule->CreateAppDomain(applicationPhysicalPath, applicationId, appConfigPath));
*/
        if (FAILED(hr))
            pProvider->SetErrorStatus(hr);

        return GL_NOTIFICATION_CONTINUE;
    }


    // GL_APPLICATION_STOP

    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationStop(
        IN IHttpApplicationStopProvider *   pProvider)
    {
        UNREFERENCED_PARAMETER( pProvider );

        HRESULT hr = S_OK;

        HttpApplicationStoredContext* applicationContext = NULL;
        _HR(HttpApplicationStoredContext::Get(_crosswalkModule, pProvider->GetApplication(), &applicationContext));
        _HR(applicationContext->TerminateAppDomain());

        if (FAILED(hr))
        {
            pProvider->SetErrorStatus(hr);
        }

        return GL_NOTIFICATION_CONTINUE;
    }
};


