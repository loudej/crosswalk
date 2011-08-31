
#pragma once

#include "HttpModule.h"

class HttpModuleFactory : public IHttpModuleFactory
{
    ICrosswalkModulePtr _crosswalkModule;
    HTTP_MODULE_ID _moduleId;
    IHttpServer* _httpServer;

public:
    HttpModuleFactory(ICrosswalkModule* crosswalkModule, HTTP_MODULE_ID moduleId, IHttpServer* httpServer)
    {
        _crosswalkModule = crosswalkModule;
        _moduleId = moduleId;
        _httpServer = httpServer;
    }

    HRESULT GetHttpModule(
        OUT CHttpModule**          ppModule, 
        IN  IModuleAllocator*      pAllocator)
    {
        HRESULT hr = S_OK;

        IHttpTransactionPtr httpModule = new ComObject<HttpModule>();
        _HR(httpModule->Initialize(_crosswalkModule, _moduleId, _httpServer));
        if (SUCCEEDED(hr))
        {
            IHttpTransaction* p = httpModule.Detach();
            *ppModule = static_cast<HttpModule*>(p);
            int x = 5;
        }

        return hr;
    }

    void Terminate()
    {
        delete this;
    }
};

