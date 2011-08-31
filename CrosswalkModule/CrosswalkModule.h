
#pragma once

#include "FileStream.h"
#include "CriticalSection.h"

#include <wpframework.h>

struct AppPoolInfo
{
    typedef int (*CreateAppDomainDelegate)(PCWSTR applicationPhysicalPath, PCWSTR applicationId,  PCWSTR appConfigPath);
    typedef void (*UnloadAppDomainDelegate)(DWORD domainId);

    /* in */ CreateAppDomainDelegate CreateAppDomain;
    /* in */ UnloadAppDomainDelegate UnloadAppDomain;

    /* out */ BSTR AppPoolName;
    /* out */ BSTR ClrConfigFile;
};


struct ExecuteHandlerContext
{
    typedef void (*ExecuteHandlerDelegate)(
        IUnknown* transaction,
        PCSTR* knownServerVariables,
        PCSTR* knownRequestHeaders,
        PCSTR* unknownRequestHeaderNames,
        int unknownRequestHeaderNamesCount,
        PCSTR* unknownRequestHeaderValues,
        int unknownRequestHeaderValuesCount);

    typedef void (*ContinuationDelegate)();

    IUnknown* transaction;
};

struct BindHandlerContext
{
    /* in */ PCWSTR Name;
    /* in */ PCWSTR ManagedType;
    /* in */ PCWSTR ScriptProcessor;
};

struct AppHandlerInfo
{
    typedef void (*BindHandlerDelegate)(BindHandlerContext* context,
        ExecuteHandlerContext::ExecuteHandlerDelegate* ppfnExecuteHandler);

    /* in */ BindHandlerDelegate BindHandler;
};




class __declspec(uuid("3FD03887-31D0-49CF-AC17-1477527E1C73")) ICrosswalkModule : public IUnknown
{
public:
    virtual HTTP_MODULE_ID GetModuleId() = 0;

    virtual HRESULT Initialize(HTTP_MODULE_ID moduleId, IHttpServer* httpServer) = 0;
    virtual HRESULT BindAppPoolInfo(AppPoolInfo* pInfo) = 0;
    virtual HRESULT BindAppHandlerInfo(AppHandlerInfo* pInfo) = 0;
    virtual HRESULT InitializeRuntime() = 0;

    virtual HRESULT CreateAppDomain(
        /*in*/ PCWSTR applicationPhysicalPath, 
        /*in*/ PCWSTR applicationId,  
        /*in*/ PCWSTR appConfigPath,
        /*out*/ DWORD* pdwDomainId,
        /*out*/ AppHandlerInfo::BindHandlerDelegate* ppfnBindHandler) = 0;

    virtual HRESULT UnloadAppDomain(DWORD dwDomainId) = 0;
};

_COM_SMARTPTR_TYPEDEF(ICrosswalkModule, __uuidof(ICrosswalkModule));


