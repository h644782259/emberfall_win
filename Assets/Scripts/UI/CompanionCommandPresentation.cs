namespace Emberfall
{
    // Presentation of live free orders: no synthetic resource, cooldown or click timer.
    public readonly struct CompanionCommandPresentation
    {
        public readonly bool FocusEnabled, RecallEnabled, FocusActive, RecallActive;
        public readonly string FocusReason, RecallReason, RecallName;
        public CompanionCommandPresentation(int count, bool blocked, bool targetValid, bool inRange, bool focused, bool recalled)
        {
            FocusActive=count>0&&!blocked&&focused;
            RecallActive=count>0&&!blocked&&recalled;
            FocusReason=blocked?"暂停":count==0?"无召唤物":!targetValid?"无目标":!inRange?"目标太远":"";
            RecallReason=blocked?"暂停":count==0?"无召唤物":"";
            FocusEnabled=FocusReason.Length==0;RecallEnabled=RecallReason.Length==0;
            RecallName=recalled?"出击":"召回";
        }
    }
}
