using System.Threading.Tasks;

namespace AvaloniaApplication1.Video;

public interface IVideoPlayerInstance
{
    string Name { get; } // player name
    string FileName { get; } // current loaded media file

    void LoadFile(string fileName);
    void CloseFile();

    void Play();
    void PlayOrPause();
    void Pause();
    void Stop();    

    bool IsPlaying { get; }
    bool IsPaused { get; }

    double Position { get; set; }
    double Duration { get; }

    int VolumeMaximum { get; }
    double Volume { get; set; }

    double Speed { get; set; }
}

