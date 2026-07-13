namespace RPGTable.MapEditor
{
    public static class CampaignEditSession
    {
        public static string ActiveCampaignName;
        public static string EditingNodeId;
        public static string EditingMapPath;

        public static bool IsEditingPresetTokens => !string.IsNullOrEmpty(EditingNodeId);

        public static void Clear()
        {
            ActiveCampaignName = null;
            EditingNodeId = null;
            EditingMapPath = null;
        }
    }
}
