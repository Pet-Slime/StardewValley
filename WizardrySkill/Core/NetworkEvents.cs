using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Spells;
using Log = MoonShared.Attributes.Log;

namespace WizardrySkill.Core
{
    /// <summary>
    /// A structured multiplayer packet for announcing that a player cast a spell.
    /// </summary>
    /// <remarks>
    /// This packet describes the cast. It does not mean every machine should replay the full spell.
    /// The caster runs the real local spell behavior. Remote machines may observe the cast, create
    /// custom Wizardry visuals, or perform local-player-only reactions when a spell explicitly opts in.
    /// </remarks>
    internal sealed class SpellCastPacket
    {
        /// <summary>A unique ID for this cast, used to reject duplicate packets.</summary>
        public string CastId { get; set; } = "";

        /// <summary>The unique multiplayer ID of the farmer who originally cast the spell.</summary>
        public long CasterId { get; set; }

        /// <summary>The full spell ID, like "elemental:meteor" or "life:heal".</summary>
        public string SpellId { get; set; } = "";

        /// <summary>The prepared spell level used for this cast.</summary>
        public int Level { get; set; }

        /// <summary>The target X coordinate in world pixels.</summary>
        public int TargetX { get; set; }

        /// <summary>The target Y coordinate in world pixels.</summary>
        public int TargetY { get; set; }

        /// <summary>Extra spell-specific packet data.</summary>
        public Dictionary<string, string> Data { get; set; } = new();

        /// <summary>Read a string value from the packet data.</summary>
        public string GetString(string key, string defaultValue = "")
        {
            return this.Data != null && this.Data.TryGetValue(key, out string value)
                ? value ?? defaultValue
                : defaultValue;
        }

        /// <summary>Read an int value from the packet data.</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return int.TryParse(this.GetString(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : defaultValue;
        }

