using UnityEngine;
using UnityEngine.Rendering;

public class Utils
{
    private static string GetGraphicsDeviceName()
    {
        string gfxType = "Null";
        switch (SystemInfo.graphicsDeviceType)
        {
            case GraphicsDeviceType.Null:
                gfxType = "Null";
                break;

            case GraphicsDeviceType.Direct3D11:
                gfxType = "Direct3D11";
                break;

#pragma warning disable CS0618 // Type or member is obsolete
            case GraphicsDeviceType.OpenGL2:
#pragma warning restore CS0618 // Type or member is obsolete
                gfxType = "OpenGL2";
                break;

            case GraphicsDeviceType.OpenGLES2:
                gfxType = "OpenGLES2";
                break;

            case GraphicsDeviceType.OpenGLES3:
                gfxType = "OpenGLES3";
                break;

#pragma warning disable CS0618 // Type or member is obsolete
            case GraphicsDeviceType.PlayStation3:
#pragma warning restore CS0618 // Type or member is obsolete
                gfxType = "PlayStation3";
                break;

            case GraphicsDeviceType.PlayStation4:
                gfxType = "PlayStation4";
                break;

            case GraphicsDeviceType.XboxOne:
                gfxType = "XboxOne";
                break;

            case GraphicsDeviceType.Metal:
                gfxType = "Metal";
                break;

            case GraphicsDeviceType.OpenGLCore:
                gfxType = "OpenGLCore";
                break;

            case GraphicsDeviceType.Direct3D12:
                gfxType = "Direct3D12";
                break;

            case GraphicsDeviceType.Vulkan:
                gfxType = "Vulkan";
                break;

            case GraphicsDeviceType.Switch:
                gfxType = "Switch";
                break;

            case GraphicsDeviceType.XboxOneD3D12:
                gfxType = "XboxOneD3D12";
                break;
        }

        return gfxType;
    }
}