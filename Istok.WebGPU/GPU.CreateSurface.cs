using Istok.WebGPU.MacOS;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;

namespace Istok.WebGPU;

public unsafe partial class GPU
{
	public unsafe GPUSurface CreateWebGPUSurface(INativeWindowSource view)
  {
    WGPUSurfaceDescriptor descriptor = new WGPUSurfaceDescriptor()
    {
      label = new WGPUStringView()
      {
        data = null,
        length = Strlen
      }
    };

    if (view.Native.X11.HasValue)
    {
      WGPUSurfaceSourceXlibWindow descriptorFromXlibWindow = new WGPUSurfaceSourceXlibWindow()
      {
        chain = new ChainedStruct()
        {
          next = (ChainedStruct*) null,
          sType = WGPUSType.SurfaceSourceXlibWindow
        },
        display = (void*) view.Native.X11.Value.Display,
        window = (ulong) (uint) view.Native.X11.Value.Window
      };
      descriptor.nextInChain = (ChainedStruct*) &descriptorFromXlibWindow;
    }
    else if (view.Native.Cocoa.HasValue)
    {
      IntPtr ptr = view.Native.Cocoa.Value;
      CAMetalLayer caMetalLayer = CAMetalLayer.New();
      NSView contentView = new NSWindow(ptr).contentView with
      {
        wantsLayer = (Bool8) true,
        layer = caMetalLayer.NativePtr
      };
      
      WGPUSurfaceSourceMetalLayer descriptorFromMetalLayer = new WGPUSurfaceSourceMetalLayer()
      {
        chain = new ChainedStruct()
        {
          next = (ChainedStruct*) null,
          sType = WGPUSType.SurfaceSourceMetalLayer
        },
        layer = (void*) caMetalLayer.NativePtr
      };
      descriptor.nextInChain = (ChainedStruct*) &descriptorFromMetalLayer;
    }
    else if (view.Native.Wayland.HasValue)
    {
      WGPUSurfaceSourceWaylandSurface fromWaylandSurface = new WGPUSurfaceSourceWaylandSurface()
      {
        chain = new ChainedStruct()
        {
          next = (ChainedStruct*) null,
          sType = WGPUSType.SurfaceSourceWaylandSurface
        },
        display = (void*) view.Native.Wayland.Value.Display,
        surface = (void*) view.Native.Wayland.Value.Surface
      };
      descriptor.nextInChain = (ChainedStruct*) &fromWaylandSurface;
    }
    else if (view.Native.Win32.HasValue)
    {
      WGPUSurfaceSourceWindowsHWND descriptorFromWindowsHwnd = new WGPUSurfaceSourceWindowsHWND()
      {
        chain = new ChainedStruct()
        {
          next = (ChainedStruct*) null,
          sType = WGPUSType.SurfaceSourceWindowsHWND
        },
        hwnd = (void*) view.Native.Win32.Value.Hwnd,
        hinstance = (void*) view.Native.Win32.Value.HInstance
      };
      descriptor.nextInChain = (ChainedStruct*) &descriptorFromWindowsHwnd;
    }
    else
    {
      if (!view.Native.Android.HasValue)
        throw new PlatformNotSupportedException($"Your platform is not supported! {view.Native.Kind}");
      WGPUSurfaceSourceAndroidNativeWindow androidNativeWindow = new WGPUSurfaceSourceAndroidNativeWindow()
      {
        chain = new ChainedStruct()
        {
          next = (ChainedStruct*) null,
          sType = WGPUSType.SurfaceSourceAndroidNativeWindow
        },
        window = (void*) view.Native.Android.Value.Window
      };
      descriptor.nextInChain = (ChainedStruct*) &androidNativeWindow;
    }
    WGPUSurface surface = wgpuInstanceCreateSurface(_instance, &descriptor);
    
    return new GPUSurface(surface, null, false);
  }
}