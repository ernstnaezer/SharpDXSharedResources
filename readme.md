Combined resource between direct 2d and direct 3d (v11)
=====
This is a 'one file' example of a combined direct 2D and direct 3D shared texture using SharpDX.

Origin
----
The code is ported from / based on a SlimDX sample you can find here: http://www.aaronblog.us/?p=36

Exception
====
!! Please note that the current code is not working yet. 

The following exception is thrown when opening the shared resource:
  
    SharpDX.SharpDXException was unhandled
    HResult=-2147024809
    Message=HRESULT: [0x80070057], Module: [Unknown], ApiCode: [Unknown/Unknown], Message: The parameter is incorrect.

    Source=SharpDX
    StackTrace:
         at SharpDX.Result.CheckError()
         at SharpDX.Direct3D10.Device.OpenSharedResource(IntPtr hResource, Guid returnedInterface, IntPtr& resourceOut)
         at SharpDX.Direct3D10.Device.OpenSharedResource[T](IntPtr resourceHandle)
         at Combined2DAnd3D.Program.Run() in d:\Working\Combined2DAnd3D\Combined2DAnd3D\Program.cs:line 140
         at Combined2DAnd3D.Program.Main(String[] args) in d:\Working\Combined2DAnd3D\Combined2DAnd3D\Program.cs:line 325
         at System.AppDomain._nExecuteAssembly(RuntimeAssembly assembly, String[] args)
         at System.AppDomain.ExecuteAssembly(String assemblyFile, Evidence assemblySecurity, String[] args)
         at Microsoft.VisualStudio.HostingProcess.HostProc.RunUsersAssembly()
         at System.Threading.ThreadHelper.ThreadStart_Context(Object state)
         at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
         at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
         at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state)
         at System.Threading.ThreadHelper.ThreadStart()
    InnerException: 
      