        /// <summary>Read a float value from the packet data.</summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return float.TryParse(this.GetString(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : defaultValue;
        }

        /// <summary>Read a bool value from the packet data.</summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            return bool.TryParse(this.GetString(key), out bool value)
                ? value
                : defaultValue;
        }

        /// <summary>Read a Vector2 value from the packet data using "x,y" format.</summary>
        public Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            string raw = this.GetString(key);
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            string[] parts = raw.Split(',');
            if (parts.Length != 2)
                return defaultValue;

            return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                ? new Vector2(x, y)
                : defaultValue;
        }
    }

    /// <summary>Packet for a local-player-only healing pulse.</summary>
    internal sealed class HealPulsePacket
    {
        /// <summary>A unique ID for this heal pulse, used to reject duplicate packets.</summary>
        public string PulseId { get; set; } = "";

        /// <summary>The unique multiplayer ID of the player or aura owner who created the pulse.</summary>
        public long CasterId { get; set; }

        /// <summary>The spell ID that created the pulse.</summary>
        public string SpellId { get; set; } = "";

        /// <summary>The spell level used for the pulse.</summary>
        public int Level { get; set; }

        /// <summary>The location name where the pulse happened.</summary>
        public string LocationName { get; set; } = "";

        /// <summary>The pulse center X coordinate in world pixels.</summary>
        public int CenterX { get; set; }

        /// <summary>The pulse center Y coordinate in world pixels.</summary>
        public int CenterY { get; set; }

        /// <summary>The pulse radius in world pixels.</summary>
        public float Radius { get; set; }

        /// <summary>The heal amount to apply if the local player is inside the pulse.</summary>
        public int HealAmount { get; set; }

        /// <summary>Extra heal-pulse-specific packet data.</summary>
        public Dictionary<string, string> Data { get; set; } = new();
    }

    [SEvent]
    public class NetworkEvents
    {
        /*********
        ** Constants
        *********/

        /// <summary>Message type used to announce that a spell was cast.</summary>
        private const string SpellCastObservedMessage = "SpellCastObserved";

        /// <summary>Message type used to update durable summon state.</summary>
        private const string SummonStateUpdatedMessage = "SummonStateUpdated";

        /// <summary>Message type used to clear durable summon state.</summary>
        private const string SummonClearedMessage = "SummonCleared";

        /// <summary>Message type used when a client asks peers for owned summon states in a location.</summary>
        private const string SummonStateRequestMessage = "SummonStateRequest";

        /// <summary>Message type used when a peer sends its owned summon states for a location.</summary>
        private const string SummonStateSnapshotMessage = "SummonStateSnapshot";

        /// <summary>Message type used for local-player-only healing pulse checks.</summary>
        private const string HealPulseMessage = "HealPulse";


        /*********
        ** Fields
        *********/

        /// <summary>Whether the SMAPI multiplayer message event has already been registered.</summary>
        private static bool IsInitialized;

        /// <summary>Cast IDs this client has already processed, used to avoid duplicate observed casts.</summary>
        private static readonly HashSet<string> ProcessedCastIds = new();

        /// <summary>Heal pulse IDs this client has already processed, used to avoid duplicate local-player healing checks.</summary>
        private static readonly HashSet<string> ProcessedHealPulseIds = new();


        /*********
        ** Events
        *********/

        /// <summary>Raised when a heal pulse packet is received and should be checked by local-player-only heal logic.</summary>
        internal static event Action<HealPulsePacket> HealPulseReceived;


        /*********
        ** Public methods
        *********/

        /// <summary>Register SMAPI multiplayer message handlers and network hooks.</summary>
        public static void Init()
        {
            if (IsInitialized)
                return;

            ModEntry.Instance.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

            SummonManager.SummonStateBroadcastRequested += BroadcastSummonState;
            SummonManager.SummonClearBroadcastRequested += BroadcastSummonClear;
            SummonManager.SummonStateRequestBroadcastRequested += BroadcastSummonStateRequest;
            SummonManager.SummonStateSnapshotSendRequested += SendSummonStateSnapshot;

            IsInitialized = true;
        }

        /// <summary>Clear temporary network state when returning to title or unloading a save.</summary>
        public static void Reset()
        {
            ProcessedCastIds.Clear();
            ProcessedHealPulseIds.Clear();
        }

        /// <summary>
        /// Route a local spell cast based on its declared sync mode.
        /// </summary>
        /// <param name="player">The local player casting the spell.</param>
        /// <param name="spellBook">The local player's spell book.</param>
        /// <param name="spell">The spell being cast.</param>
        /// <param name="level">The prepared spell level.</param>
        /// <param name="pos">The target position in world pixels.</param>
        public static void DispatchSpellCast(Farmer player, SpellBook spellBook, Spell spell, int level, Point pos)
        {
            if (player == null || spell == null)
                return;

            SpellCastPacket packet = BuildSpellCastPacket(player, spell, level, pos);
            TryRememberProcessedCast(packet.CastId);

            // The caster's own machine always runs the real local spell behavior immediately.
            // Remote machines never replay full OnCast from this packet.
            Events.AddActiveEffect(spell.OnCast(player, level, pos.X, pos.Y));

            switch (spell.SyncMode)
            {
                case SpellSyncMode.LocalOnly:
                    return;

                case SpellSyncMode.LocalWorld:
                case SpellSyncMode.NetworkedEffect:
                    BroadcastSpellCastObserved(packet, includeCaster: false);
                    return;

                case SpellSyncMode.Summon:
                    // Summon spells create durable summon state through SummonManager.
                    // SummonManager sends its own summon-state packet, so no cast-observed packet is needed here.
                    return;

                default:
                    Log.Warn($"Unknown spell sync mode {spell.SyncMode} for {spell.FullId}; local cast already ran, but no packet was sent.");
                    return;
            }
        }

        /// <summary>Broadcast a heal pulse so each client can decide whether its own local player is affected.</summary>
        /// <param name="packet">The heal pulse packet.</param>
        internal static void BroadcastHealPulse(HealPulsePacket packet)
        {
            if (packet == null)
                return;

            if (string.IsNullOrWhiteSpace(packet.PulseId))
                packet.PulseId = Guid.NewGuid().ToString("N");

            TryRememberHealPulse(packet.PulseId);

            long[] recipients = GetOnlinePlayerIdsExceptLocal();
            if (recipients.Length == 0)
                return;

            ModEntry.Instance.Helper.Multiplayer.SendMessage(packet, HealPulseMessage, modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID }, playerIDs: recipients);
        }


        /*********
        ** SMAPI message handling
        *********/

        /// <summary>Handle incoming SMAPI multiplayer messages.</summary>
        private static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModEntry.Instance.ModManifest.UniqueID)
                return;

            switch (e.Type)
            {
                case SpellCastObservedMessage:
                    HandleSpellCastObserved(e.ReadAs<SpellCastPacket>());
                    return;

                case SummonStateUpdatedMessage:
                    SummonManager.ApplySummonStatePacket(e.ReadAs<SummonManager.SummonStatePacket>());
                    return;

                case SummonClearedMessage:
                    SummonManager.ApplySummonClearPacket(e.ReadAs<SummonManager.SummonClearPacket>());
                    return;

                case SummonStateRequestMessage:
                    HandleSummonStateRequest(e.FromPlayerID, e.ReadAs<SummonManager.SummonStateRequestPacket>());
                    return;

                case SummonStateSnapshotMessage:
                    SummonManager.ApplySummonStateSnapshot(e.ReadAs<SummonManager.SummonStateSnapshotPacket>());
                    return;

                case HealPulseMessage:
                    HandleHealPulse(e.ReadAs<HealPulsePacket>());
                    return;
            }
        }

        /// <summary>
        /// Handle an observed spell cast from another player.
        /// </summary>
        /// <param name="packet">The observed spell cast packet.</param>
        private static void HandleSpellCastObserved(SpellCastPacket packet)
        {
            if (!TryRememberProcessedCast(packet?.CastId))
                return;

            Spell spell = ResolveSpell(packet);
            Farmer caster = ResolveCaster(packet);
            if (spell == null || caster == null)
                return;

            IActiveEffect effect = spell.OnRemoteCast(caster, packet.Level, packet.TargetX, packet.TargetY, packet.Data ?? new Dictionary<string, string>());
            Events.AddActiveEffect(effect);
        }

        /// <summary>Handle a peer request for this client's owned summon states in a location.</summary>
        /// <param name="requesterPlayerId">The requesting player's unique multiplayer ID.</param>
        /// <param name="packet">The summon state request packet.</param>
        private static void HandleSummonStateRequest(long requesterPlayerId, SummonManager.SummonStateRequestPacket packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.LocationName))
                return;

            // Each client only replies with its own owned summon states.
            // The host is not special here unless the host is the summon owner.
            SummonManager.SendOwnedSummonStateSnapshot(requesterPlayerId, packet.LocationName);
        }

        /// <summary>Handle a heal pulse packet.</summary>
        /// <param name="packet">The heal pulse packet.</param>
        private static void HandleHealPulse(HealPulsePacket packet)
        {
            if (!TryRememberHealPulse(packet?.PulseId))
                return;

            HealPulseReceived?.Invoke(packet);
        }


        /*********
        ** Sending helpers
        *********/

        /// <summary>
        /// Broadcast an observed spell cast packet to other players.
        /// </summary>
        /// <param name="packet">The spell cast packet to broadcast.</param>
        /// <param name="includeCaster">Whether the original caster should receive this broadcast if they are not the sender.</param>
        private static void BroadcastSpellCastObserved(SpellCastPacket packet, bool includeCaster)
        {
            long[] recipients = GetBroadcastRecipients(packet.CasterId, includeCaster);
            if (recipients.Length == 0)
                return;

            Log.Trace($"Broadcasting observed spell cast {packet.SpellId} from {packet.CasterId} to {recipients.Length} player(s).");

            ModEntry.Instance.Helper.Multiplayer.SendMessage(
                packet,
                SpellCastObservedMessage,
                modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID },
                playerIDs: recipients);
        }

        /// <summary>Broadcast a summon state update packet to other players.</summary>
        /// <param name="packet">The summon state packet.</param>
        private static void BroadcastSummonState(SummonManager.SummonStatePacket packet)
        {
            if (packet == null)
                return;

            long[] recipients = GetOnlinePlayerIdsExceptLocal();
            if (recipients.Length == 0)
                return;

            ModEntry.Instance.Helper.Multiplayer.SendMessage(packet, SummonStateUpdatedMessage, modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID }, playerIDs: recipients);
        }

        /// <summary>Broadcast a summon clear packet to other players.</summary>
        /// <param name="packet">The summon clear packet.</param>
        private static void BroadcastSummonClear(SummonManager.SummonClearPacket packet)
        {
            if (packet == null)
                return;

            long[] recipients = GetOnlinePlayerIdsExceptLocal();
            if (recipients.Length == 0)
                return;

            ModEntry.Instance.Helper.Multiplayer.SendMessage(packet, SummonClearedMessage, modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID }, playerIDs: recipients);
        }

        /// <summary>Ask all peers for their owned summon states in a location.</summary>
        /// <param name="packet">The summon state request packet.</param>
        private static void BroadcastSummonStateRequest(SummonManager.SummonStateRequestPacket packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.LocationName))
                return;

            long[] recipients = GetOnlinePlayerIdsExceptLocal();
            if (recipients.Length == 0)
                return;

            ModEntry.Instance.Helper.Multiplayer.SendMessage(
                packet,
                SummonStateRequestMessage,
                modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID },
                playerIDs: recipients);
        }

        /// <summary>Send this client's owned summon state snapshot to one peer.</summary>
        /// <param name="recipientPlayerId">The recipient player's unique multiplayer ID.</param>
        /// <param name="packet">The owned summon state snapshot packet.</param>
        private static void SendSummonStateSnapshot(long recipientPlayerId, SummonManager.SummonStateSnapshotPacket packet)
        {
            if (packet == null)
                return;

            ModEntry.Instance.Helper.Multiplayer.SendMessage(
                packet,
                SummonStateSnapshotMessage,
                modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID },
                playerIDs: new[] { recipientPlayerId });
        }


        /*********
        ** Packet helpers
        *********/

        /// <summary>
        /// Build a structured spell cast packet from a local cast.
        /// </summary>
        /// <param name="caster">The local player casting the spell.</param>
        /// <param name="spell">The spell being cast.</param>
        /// <param name="level">The prepared spell level.</param>
        /// <param name="pos">The target position in world pixels.</param>
        private static SpellCastPacket BuildSpellCastPacket(Farmer caster, Spell spell, int level, Point pos)
        {
            return new SpellCastPacket
            {
                CastId = Guid.NewGuid().ToString("N"),
                CasterId = caster.UniqueMultiplayerID,
                SpellId = spell.FullId,
                Level = level,
                TargetX = pos.X,
                TargetY = pos.Y,
                Data = spell.BuildPacketData(caster, level, pos.X, pos.Y) ?? new Dictionary<string, string>()
            };
        }

        /// <summary>Resolve the spell referenced by a packet.</summary>
        /// <param name="packet">The spell cast packet.</param>
        private static Spell ResolveSpell(SpellCastPacket packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.SpellId))
                return null;

            Spell spell = SpellManager.Get(packet.SpellId);
            if (spell == null)
                Log.Warn($"Received packet for unknown spell '{packet.SpellId}'.");

            return spell;
        }

        /// <summary>Resolve the caster referenced by a packet.</summary>
        /// <param name="packet">The spell cast packet.</param>
        private static Farmer ResolveCaster(SpellCastPacket packet)
        {
            if (packet == null)
                return null;

            Farmer caster = Game1.GetPlayer(packet.CasterId) ?? Game1.getOnlineFarmers().FirstOrDefault(farmer => farmer?.UniqueMultiplayerID == packet.CasterId);
            if (caster == null)
                Log.Warn($"Received packet for unknown caster '{packet.CasterId}'.");

            return caster;
        }

        /// <summary>
        /// Get the remote player IDs that should receive a spell broadcast from this machine.
        /// </summary>
        /// <param name="casterId">The original caster's unique multiplayer ID.</param>
        /// <param name="includeCaster">Whether the original caster can be a recipient.</param>
        private static long[] GetBroadcastRecipients(long casterId, bool includeCaster)
        {
            long localId = Game1.player.UniqueMultiplayerID;
            List<long> recipients = new();

            foreach (Farmer who in Game1.getOnlineFarmers())
            {
                if (who == null)
                    continue;

                long playerId = who.UniqueMultiplayerID;

                // SMAPI does not need us to send the message back to this same local game instance.
                if (playerId == localId)
                    continue;

                // Usually don't echo an observed cast back to the original caster.
                if (!includeCaster && playerId == casterId)
                    continue;

                recipients.Add(playerId);
            }

            return recipients.ToArray();
        }

        /// <summary>Get all online player IDs except the local player.</summary>
        private static long[] GetOnlinePlayerIdsExceptLocal()
        {
            long localId = Game1.player.UniqueMultiplayerID;
            return Game1.getOnlineFarmers()
                .Where(farmer => farmer != null && farmer.UniqueMultiplayerID != localId)
                .Select(farmer => farmer.UniqueMultiplayerID)
                .ToArray();
        }

        /// <summary>
        /// Track a processed cast ID and reject duplicates.
        /// </summary>
        /// <param name="castId">The unique cast ID to remember.</param>
        private static bool TryRememberProcessedCast(string castId)
        {
            if (string.IsNullOrWhiteSpace(castId))
                return false;

            if (ProcessedCastIds.Count > 500)
                ProcessedCastIds.Clear();

            return ProcessedCastIds.Add(castId);
        }

        /// <summary>
        /// Track a processed heal pulse ID and reject duplicates.
        /// </summary>
        /// <param name="pulseId">The unique heal pulse ID to remember.</param>
        private static bool TryRememberHealPulse(string pulseId)
        {
            if (string.IsNullOrWhiteSpace(pulseId))
                return false;

            if (ProcessedHealPulseIds.Count > 500)
                ProcessedHealPulseIds.Clear();

            return ProcessedHealPulseIds.Add(pulseId);
        }
    }
}
