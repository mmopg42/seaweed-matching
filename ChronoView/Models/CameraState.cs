namespace ChronoView.Models;

/// <summary>
/// Represents the execution state of an external camera program.
/// </summary>
public enum CameraState
{
    /// <summary>
    /// The program is not running.
    /// </summary>
    Stopped,

    /// <summary>
    /// The program is currently launching.
    /// </summary>
    Starting,

    /// <summary>
    /// The program is running and active.
    /// </summary>
    Running,

    /// <summary>
    /// The program is currently terminating.
    /// </summary>
    Stopping
}
