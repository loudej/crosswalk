
#pragma once

#include "CriticalSection.h"

class HttpApplicationStoredContext : IHttpStoredContext
{
    ICrosswalkModulePtr _crosswalkModule;
    IHttpApplication* _httpApplication;
    CriticalSection _crit;
    
    BOOL _calledInitializeAppDomain;
    HRESULT _hrInitializeAppDomain;

    DWORD _domainId;
    AppHandlerInfo::BindHandlerDelegate _bindHandler;


    struct cmp_PCWSTR
    {
       bool operator()(PCWSTR a, PCWSTR b)
       {
          return CompareStringOrdinal(a, -1,  b, -1, FALSE) == CSTR_LESS_THAN;
       }
    };


    typedef std::map<PCWSTR, ExecuteHandlerContext::ExecuteHandlerDelegate, cmp_PCWSTR> ExecuteHandlerMap;
    ExecuteHandlerMap _executeHandlers;

    HttpApplicationStoredContext(ICrosswalkModule* crosswalkModule, IHttpApplication* httpApplication)
    {
        _crosswalkModule = crosswalkModule;
        _httpApplication = httpApplication;
        _calledInitializeAppDomain = FALSE;
        _domainId = 0;
        _bindHandler = NULL;
    }
    
    ~HttpApplicationStoredContext()
    {
        Lock lock(&_crit);
        for(ExecuteHandlerMap::const_iterator i = _executeHandlers.begin();
            i != _executeHandlers.end();
            ++i)
        {
            SysFreeString(const_cast<BSTR>(i->first));
        }
        _executeHandlers.clear();
    }

public:
    static HRESULT Get(
        ICrosswalkModule* crosswalkModule,
        IHttpApplication* httpApplication, 
        HttpApplicationStoredContext** ppStoredContext)
    {
        HRESULT hr = S_OK;
        
        HTTP_MODULE_ID moduleId = crosswalkModule->GetModuleId();
        IHttpModuleContextContainer* container =  httpApplication->GetModuleContextContainer();
        IHttpStoredContext* storedContext = container->GetModuleContext(moduleId);
        if (storedContext == NULL)
        {
            storedContext = new HttpApplicationStoredContext(crosswalkModule, httpApplication);

            hr = container->SetModuleContext(storedContext, moduleId);
            if (hr == HRESULT_FROM_WIN32( ERROR_ALREADY_ASSIGNED))
            {
                delete storedContext;

                hr = S_OK;
                storedContext = container->GetModuleContext(moduleId);
            }
        }

        *ppStoredContext = static_cast<HttpApplicationStoredContext*>(storedContext);
        return hr;
    }

    void CleanupStoredContext()
    {
        TerminateAppDomain();
        delete this;
    }

    HRESULT InitializeAppDomain()
    {
        Lock lock(&_crit);
        if (_calledInitializeAppDomain)
            return _hrInitializeAppDomain;

        HRESULT hr = S_OK;
        
        PCWSTR applicationPhysicalPath = _httpApplication->GetApplicationPhysicalPath();
        PCWSTR appConfigPath = _httpApplication->GetAppConfigPath();
        PCWSTR applicationId = _httpApplication->GetApplicationId();
        
        _HR(_crosswalkModule->CreateAppDomain(
            applicationPhysicalPath, 
            applicationId, 
            appConfigPath,
            &_domainId,
            &_bindHandler));

        _hrInitializeAppDomain = hr;
        _calledInitializeAppDomain = TRUE;
        return hr;
    }

    HRESULT TerminateAppDomain()
    {
        HRESULT hr = S_OK;
        if (_domainId != 0)
        {
            _HR(_crosswalkModule->UnloadAppDomain(_domainId));
            _domainId = 0;
        }
        return hr;
    }

    HRESULT InitializeHandler(
        IScriptMapInfo* scriptMap, 
        ExecuteHandlerContext::ExecuteHandlerDelegate* ppfnExecuteHandler)
    {
        HRESULT hr = S_OK;
        _HR_OUT(ppfnExecuteHandler);

        _HR(InitializeAppDomain());

        PCWSTR name = scriptMap->GetName();

        Lock lock(&_crit);
        ExecuteHandlerMap::const_iterator found = _executeHandlers.find(name);
        if (found != _executeHandlers.end())
        {
            *ppfnExecuteHandler = found->second;
        }
        else
        {
            BindHandlerContext bind = {0};
            bind.Name = scriptMap->GetName();
            bind.ManagedType = scriptMap->GetManagedType();
            bind.ScriptProcessor = scriptMap->GetScriptProcessor();
            _bindHandler(&bind, ppfnExecuteHandler);
            _executeHandlers[SysAllocString(name)] = *ppfnExecuteHandler;
        }

        return hr;
    }
};

