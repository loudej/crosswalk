#I "./packages/FAKE.1.58.9/tools"
#I "c:/windows/System32/inetsrv"
#r "FakeLib.dll"
#r "System.Xml.dll"
#r "Microsoft.Web.Administration.dll"

open Fake
open Microsoft.Web.Administration
open System
open System.Runtime.InteropServices 

// properties
let projectName = "Crosswalk"
let version = "0.1.0"  
let projectSummary = "A native IIS module to run .NET code."
let projectDescription = "A native IIS module to run .NET code."
let authors = ["loudej"]
let mail = "louis.dejardin@gmail.com"
let homepage = "http://github.com/loudej/crosswalk"

// directories
let buildDir = "./build/compile/"
let buildDir64 = buildDir + "x64/"
let testDir = "./build/test/"
let deployDir = "./build/deploy/"
//let docsDir = "./build/docs/"
let inetsrvDir = (environVar("windir") + "/SysWOW64/inetsrv/")
let inetsrvDir64 = (environVar("windir") + "/System32/inetsrv/")

// files
let applicationHostConfig = inetsrvDir64 + "config/applicationHost.config"

// tools
let fakePath = "./packages/FAKE.1.58.9/tools"
let nunitPath = "./packages/NUnit.2.5.10.11092/Tools"

// Filesets
let appReferences  = 
    !+ @"src\app\**\*.csproj" 
      ++ @"src\app\**\*.fsproj"
      |> Scan

let testReferences = 
    !+ @"src\test\**\*.csproj" 
      |> Scan

let vcxReferences  = 
    !+ @"src\app\**\*.vcxproj"  
      |> Scan

let vcxOutputs  = 
    !+ @"src\app\CrosswalkModule\bin\Release\*.dll" 
      ++ @"src\app\CrosswalkModule\bin\Release\*.pdb"
      |> Scan

let vcxOutputs64  = 
    !+ @"src\app\CrosswalkModule\bin\x64\Release\*.dll" 
      ++ @"src\app\CrosswalkModule\bin\x64\Release\*.pdb"
      |> Scan


// Targets
Target "Clean" (fun _ -> 
    CleanDirs [buildDir; buildDir64; testDir; deployDir]
    CreateDir buildDir
    CreateDir buildDir64
    CreateDir testDir
    CreateDir deployDir
)

Target "BuildApp32" (fun _ ->
    MSBuild buildDir "Build" ["Configuration", "Release"; "Platform", "Win32"] vcxReferences
        |> Log "BuildApp32-Output: "
    Copy buildDir vcxOutputs
)

Target "BuildApp64" (fun _ ->
    MSBuild buildDir "Build" ["Configuration", "Release"; "Platform", "x64"] vcxReferences
        |> Log "BuildApp64-Output: "
    Copy (buildDir + "x64/") vcxOutputs64
)

Target "BuildApp" (fun _ ->
    MSBuildRelease buildDir "Build" appReferences
        |> Log "BuildApp-Output: "
)

Target "BuildTest" (fun _ ->
    MSBuildRelease testDir "Build" testReferences
        |> Log "BuildTest-Output: "
)

Target "Test" (fun _ ->
    let dirinfo = System.IO.Directory.CreateDirectory testDir
    !+ (testDir + "/*.Tests.dll")
        |> Scan
        |> NUnit (fun p ->
            {p with
                ToolPath = nunitPath
                DisableShadowCopy = true
                OutputFile = testDir + "TestResults.xml" })
)

Target "Default" (fun _ ->
    trace " --- Default --- "
)

