// CrosswalkModule.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "ComObject.h"
#include "CrosswalkModule.h"
#include "GlobalModule.h"
#include "HttpModuleFactory.h"

ICrosswalkModulePtr g_crosswalkModule;

HRESULT __stdcall RegisterModule(
    DWORD dwServerVersion,
    IHttpModuleRegistrationInfo * pModuleInfo,
    IHttpServer * httpServer)
{
    UNREFERENCED_PARAMETER( dwServerVersion );

    HRESULT hr = S_OK;

    PCWSTR moduleName = pModuleInfo->GetName();
    if (lstrcmp(moduleName, L"CrosswalkModule") == 0)
    {
        HTTP_MODULE_ID moduleId = pModuleInfo->GetId();

        ICrosswalkModulePtr crosswalkModule = new ComObject<CrosswalkModule, ICrosswalkModule>();
        _HR(crosswalkModule->Initialize(moduleId, httpServer));

        _HR(pModuleInfo->SetGlobalNotifications(
            new GlobalModule(crosswalkModule, moduleId, httpServer),
            GL_APPLICATION_START | 
            GL_APPLICATION_STOP));

        _HR(pModuleInfo->SetRequestNotifications(
            new HttpModuleFactory(crosswalkModule, moduleId, httpServer),
            RQ_EXECUTE_REQUEST_HANDLER,
            0));

        if (SUCCEEDED(hr))
        {
            // LOCK
            g_crosswalkModule = crosswalkModule;
        }
    }

    return hr;
}


extern "C" HRESULT __stdcall BindAppPoolInfo(AppPoolInfo* pInfo)
{
    HRESULT hr = S_OK;

    // LOCK g_
    ICrosswalkModulePtr crosswalkModule = g_crosswalkModule;

    _HR(crosswalkModule->BindAppPoolInfo(pInfo));
    return hr;
}

extern "C" HRESULT __stdcall BindAppHandlerInfo(AppHandlerInfo* pInfo)
{
    HRESULT hr = S_OK;

    // LOCK g_
    ICrosswalkModulePtr crosswalkModule = g_crosswalkModule;

    _HR(crosswalkModule->BindAppHandlerInfo(pInfo));
    return hr;
}


extern "C" HRESULT __stdcall ResponseStart(
    IUnknown* transaction,
    PCWSTR status,
    int headerCount,
    PCWSTR* headerNames,
    PCWSTR* headerValues)
{
    IHttpTransactionPtr ptr = transaction;

    HRESULT hr = S_OK;
    _HR_E_POINTER(ptr);
    _HR(ptr->ResponseStart(status, headerCount, headerNames, headerValues));
    return hr;
}

extern "C" HRESULT __stdcall ResponseBody(
    IUnknown* transaction,
    const BYTE* buffer,
    int offset,
    int count,
    ExecuteHandlerContext::ContinuationDelegate continuation,
    BOOL* async)
{
    IHttpTransactionPtr ptr = transaction;

    HRESULT hr = S_OK;
    _HR_E_POINTER(ptr);
    _HR(ptr->ResponseBody(buffer, offset, count, continuation, async));
    return hr;
}

extern "C" HRESULT __stdcall ResponseComplete(
    IUnknown* transaction,
    HRESULT hresultFromException)
{
    IHttpTransactionPtr ptr = transaction;

    HRESULT hr = S_OK;
    _HR_E_POINTER(ptr);
    _HR(ptr->ResponseComplete(hresultFromException));
    return hr;
}

// data structure for the callback function
struct HttpHandlerContext
{
    int I;
    TCHAR* Message;
};


// callback function prototype
typedef void (*HttpHandlerDelegate)(HttpHandlerContext data);

HRESULT __stdcall Wack(HttpHandlerDelegate callback)
{
    return S_OK;
}
