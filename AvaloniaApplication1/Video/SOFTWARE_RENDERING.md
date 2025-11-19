# MPV Software Rendering for Avalonia

This implementation provides a software rendering option for MPV video playback in Avalonia applications, particularly useful for Linux systems where OpenGL/hardware acceleration is problematic.

## Components

### 1. MpvPlayer Software Rendering Support

Added to the `MpvPlayer` class:

- **`InitializeWithSoftwareRendering()`** - Initializes MPV with software rendering mode instead of OpenGL
- **`SoftwareRender(int width, int height, IntPtr surfaceAddress, string format)`** - Renders video frames to a software buffer

### 2. MpvSoftwareControl

A new Avalonia control `MpvSoftwareControl` that uses software rendering instead of OpenGL.

## Usage

### Using OpenGL Rendering (existing):

```csharp
<video:MpvOpenGLControl x:Name="VideoControl" />
```

### Using Software Rendering (new):

```csharp
<video:MpvSoftwareControl x:Name="VideoControl" />
```

### Code Example:

```csharp
// Load a video file
videoControl.LoadFile("/path/to/video.mp4");

// Toggle play/pause
videoControl.TogglePlayPause();

// Access the player for more control
var player = videoControl.Player;
if (player != null)
{
    player.Volume = 75;
    player.Speed = 1.5;
    player.Position = 30.0; // Seek to 30 seconds
}

// Unload video
videoControl.Unload();
```

## When to Use Software Rendering

Use `MpvSoftwareControl` when:

1. **Linux environments** where OpenGL context creation is problematic (Wayland/X11 issues)
2. **Virtual machines** or remote desktop scenarios
3. **Systems without GPU acceleration**
4. **Fallback option** when OpenGL rendering fails

## Performance Considerations

- Software rendering is CPU-intensive and may use more resources than OpenGL
- Performance depends on video resolution and CPU capabilities
- Lower resolution videos work better with software rendering

## Platform-Specific Notes

### Linux
- Software rendering works well when OpenGL has display server conflicts
- No need to configure `gpu-context` settings
- Works with both Wayland and X11

### Windows
- OpenGL rendering is generally more reliable on Windows
- Software rendering can be used as a fallback

### macOS
- OpenGL rendering is recommended
- Software rendering available as fallback

## Technical Details

The software rendering uses MPV's `sw` render API:
- Renders to a `WriteableBitmap` buffer
- Uses `bgra` pixel format (or `rgba` on Android)
- Updates are triggered by MPV's render callback system
- Integrates with Avalonia's rendering pipeline through `DrawingContext`