module InteropWithNative =
    [<DllImport(@"kernel32.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int Wow64DisableWow64FsRedirection([<Out>] IntPtr ppv)

    [<DllImport(@"kernel32.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int Wow64RevertWow64FsRedirection(IntPtr pv)

Target "Install" (fun _ ->
    let pv = System.IntPtr.Zero
    let ok = InteropWithNative.Wow64DisableWow64FsRedirection pv

    Copy inetsrvDir vcxOutputs
    Copy inetsrvDir64 vcxOutputs64

    let ok = InteropWithNative.Wow64RevertWow64FsRedirection pv

    let serverManager = new ServerManager()
    let config = serverManager.GetApplicationHostConfiguration()

    let pools = serverManager.ApplicationPools
    pools |>  Seq.filter (fun x -> x.State = ObjectState.Started ) |>  Seq.iter (fun x -> x.Stop() |> ignore )

    let removeConfigurationElementName = fun (collection : ConfigurationElementCollection, name) -> 
        let isName = fun (x : ConfigurationElement) -> x.["name"].ToString() = name
        let oldElement = collection |> Seq.tryFind isName 
        if (oldElement.IsSome) then collection.Remove(oldElement.Value)
    
    let removeAndAddConfigurationElement = fun (sectionName, elementType, elementName, configure) ->
        let section = config.GetSection sectionName
        let collection = section.GetCollection()
        let isName = fun (x : ConfigurationElement) -> x.["name"].ToString() = elementName
        let oldElement = collection |> Seq.tryFind isName 
        if (oldElement.IsSome) then collection.Remove(oldElement.Value)
        let addElement = collection.CreateElement elementType
        addElement.["name"] <- elementName
        configure addElement
        collection.Add addElement

    removeAndAddConfigurationElement ("system.webServer/globalModules", "add", "CrosswalkModule", (fun elt->
        elt.["image"] <- @"%windir%\system32\inetsrv\CrosswalkModule.dll"
    ))

    removeAndAddConfigurationElement ("system.applicationHost/applicationPools", "add", "CrosswalkSamplePool", (fun elt->
        elt.["managedRuntimeVersion"] <- @""
        elt.["CLRConfigFile"] <- @"hello.config"
    ))

    removeAndAddConfigurationElement ("system.applicationHost/sites", "site", "CrosswalkSample", (fun siteElement->
        siteElement.["id"] <- 6803

        let bindingsCollection = siteElement.GetCollection("bindings");
        let bindingElement = bindingsCollection.CreateElement("binding");
        bindingElement.["protocol"] <- @"http";
        bindingElement.["bindingInformation"] <- @"*:6803:";
        bindingsCollection.Add(bindingElement);

        let siteCollection = siteElement.GetCollection();
        let applicationElement = siteCollection.CreateElement("application");
        applicationElement.["path"] <- @"/";
        applicationElement.["applicationPool"] <- @"CrosswalkSamplePool";
        let applicationCollection = applicationElement.GetCollection();
        let virtualDirectoryElement = applicationCollection.CreateElement("virtualDirectory");
        virtualDirectoryElement.["path"] <- @"/";
        virtualDirectoryElement.["physicalPath"] <- @"C:\Projects\Crosswalk\build\compile\_PublishedWebsites\Sandbox";
        applicationCollection.Add(virtualDirectoryElement);
        siteCollection.Add(applicationElement) |> ignore
    ))

    serverManager.CommitChanges()

    pools |>  Seq.filter (fun x -> x.State = ObjectState.Stopped ) |>  Seq.iter (fun x -> x.Start() |> ignore )

    trace " --- Installed --- "

    //Copy inetsrvDir, [(buildDir + "Crosswalk.dll")]
//    Copy buildDir64, vcxOutputs64
//        |> Log "BuildTest-Output: "
//    let doc = new XmlDocument()
//    doc.Load applicationHostConfig
//    XPathReplaceNS xpath value namespaces doc
//    |> fun x -> x.Save fileName
)

Target "Deploy" (fun _ ->
    !+ (buildDir + "\**\*.*") 
        -- "*.zip" 
        |> Scan
        |> Zip buildDir (deployDir + "Crosswalk." + version + ".zip")
)

// Dependencies

"BuildApp" <== ["Clean"]
"BuildApp32" <== ["Clean"]
"BuildApp64" <== ["Clean"]
"BuildTest" <== ["Clean"]

"BuildApp" <== ["BuildApp32"]
"BuildApp" <== ["BuildApp64"]
"Test" <== ["BuildTest"]

"Deploy" <== ["BuildApp"]
"Default" <== ["Test"]
"Default" <== ["Deploy"]

"Install" <== ["BuildApp"]

// Start build
Run <| getBuildParamOrDefault "target" "Default"
