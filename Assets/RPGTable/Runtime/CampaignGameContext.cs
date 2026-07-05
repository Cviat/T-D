using System.Collections.Generic;
using RPGTable.Board;
using RPGTable.MapEditor;
using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Shared mutable state for the campaign game services.
    /// Owned by CampaignGameLoader, read/written by services.
    /// </summary>
    internal sealed class CampaignGameContext
    {
        public SavedCampaignData Campaign { get; set; }
        public readonly Dictionary<string, SavedCampaignMapNodeData> MapNodes = new Dictionary<string, SavedCampaignMapNodeData>();
        public SavedCampaignMapNodeData CurrentMapNode { get; set; }
        public Camera WorldCamera { get; set; }
        public BoardGrid Grid { get; set; }
        public Transform MapRoot { get; set; }
        public Transform TokenRoot { get; set; }
        public string SelectedPlayerId { get; set; }
        public CampaignRuntimeToken SelectedToken { get; set; }
    }
}
