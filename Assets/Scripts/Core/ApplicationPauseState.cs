namespace Emberfall
{
    // Operating-system suspension never owns the manual menu bit.
    public sealed class ApplicationPauseState
    {
        public bool Focused { get; private set; } = true;
        public bool Suspended { get; private set; }
        public bool BackgroundPaused { get { return !Focused || Suspended; } }
        public void SetFocus(bool focused) { Focused = focused; }
        public void SetSuspended(bool suspended) { Suspended = suspended; }
        // Desktop keeps its existing frame/vSync policy. Mobile menus need less rendering.
        public int TargetFrameRate(bool mobile, bool advancing)
        { return !mobile ? 60 : BackgroundPaused ? 15 : advancing ? 60 : 30; }
        public bool CanAdvance(bool started, bool manualPause, bool modal, bool dead)
        { return started && !manualPause && !modal && !dead && !BackgroundPaused; }
    }
}