class CrosswalkModule : 
    public ICrosswalkModule,
    public IHostControl
{
    HTTP_MODULE_ID _moduleId;
    IHttpServer* _httpServer;

    CriticalSection _crit;

    BOOL _calledResolveAppPoolInfo;
    HRESULT _hrResolveAppPoolInfo;
    _bstr_t _clrVersion;
    _bstr_t _appPoolName;
    _bstr_t _appHostFileName;
    _bstr_t _rootWebConfigFileName;
    _bstr_t _clrConfigFileName;

    BOOL _calledInitializeRuntime;
    HRESULT _hrInitializeRuntime;
    ICLRMetaHostPtr _MetaHost;
    ICLRMetaHostPolicyPtr _MetaHostPolicy;
    ICLRRuntimeHostPtr _RuntimeHost;


    AppPoolInfo::CreateAppDomainDelegate _createAppDomain;
    AppPoolInfo::UnloadAppDomainDelegate _unloadAppDomain;


    CriticalSection _createAppDomain_Crit;
    AppHandlerInfo::BindHandlerDelegate _createAppDomain_BindHandler;


public:
    CrosswalkModule()
    {
        _createAppDomain = NULL;
        _unloadAppDomain = NULL;
        _createAppDomain_BindHandler = NULL;

        _calledResolveAppPoolInfo = FALSE;
        _calledInitializeRuntime = FALSE;
    }

    IUnknown* CastInterface(REFIID riid)
    {
        if (riid == __uuidof(ICrosswalkModule))
            return static_cast<ICrosswalkModule*>(this);
        if (riid == __uuidof(IHostControl))
            return static_cast<IHostControl*>(this);
        return NULL;
    }

    HTTP_MODULE_ID GetModuleId()
    {
        return _moduleId;
    }

    HRESULT Initialize(HTTP_MODULE_ID moduleId, IHttpServer* httpServer)
    {
        HRESULT hr = S_OK;

        _moduleId = moduleId;
        _httpServer = httpServer;

        return hr;
    }

    
    HRESULT BindAppPoolInfo(AppPoolInfo* pInfo)
    {
        HRESULT hr = S_OK;

        _createAppDomain = pInfo->CreateAppDomain;
        _unloadAppDomain = pInfo->UnloadAppDomain;

        _HR(ResolveAppPoolInfo());

        pInfo->AppPoolName = _appPoolName.copy();
        pInfo->ClrConfigFile = _clrConfigFileName.copy();

        return hr;
    }
    
    HRESULT BindAppHandlerInfo(AppHandlerInfo* pInfo)
    {
        HRESULT hr = S_OK;

        _createAppDomain_BindHandler = pInfo->BindHandler;

        return hr;
    }

    HRESULT ResolveAppPoolInfo()
    {
        Lock lock(&_crit);
        if (_calledResolveAppPoolInfo)
            return _hrResolveAppPoolInfo;

        HRESULT hr = S_OK;

        _appPoolName = _httpServer->GetAppPoolName();

        IWpfSettings* settings = NULL;
        _HR(_httpServer->GetWorkerProcessSettings(&settings));

        _HR(GetStringProperty(settings, CLR_VERSION, &_clrVersion));
        _HR(GetStringProperty(settings, APP_POOL_NAME, &_appPoolName));
        _HR(GetStringProperty(settings, APP_HOST_FILE_NAME, &_appHostFileName));
        _HR(GetStringProperty(settings, ROOT_WEB_CONFIG_FILE_NAME, &_rootWebConfigFileName));
        _HR(GetStringProperty(settings, CLR_CONFIG_FILE_NAME, &_clrConfigFileName));

        if (settings != NULL)
        {
            settings->Release();
        }

        _hrResolveAppPoolInfo = hr;
        _calledResolveAppPoolInfo = TRUE;
        return hr;
    }

    HRESULT GetStringProperty(IWpfSettings* settings, WPF_SETTINGS_STRING_ENUM settingId, _bstr_t* value)
    {
        HRESULT hr = S_OK;
        DWORD cchValue = 0;
        _HR(settings->GetStringProperty(settingId, NULL, &cchValue));
        if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            return hr;

        hr = S_OK;
        BSTR bstrValue = SysAllocStringLen(NULL, cchValue - 1);
        _HR_E_OUTOFMEMORY(bstrValue);
        _HR(settings->GetStringProperty(settingId, bstrValue, &cchValue));
        if (SUCCEEDED(hr))
        {
            value->Attach(bstrValue);
        }
        else if (bstrValue != NULL)
        {
            SysFreeString(bstrValue);
        }

        return hr;
    }

    HRESULT InitializeRuntime()
    {
        Lock lock(&_crit);
        if (_calledInitializeRuntime)
            return _hrInitializeRuntime;
         

        HRESULT hr = S_OK;
        
        _HR(ResolveAppPoolInfo());

        _HR(CLRCreateInstance(CLSID_CLRMetaHost, PPV(&_MetaHost))); 
        _HR(CLRCreateInstance(CLSID_CLRMetaHostPolicy, PPV(&_MetaHostPolicy))); 

        IStreamPtr cfgStream = new ComObject<FileStream>();
        _HR(static_cast<FileStream*>(cfgStream.GetInterfacePtr())->Open(_clrConfigFileName));

        WCHAR wzVersion[130] = {0};
        DWORD cchVersion = 129;
        DWORD dwConfigFlags = 0;

        ICLRRuntimeInfoPtr runtimeInfo;
        _HR(_MetaHostPolicy->GetRequestedRuntime(
            METAHOST_POLICY_APPLY_UPGRADE_POLICY,
            NULL,
            cfgStream,
            wzVersion,
            &cchVersion,
            NULL,//wzImageVersion,
            NULL,//&cchImageVersion,
            &dwConfigFlags,
            PPV(&runtimeInfo)));

        cfgStream = NULL;

        _HR(runtimeInfo->SetDefaultStartupFlags(
            STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST |
            STARTUP_SERVER_GC,
            _clrConfigFileName));


        ICLRRuntimeHostPtr runtimeHost;
        _HR(runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, PPV(&runtimeHost)));

        _HR(runtimeHost->SetHostControl(this));

        _HR(runtimeHost->Start());

        DWORD dwAppDomainId = 0;
        _HR(runtimeHost->GetCurrentAppDomainId(&dwAppDomainId));
        
        _RuntimeHost = runtimeHost;

        _hrInitializeRuntime = hr;
        _calledInitializeRuntime = TRUE;
        return hr;
    }

    HRESULT CreateAppDomain(
        /*in*/ PCWSTR applicationPhysicalPath, 
        /*in*/ PCWSTR applicationId,  
        /*in*/ PCWSTR appConfigPath,
        /*out*/ DWORD* pdwDomainId,
        /*out*/ AppHandlerInfo::BindHandlerDelegate* ppfnBindHandler)
    {
        HRESULT hr = S_OK;
        _HR_OUT(pdwDomainId);
        _HR_OUT(ppfnBindHandler);

        _HR(InitializeRuntime());

        Lock lock(&_createAppDomain_Crit);
        {
            DWORD domainId = _createAppDomain(applicationPhysicalPath, applicationId,  appConfigPath);
            *pdwDomainId = domainId;
            *ppfnBindHandler = _createAppDomain_BindHandler;
            _createAppDomain_BindHandler = NULL;
        }

        return hr;
    }

    HRESULT UnloadAppDomain(DWORD domainId)
    {
        HRESULT hr = S_OK;
        _unloadAppDomain(domainId);
        return hr;
    }

    //////////////////////////
    // IHostControl

    STDMETHODIMP GetHostManager( 
        /* [in] */ REFIID riid,
        /* [out] */ void **ppObject)
    {
        HRESULT hr = S_OK;
        _HR(static_cast<ICrosswalkModule*>(this)->QueryInterface(riid, ppObject));
        return hr;
    }
        
    STDMETHODIMP SetAppDomainManager( 
        /* [in] */ DWORD dwAppDomainID,
        /* [in] */ IUnknown *pUnkAppDomainManager)
    {
        HRESULT hr = S_OK;
        return hr;
    }
};

