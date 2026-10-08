using System;
using Emberfall;
// Compile the production partial with the smallest UI/session surface. These
// tests execute its state changes; they do not simulate Unity event delivery.
namespace Emberfall
{
    public static class MobileControls { public static bool Active; }
    public sealed class PauseSessionStub { public bool Paused; }
    public sealed partial class GameUI
    {
        private bool MerchantServiceActive=>true;enum Panel{ None, Controls, Chests, SaveSelection, SaveLocation, Bindings ,Skills}
        private Panel panel;
        private PauseSessionStub session;
        private int mobilePausePage, transitionBlocks;
        private void BlockUITransition() { transitionBlocks++; }
        public static int Verify()
        {
            int n = 0;
            Action<bool,string> check = (ok, message) => { n++; if (!ok) throw new Exception(message); };
            for (int mobile = 0; mobile < 2; mobile++)
            for (int paused = 0; paused < 2; paused++)
            for (int modal = 0; modal < 6; modal++)
            for (int page = 0; page < 3; page++)
            {
                MobileControls.Active = mobile != 0;
                var ui = new GameUI { session = new PauseSessionStub { Paused = paused != 0 },
                    panel = (Panel)modal, mobilePausePage = page };
                bool expected = mobile != 0 && paused != 0 && (modal == 0 || modal == 2) && page > 0;
                check(ui.ReturnToMobilePauseRoot() == expected, "only mobile paused subpages consume Back");
                check(ui.mobilePausePage == (expected ? 0 : page), "subpage returns to root; other surfaces remain intact");
                check(ui.session.Paused == (paused != 0), "Back to root never resumes or introduces pause");
                check(ui.transitionBlocks == (expected ? 1 : 0), "only consumed navigation blocks held pointer");
                check(!ui.ReturnToMobilePauseRoot(), "next Back on root falls through to normal pause handling");
                check(ui.transitionBlocks == (expected ? 1 : 0), "root does not relatch the touch gate");
            }
            MobileControls.Active = true;
            check(!new GameUI { mobilePausePage = 2 }.ReturnToMobilePauseRoot(), "missing session ignored");
            return n;
        }
    }
}
public static class MobilePauseNavigationTests
{
    public static string Run() { return "PASS: " + GameUI.Verify() + " production mobile pause navigation assertions"; }
}
