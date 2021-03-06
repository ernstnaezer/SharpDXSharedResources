SharpDX Shared surface example
=====
This is a 'one file' example of a shared surfce between two direct X devices written in C# / SharpDX. Windows 8 Direct2D.1 has native support for this but as long as you are on Windows7 you need some trickery.

![the example](https://github.com/enix/SharpDXSharedResources/raw/master/Artifacts/shared_surfaces.png "this is how it should look")

Details
---

Direct2D only works when you create a Direct3D 10.1 device, but it can share surfaces with Direct3D 11. All you need to do is create both devices and render all of your Direct2D content to a texture that you share between them. It incurs a slight cost, but it is small and constant per frame.

A basic outline of the process you will need to use is:

1. Create your Direct3D 11 device like you do normally.
* Create a texture with the D3D10_RESOURCE_MISC_SHARED_KEYEDMUTEX option in order to allow access to the ID3D11KeyedMutex interface.
* Use the GetSharedHandle to get a handle to the texture that can be shared among devices.
* Create a Direct3D 10.1 device, ensuring that it is created on the same adapter.
* Use OpenSharedResource function on the Direct3D 10.1 device to get a version of the texture for Direct3D 10.1.
* Get access to the D3D10 KeyedMutex interface for the texture.
* Use the Direct3D 10.1 version of the texture to create the RenderTarget using Direct2D.
* When you want to render with D2D, use the keyed mutex to lock the texture for the D3D10 device. Then, acquire it in D3D11 and render the texture like you were probably already trying to do.

It's not trivial, but it works well, and it is the way that they intended you to interoperate between them.

(Taken from [this](http://stackoverflow.com/questions/4485265/cant-create-direct2d-dxgi-surface) stackoverflow post written by zhuman)

Code origin
----
The code is ported from / based on a SlimDX sample you can find here: http://www.aaronblog.us/?p=36

Resources
---

* http://sharpdx.org/forum/5-api-usage/164-d3d11-d2d1-direct3d-and-2d-interop
* http://msdn.microsoft.com/en-us/library/ee913554(v=vs.85).aspx
* http://www.aaronblog.us/?p=36
* http://stackoverflow.com/questions/4485265/cant-create-direct2d-dxgi-surface

License
===
Coded by Aaron Auseth and Ernst Naezer

  Freeware: The author, of this software accepts no responsibility for damages resulting
  from the use of this product and makes no warranty or representation, either
  express or implied, including but not limited to, any implied warranty of
  merchantability or fitness for a particular purpose. This software is provided
  "AS IS", and you, its user, assume all risks when using it.
  
  All I ask is that I be given credit if you use as a tutorial or for educational purposes.
