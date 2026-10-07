namespace Emberfall
{
    internal static class HubNpcGreeting
    {
        public static string Text(HubNpcKind kind)
        {
            return kind==HubNpcKind.Merchant?"商人：欢迎，旅行者。补给都备好了，来看看吧。":
                kind==HubNpcKind.Blacksmith?"铁匠：来得正好。把装备交给我，我帮你打磨。":
                kind==HubNpcKind.Exchange?"观星员：星路正在指引你。准备好下一段旅程了吗？":"";
        }
        public static string Resource(HubNpcKind kind)
        {
            return kind==HubNpcKind.Merchant?"Audio/Npc/MerchantGreeting":
                kind==HubNpcKind.Blacksmith?"Audio/Npc/BlacksmithGreeting":
                kind==HubNpcKind.Exchange?"Audio/Npc/StargazerGreeting":null;
        }
    }
}
