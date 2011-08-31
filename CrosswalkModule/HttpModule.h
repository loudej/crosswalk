
#pragma once


#include "HttpApplicationStoredContext.h"

class __declspec(uuid("4FD8E073-2652-4855-ADA6-2A4CB8E62AA0")) IHttpTransaction : public IUnknown
{
public:
    virtual HRESULT Initialize(ICrosswalkModule* crosswalkModule, HTTP_MODULE_ID moduleId, IHttpServer* httpServer) = 0;

    virtual HRESULT ResponseStart(
        PCWSTR status,
        int headerCount,
        PCWSTR* headerNames,
        PCWSTR* headerValues) = 0;

    virtual HRESULT ResponseBody(
        const BYTE* buffer,
        int offset,
        int count,
        ExecuteHandlerContext::ContinuationDelegate continuation,
        BOOL* async) = 0;

    virtual HRESULT ResponseComplete(
        HRESULT hresultFromException) = 0;
};

_COM_SMARTPTR_TYPEDEF(IHttpTransaction, __uuidof(IHttpTransaction));

class HttpModule : 
    public CHttpModule,
    public IHttpTransaction
{
    ICrosswalkModulePtr _crosswalkModule;
    HTTP_MODULE_ID _moduleId;
    IHttpServer* _httpServer;

    IHttpContext* _httpContext;

    CriticalSection _vitalityCrit;
    int _vitalityPending;
    REQUEST_NOTIFICATION_STATUS _vitalityExitStatus;

    ExecuteHandlerContext::ContinuationDelegate _continuation;

public:
    HttpModule()
    {
        _httpServer = NULL;
        _httpContext = NULL;
        _vitalityExitStatus = RQ_NOTIFICATION_CONTINUE;
        _vitalityPending = 0;
        _continuation = NULL;
    }

    IUnknown* CastInterface(REFIID riid)
    {
        if (riid == __uuidof(IHttpTransaction))
            return static_cast<IHttpTransaction*>(this);
        return NULL;
    }
    
    HRESULT Initialize(ICrosswalkModule* crosswalkModule, HTTP_MODULE_ID moduleId, IHttpServer* httpServer)
    {
        HRESULT hr = S_OK;
        _crosswalkModule = crosswalkModule;
        _moduleId = moduleId;
        _httpServer = httpServer;
        return hr;
    }

    void Dispose()
    {
        Release();
    }


    typedef enum
    {
        VITALITY_Calling_OnExecuteRequestHandler,
        VITALITY_Calling_OnAsyncCompletion,
        VITALITY_Pending_OnAsyncCompletion,
        VITALITY_Pending_RequestComplete,
    } VITALITY_Correlation;

    void IncreaseVitality(VITALITY_Correlation correlation)
    {
        Lock lock(&_vitalityCrit);
        ++_vitalityPending;
        if (_vitalityPending == 1)
        {
            AddRef();
        }
    }

    REQUEST_NOTIFICATION_STATUS DecreaseVitality(VITALITY_Correlation correlation)
    {
        Lock lock(&_vitalityCrit);
        --_vitalityPending;
        if (_vitalityPending == 0)
        {
            Release();
            return _vitalityExitStatus;
        }
        else
        {
            return RQ_NOTIFICATION_PENDING;
        }
    }
    
    // RQ_EXECUTE_REQUEST_HANDLER

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        
        HRESULT hr = S_OK;
        
        // REVIEW guard this?
        _httpContext = pHttpContext;

        IncreaseVitality(VITALITY_Calling_OnExecuteRequestHandler);
        IncreaseVitality(VITALITY_Pending_RequestComplete);

        HttpApplicationStoredContext* applicationContext = NULL;
        _HR(HttpApplicationStoredContext::Get(_crosswalkModule, pHttpContext->GetApplication(), &applicationContext));

        ExecuteHandlerContext::ExecuteHandlerDelegate executeHandler = NULL;
        _HR(applicationContext->InitializeHandler(
            pHttpContext->GetScriptMap(), 
            &executeHandler));
        
        static PCSTR const serverVariableNames[] = 
        {
            "REQUEST_METHOD",
            "SCRIPT_NAME",
            "PATH_INFO",
            "QUERY_STRING",
            "CONTENT_TYPE",
            "CONTENT_LENGTH",
            "SERVER_NAME",
            "SERVER_PORT",
            "SERVER_PROTOCOL",
        };
        const int serverVariableCount = sizeof(serverVariableNames) / sizeof(*serverVariableNames);

        PCSTR pszServerVariables[serverVariableCount] = {0};
        DWORD cchServerVariables[serverVariableCount] = {0};    
        for(int index = 0; index != serverVariableCount; ++index)
        {
            _HR(pHttpContext->GetServerVariable(serverVariableNames[index], &pszServerVariables[index], &cchServerVariables[index]));
        }

        IHttpRequest* httpRequest = pHttpContext->GetRequest();
        
        DWORD   dwOldChangeNumber = 0;
        DWORD  dwNewChangeNumber = 0;
        PCSTR   knownHeaderSnapshot[HttpHeaderRequestMaximum] = {0};
        DWORD dwUnknownHeaderSnapshot = {0};
        PCSTR *pUnknownHeaderNameSnapshot = NULL;
        PCSTR *pUnknownHeaderValueSnapshot = NULL;
        DWORD   diffedKnownHeaderIndices[HttpHeaderRequestMaximum+1] = {0};
        DWORD dwDiffedUnknownHeaders = 0;
        DWORD* pDiffedUnknownHeaderIndices = NULL;
        _HR(httpRequest->GetHeaderChanges(
            dwOldChangeNumber,
            &dwNewChangeNumber,
            knownHeaderSnapshot,
            &dwUnknownHeaderSnapshot,
            &pUnknownHeaderNameSnapshot,
            &pUnknownHeaderValueSnapshot,
            diffedKnownHeaderIndices,
            &dwDiffedUnknownHeaders,
            &pDiffedUnknownHeaderIndices
        ));

        pHttpContext->GetResponse()->DisableBuffering();

        if (SUCCEEDED(hr))
        {
            executeHandler(
                this,
                pszServerVariables,
                knownHeaderSnapshot,
                pUnknownHeaderNameSnapshot,
                dwUnknownHeaderSnapshot,
                pUnknownHeaderValueSnapshot,
                dwUnknownHeaderSnapshot);
        }

        if (FAILED(hr))
        {
            pProvider->SetErrorStatus(hr);
            _vitalityExitStatus = RQ_NOTIFICATION_FINISH_REQUEST;
        }
        
        return DecreaseVitality(VITALITY_Calling_OnExecuteRequestHandler);
    }
    
    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        IN IHttpContext *                       pHttpContext,
        IN DWORD                                dwNotification,
        IN BOOL                                 fPostNotification,
        IN IHttpEventProvider *                 pProvider,
        IN IHttpCompletionInfo *                pCompletionInfo)
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( dwNotification );
        UNREFERENCED_PARAMETER( fPostNotification );
        UNREFERENCED_PARAMETER( pProvider );
        UNREFERENCED_PARAMETER( pCompletionInfo );

        IncreaseVitality(VITALITY_Calling_OnAsyncCompletion);
        DecreaseVitality(VITALITY_Pending_OnAsyncCompletion);

        ExecuteHandlerContext::ContinuationDelegate continuation = _continuation;
        _continuation = NULL;
        if (continuation != NULL)
        {
            continuation();
        }

        return DecreaseVitality(VITALITY_Calling_OnAsyncCompletion);
    }

    HRESULT ResponseStart(
        PCWSTR status,
        int headerCount,
        PCWSTR* headerNames,
        PCWSTR* headerValues)
    {
        HRESULT hr = S_OK;
        IHttpResponse* response = _httpContext->GetResponse();
        _HR(response->SetStatus(200, "OK"));
        for(int index = 0; index != headerCount; ++index)
        {
            _bstr_t name(headerNames[index]);
            _bstr_t value(headerValues[index]);
            _HR(response->SetHeader(name, value, value.length(), FALSE));
        }

        return hr;
    }

    HRESULT ResponseBody(
        const BYTE* buffer,
        int offset,
        int count,
        ExecuteHandlerContext::ContinuationDelegate continuation,
        BOOL* async)
    {
        HRESULT hr = S_OK;
        _HR_OUT(async);

        IncreaseVitality(VITALITY_Pending_OnAsyncCompletion);
        BOOL rollbackVitality = TRUE;
        _continuation = continuation;

        IHttpResponse* response = _httpContext->GetResponse();
        HTTP_DATA_CHUNK dataChunk;
        dataChunk.DataChunkType = HttpDataChunkFromMemory;
        dataChunk.FromMemory.pBuffer = const_cast<BYTE*>(buffer + offset);
        dataChunk.FromMemory.BufferLength = count;

        DWORD cbSent = 0;
        BOOL completionExpected = FALSE;
        _HR(response->WriteEntityChunks(
            &dataChunk, 
            1, 
            continuation != NULL,
            TRUE, 
            &cbSent, 
            &completionExpected));

        if (SUCCEEDED(hr) && completionExpected)
            rollbackVitality = FALSE;

        if (rollbackVitality)
        {
            _continuation = NULL;
            REQUEST_NOTIFICATION_STATUS status = DecreaseVitality(VITALITY_Pending_OnAsyncCompletion);
            if (status != RQ_NOTIFICATION_PENDING)
            {
                _httpContext->IndicateCompletion(status);
            }
        }

        *async = completionExpected;
        return hr;
    }

    HRESULT ResponseComplete(
        HRESULT hresultFromException)
    {
        HRESULT hr = S_OK;
        REQUEST_NOTIFICATION_STATUS status = DecreaseVitality(VITALITY_Pending_RequestComplete);
        if (status != RQ_NOTIFICATION_PENDING)
        {
            _httpContext->IndicateCompletion(status);
        }
        return hr;
    }
};




