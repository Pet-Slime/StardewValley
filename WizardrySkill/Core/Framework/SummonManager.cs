using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using WizardrySkill.Core.Framework.Spells.Effects;
using Log = MoonShared.Attributes.Log;

namespace WizardrySkill.Core.Framework
{
    /// <summary>Tracks Wizardry summon state separately from local visual instances.</summary>
    internal static class SummonManager
    {
        /*********
        ** Constants
        *********/
        /// <summary>The number of summon slots each player can have.</summary>
        internal const int MaxSummonSlotsPerPlayer = 2;

        /// <summary>Fallback duration for most summon visuals.</summary>
        private const int DefaultSummonTicksLeft = 60 * 60;

        /// <summary>Fallback lantern duration, matching the current lantern effect behavior.</summary>
        private const int DefaultLanternTicksLeft = 30 * 3 * 60;


        /*********
        ** Fields
        *********/
        /// <summary>The durable summon state, keyed by owner multiplayer ID and slot index.</summary>
        private static readonly Dictionary<SummonKey, SummonState> Summons = new();

        /// <summary>The local visual/effect instances created from durable summon state.</summary>
        private static readonly Dictionary<SummonKey, LocalSummonInstance> LocalVisuals = new();

        /// <summary>
        /// Slot follow offsets relative to the farmer's facing direction.
        /// X = tiles behind the farmer.
        /// Y = tiles to the farmer's left. Negative Y means farmer's right.
        /// </summary>
        private static readonly Vector2[] SlotFollowOffsets =
        {
            new(0.80f, 0.45f),   // slot 0: back-left
            new(0.80f, -0.45f)   // slot 1: back-right
        };


        /*********
        ** Networking hook events
        *********/
        /// <summary>Raised when summon state should be broadcast through NetworkEvents.</summary>
        internal static event Action<SummonStatePacket> SummonStateBroadcastRequested;

        /// <summary>Raised when a summon clear packet should be broadcast through NetworkEvents.</summary>
        internal static event Action<SummonClearPacket> SummonClearBroadcastRequested;

        /// <summary>Raised when this client should ask peers for summon states in a location.</summary>
        internal static event Action<SummonStateRequestPacket> SummonStateRequestBroadcastRequested;

        /// <summary>Raised when this client should send its owned summon state snapshot to one peer.</summary>
        internal static event Action<long, SummonStateSnapshotPacket> SummonStateSnapshotSendRequested;


        /*********
        ** Public operation methods
        *********/
        /// <summary>Add or replace a summon owned by a player.</summary>
        /// <param name="owner">The player who owns the summon.</param>
        /// <param name="defId">The summon definition ID.</param>
        /// <param name="level">The spell level used to create the summon.</param>
        /// <param name="ticksLeft">The summon duration in ticks, or -1 to use the default for the summon type.</param>
        /// <param name="data">Extra summon-specific data.</param>
        /// <param name="preferredSlotIndex">The preferred slot index, or null to choose one automatically.</param>
        /// <param name="broadcast">Whether to request a network broadcast for the new summon state.</param>
        /// <returns>Returns true if the summon was added or replaced.</returns>
        internal static bool TryAddOrReplaceSummon(Farmer owner, string defId, int level, int ticksLeft = -1, IDictionary<string, string> data = null, int? preferredSlotIndex = null, bool broadcast = true)
        {
            if (owner == null || string.IsNullOrWhiteSpace(defId))
                return false;

            if (!IsKnownSummonDef(defId))
            {
                Log.Warn($"Tried to create unknown summon def_id '{defId}'.");
                return false;
            }

            int slotIndex = GetSlotForSummon(owner.UniqueMultiplayerID, defId, preferredSlotIndex);
            int resolvedTicksLeft = ticksLeft > 0 ? ticksLeft : GetDefaultTicksLeft(defId);
            SummonKey key = new(owner.UniqueMultiplayerID, slotIndex);

            SummonState state = new()
            {
                OwnerId = owner.UniqueMultiplayerID,
                SlotIndex = slotIndex,
                DefId = defId,
                Level = level,
                TicksLeft = resolvedTicksLeft,
                LocationName = GetLocationName(owner.currentLocation),
                Data = CopyData(data)
            };

            Summons[key] = state;
            DestroyLocalVisual(key);

            if (broadcast)
                BroadcastSummonState(state);

            RefreshLocalVisuals();
            return true;
        }

        /// <summary>Clear one summon slot for a player.</summary>
        /// <param name="ownerId">The owner player's unique multiplayer ID.</param>
        /// <param name="slotIndex">The summon slot index.</param>
        /// <param name="broadcast">Whether to request a network broadcast for the clear.</param>
        internal static void ClearSummon(long ownerId, int slotIndex, bool broadcast = true)
        {
            if (!IsValidSlotIndex(slotIndex))
                return;

            SummonKey key = new(ownerId, slotIndex);
            DestroyLocalVisual(key);
            Summons.Remove(key);

            if (broadcast)
                BroadcastSummonClear(SummonClearPacket.ForSlot(ownerId, slotIndex));
        }

        /// <summary>Clear all summons owned by a player.</summary>
        /// <param name="ownerId">The owner player's unique multiplayer ID.</param>
        /// <param name="broadcast">Whether to request a network broadcast for the clear.</param>
        internal static void ClearSummons(long ownerId, bool broadcast = true)
        {
            foreach (SummonKey key in Summons.Keys.Where(key => key.OwnerId == ownerId).ToList())
            {
                DestroyLocalVisual(key);
                Summons.Remove(key);
            }

            if (broadcast)
                BroadcastSummonClear(SummonClearPacket.ForOwner(ownerId));
        }

        /// <summary>Clear all summon state and local summon visuals.</summary>
        /// <param name="broadcast">Whether to request a network broadcast for the clear.</param>
        internal static void ClearAllSummons(bool broadcast = true)
        {
            ClearLocalVisuals();
            Summons.Clear();

            if (broadcast)
                BroadcastSummonClear(SummonClearPacket.ForAll());
        }

        /// <summary>Update durable summon timers and local visual instances.</summary>
        /// <param name="e">The update tick event args.</param>
        internal static void Update(UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            foreach (var pair in Summons.ToList())
            {
                SummonState state = pair.Value;
                if (state.TicksLeft <= 0)
                    continue;

                state.TicksLeft--;
                if (state.TicksLeft <= 0)
                    ClearSummon(state.OwnerId, state.SlotIndex, broadcast: IsLocalOwner(state.OwnerId));
            }

            foreach (var pair in LocalVisuals.ToList())
            {
                SummonKey key = pair.Key;
                LocalSummonInstance instance = pair.Value;

                if (!Summons.TryGetValue(key, out SummonState state) || !ShouldStateBeVisible(state, Game1.currentLocation))
                {
                    DestroyLocalVisual(key);
                    continue;
                }

                if (!instance.Effect.Update(e))
                    ClearSummon(state.OwnerId, state.SlotIndex, broadcast: IsLocalOwner(state.OwnerId));
            }

            RefreshLocalVisuals();
        }

        /// <summary>Draw local summon visuals if they draw manually.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        internal static void Draw(SpriteBatch spriteBatch)
        {
            foreach (LocalSummonInstance instance in LocalVisuals.Values.ToList())
                instance.Effect.Draw(spriteBatch);
        }

        /// <summary>Refresh local summon visuals for the current location.</summary>
        internal static void RefreshLocalVisuals()
        {
            RefreshVisualsForLocation(Game1.currentLocation);
        }

        /// <summary>Refresh local summon visuals for a specific location.</summary>
        /// <param name="location">The location whose visible summon visuals should be refreshed.</param>
        internal static void RefreshVisualsForLocation(GameLocation location)
        {
            if (location == null)
                return;

            foreach (SummonKey key in LocalVisuals.Keys.ToList())
            {
                if (!Summons.TryGetValue(key, out SummonState state) || !ShouldStateBeVisible(state, location))
                    DestroyLocalVisual(key);
            }

            foreach (var pair in Summons.ToList())
            {
                if (ShouldStateBeVisible(pair.Value, location))
                    EnsureLocalVisual(pair.Key, pair.Value);
            }
        }

        /// <summary>Handle a local player warp by updating their summon location, requesting peer summons for the new location, and pushing owned summon state.</summary>
        /// <param name="e">The warp event args.</param>
        internal static void OnLocalWarped(WarpedEventArgs e)
        {
            if (e?.IsLocalPlayer == true)
                OnLocalWarped(e.Player, e.OldLocation, e.NewLocation);
        }

        /// <summary>Handle a local player warp by updating their summon location, requesting peer summons for the new location, and pushing owned summon state.</summary>
        /// <param name="player">The local player who warped.</param>
        /// <param name="oldLocation">The previous location.</param>
        /// <param name="newLocation">The new location.</param>
        internal static void OnLocalWarped(Farmer player, GameLocation oldLocation, GameLocation newLocation)
        {
            if (player == null || !player.IsLocalPlayer)
                return;

            string newLocationName = GetLocationName(newLocation);
            long localOwnerId = player.UniqueMultiplayerID;

            // We only care about remote summon state for the location we are entering.
            PruneRemoteSummonsOutsideLocation(newLocationName);

            // Update my own summon states to my new location.
            foreach (SummonState state in Summons.Values.Where(state => state.OwnerId == localOwnerId).ToList())
                state.LocationName = newLocationName;

            // Pull: ask peers for their owned summons in this location.
            RequestSummonStatesForLocation(newLocationName);

            // Push: send my owned summon state for this location to peers.
            BroadcastOwnedSummonStatesForLocation(localOwnerId, newLocationName);

            RefreshVisualsForLocation(newLocation);
        }

        /// <summary>Clear all summons at the start of a new day.</summary>
        internal static void OnDayStarted()
        {
            ClearAllSummons(broadcast: false);
        }

        /// <summary>Clear all summons when the day is ending.</summary>
        internal static void OnDayEnding()
        {
            ClearAllSummons(broadcast: false);
        }

        /// <summary>Clear all temporary summon state when returning to title or unloading a save.</summary>
        internal static void Reset()
        {
            ClearAllSummons(broadcast: false);
        }


        /*********
        ** Public state helpers
        *********/
        /// <summary>Get a summon slot's relative follow offset.</summary>
        /// <param name="slotIndex">The summon slot index.</param>
        internal static Vector2 GetSlotFollowOffset(int slotIndex)
        {
            return SlotFollowOffsets[NormalizeSlotIndex(slotIndex)];
        }

        /// <summary>
        /// Get the ground/shadow anchor position for a flying summon.
        /// This is the summon’s real world position for follow logic and layer depth.
        /// </summary>
        /// <param name="owner">The summon owner.</param>
        /// <param name="slotIndex">The summon slot index.</param>
        internal static Vector2 GetFlyingSummonGroundAnchor(Farmer owner, int slotIndex)
        {
            if (owner == null)
                return Vector2.Zero;

            Vector2 slotOffset = GetSlotFollowOffset(slotIndex);

            Vector2 forward = GetFacingVector(owner.FacingDirection);
            Vector2 behind = -forward;
            Vector2 left = GetLeftVector(forward);

            // Use the farmer's standing / feet position, not raw top-left sprite position.
            Vector2 baseAnchor = owner.getStandingPosition();

            // Slight downward adjustment so the summon sorts from a ground point closer to its visible shadow.
            baseAnchor.Y += 4f;

            Vector2 worldOffset = (behind * slotOffset.X + left * slotOffset.Y) * Game1.tileSize;
            return baseAnchor + worldOffset;
        }

        /// <summary>Get a summon slot's target follow position around an owner.</summary>
        /// <param name="owner">The summon owner.</param>
        /// <param name="slotIndex">The summon slot index.</param>
        internal static Vector2 GetSlotFollowPosition(Farmer owner, int slotIndex)
        {
            return GetFlyingSummonGroundAnchor(owner, slotIndex);
        }

        /// <summary>Get the layer depth for a flying summon body, based on its ground/shadow anchor position.</summary>
        /// <param name="groundPosition">The summon ground/shadow anchor position.</param>
        internal static float GetFlyingSummonBodyLayerDepth(Vector2 groundPosition)
        {
            return groundPosition.Y / 10000f;
        }

        /// <summary>Get the layer depth for a flying summon shadow, slightly behind the body anchor.</summary>
        /// <param name="groundPosition">The summon ground/shadow anchor position.</param>
        internal static float GetFlyingSummonShadowLayerDepth(Vector2 groundPosition)
        {
            return (groundPosition.Y - 8f) / 10000f - 0.000002f;
        }

        /// <summary>Get a copy of the current summon states.</summary>
        internal static IEnumerable<SummonState> GetSummonStates()
        {
            return Summons.Values.Select(state => state.Clone()).ToList();
        }

        /// <summary>Get a copy of the current summon states owned by a player.</summary>
        /// <param name="ownerId">The owner player's unique multiplayer ID.</param>
        internal static IEnumerable<SummonState> GetSummonStates(long ownerId)
        {
            return Summons.Values.Where(state => state.OwnerId == ownerId).Select(state => state.Clone()).ToList();
        }


        /*********
        ** Network-facing methods
        *********/
        /// <summary>Apply a summon state packet received from the network.</summary>
        /// <param name="packet">The summon state packet.</param>
        /// <param name="refreshVisuals">Whether to refresh local visuals after applying the packet.</param>
        internal static void ApplySummonStatePacket(SummonStatePacket packet, bool refreshVisuals = true)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.DefId) || !IsValidSlotIndex(packet.SlotIndex))
                return;

            if (!IsKnownSummonDef(packet.DefId))
            {
                Log.Warn($"Received unknown summon def_id '{packet.DefId}'.");
                return;
            }

            SummonKey key = new(packet.OwnerId, packet.SlotIndex);

            // Remote summon state is only useful if it belongs to the location we are currently viewing.
            // Local-owned state is always accepted, since this machine owns it.
            long localOwnerId = Game1.player?.UniqueMultiplayerID ?? 0;
            string currentLocationName = GetLocationName(Game1.currentLocation);
            bool isLocalOwner = packet.OwnerId == localOwnerId;
            bool isRelevantRemoteLocation = !string.IsNullOrWhiteSpace(packet.LocationName) && packet.LocationName == currentLocationName;

            if (!isLocalOwner && !isRelevantRemoteLocation)
            {
                DestroyLocalVisual(key);
                Summons.Remove(key);

                if (refreshVisuals)
                    RefreshLocalVisuals();

                return;
            }

            bool shouldRecreateVisual = !Summons.TryGetValue(key, out SummonState existingState) || existingState.DefId != packet.DefId || existingState.Level != packet.Level;
            Summons[key] = SummonState.FromPacket(packet);

            if (shouldRecreateVisual)
                DestroyLocalVisual(key);

            if (refreshVisuals)
                RefreshLocalVisuals();
        }

        /// <summary>Apply a summon clear packet received from the network.</summary>
        /// <param name="packet">The summon clear packet.</param>
        /// <param name="refreshVisuals">Whether to refresh local visuals after applying the packet.</param>
        internal static void ApplySummonClearPacket(SummonClearPacket packet, bool refreshVisuals = true)
        {
            if (packet == null)
                return;

            if (packet.ClearAll)
                ClearAllSummons(broadcast: false);
            else if (packet.ClearOwner)
                ClearSummons(packet.OwnerId, broadcast: false);
            else
                ClearSummon(packet.OwnerId, packet.SlotIndex, broadcast: false);

            if (refreshVisuals)
                RefreshLocalVisuals();
        }

        /// <summary>Request summon states from peers for a specific location.</summary>
        /// <param name="locationName">The location to request summon states for.</param>
        internal static void RequestSummonStatesForLocation(string locationName)
        {
            if (!Context.IsWorldReady || string.IsNullOrWhiteSpace(locationName) || Game1.player == null)
                return;

            SummonStateRequestBroadcastRequested?.Invoke(new SummonStateRequestPacket
            {
                RequesterId = Game1.player.UniqueMultiplayerID,
                LocationName = locationName
            });
        }

        /// <summary>Build a packet containing only the local player's owned summon states for a location.</summary>
        /// <param name="locationName">The location to include.</param>
        internal static SummonStateSnapshotPacket BuildOwnedSummonStateSnapshot(string locationName)
        {
            long localOwnerId = Game1.player?.UniqueMultiplayerID ?? 0;
            string currentLocationName = GetLocationName(Game1.player?.currentLocation);

            // Keep my owned summon state fresh before answering a peer request.
            if (!string.IsNullOrWhiteSpace(currentLocationName))
            {
                foreach (SummonState state in Summons.Values.Where(state => state.OwnerId == localOwnerId).ToList())
                    state.LocationName = currentLocationName;
            }

            return new SummonStateSnapshotPacket
            {
                OwnerId = localOwnerId,
                LocationName = locationName ?? "",
                Summons = Summons.Values.Where(state => state.OwnerId == localOwnerId && state.LocationName == locationName).Select(state => state.ToPacket()).ToList()
            };
        }

        /// <summary>Send this client's owned summon states for a location to one peer.</summary>
        /// <param name="recipientPlayerId">The recipient player's unique multiplayer ID.</param>
        /// <param name="locationName">The location being requested.</param>
        internal static void SendOwnedSummonStateSnapshot(long recipientPlayerId, string locationName)
        {
            if (!Context.IsWorldReady || Game1.player == null || string.IsNullOrWhiteSpace(locationName))
                return;

            SummonStateSnapshotSendRequested?.Invoke(recipientPlayerId, BuildOwnedSummonStateSnapshot(locationName));
        }

        /// <summary>Apply an owned summon state snapshot received from another peer.</summary>
        /// <param name="packet">The summon state snapshot.</param>
        internal static void ApplySummonStateSnapshot(SummonStateSnapshotPacket packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.LocationName))
                return;

            long localOwnerId = Game1.player?.UniqueMultiplayerID ?? 0;

            // Never let a peer snapshot overwrite my own summon state.
            if (packet.OwnerId == localOwnerId)
                return;

            // Ignore snapshots for locations we are not currently viewing.
            string currentLocationName = GetLocationName(Game1.currentLocation);
            if (packet.LocationName != currentLocationName)
                return;

            HashSet<SummonKey> incomingKeys = new();

            if (packet.Summons != null)
            {
                foreach (SummonStatePacket summon in packet.Summons)
                {
                    if (summon == null)
                        continue;

                    incomingKeys.Add(new SummonKey(summon.OwnerId, summon.SlotIndex));
                    ApplySummonStatePacket(summon, refreshVisuals: false);
                }
            }

            // If the owner replied with no summon for this location, clear any stale local state for that owner/location.
            foreach (SummonKey key in Summons.Keys.ToList())
            {
                if (key.OwnerId != packet.OwnerId)
                    continue;

                if (!Summons.TryGetValue(key, out SummonState state))
                    continue;

                if (state.LocationName != packet.LocationName)
                    continue;

                if (incomingKeys.Contains(key))
                    continue;

                DestroyLocalVisual(key);
                Summons.Remove(key);
            }

            RefreshLocalVisuals();
        }

        /// <summary>Request that NetworkEvents broadcast a summon state packet.</summary>
        /// <param name="state">The summon state to broadcast.</param>
        internal static void BroadcastSummonState(SummonState state)
        {
            if (state == null)
                return;

            SummonStateBroadcastRequested?.Invoke(state.ToPacket());
        }

        /// <summary>Request that NetworkEvents broadcast a summon clear packet.</summary>
        /// <param name="packet">The summon clear packet to broadcast.</param>
        internal static void BroadcastSummonClear(SummonClearPacket packet)
        {
            if (packet == null)
                return;

            SummonClearBroadcastRequested?.Invoke(packet);
        }


        /*********
        ** Private visual helpers
        *********/
        /// <summary>Ensure a local visual instance exists for a visible summon state.</summary>
        /// <param name="key">The summon key.</param>
        /// <param name="state">The summon state.</param>
        private static void EnsureLocalVisual(SummonKey key, SummonState state)
        {
            if (LocalVisuals.TryGetValue(key, out LocalSummonInstance existing) && existing.DefId == state.DefId && existing.Level == state.Level)
                return;

            DestroyLocalVisual(key);

            Farmer owner = GetPlayer(state.OwnerId);
            if (owner?.currentLocation == null)
                return;

            IActiveEffect effect = CreateVisual(state, owner);
            if (effect == null)
                return;

            LocalVisuals[key] = new LocalSummonInstance(state.DefId, state.Level, effect);
        }

        /// <summary>Create a local visual/effect instance for a summon state.</summary>
        /// <param name="state">The summon state.</param>
        /// <param name="owner">The summon owner.</param>
        private static IActiveEffect CreateVisual(SummonState state, Farmer owner)
        {
            switch (state.DefId)
            {
                case SummonDefs.Lantern:
                    return new LanternEffect(owner, state.SlotIndex, state.Level);

                case SummonDefs.Spirit:
                    return new SpiritEffect(owner, state.SlotIndex, GetDataFloat(state, SummonDataKeys.AttackRange, ModEntry.Config.Spirit_attack_range));

                case SummonDefs.BatArtifact:
                    return new BatArtifactEffect(owner, state.SlotIndex, GetDataFloat(state, SummonDataKeys.AttackRange, 100f));

                case SummonDefs.BatMonster:
                    return new BatMonsterEffect(owner, state.SlotIndex, GetDataFloat(state, SummonDataKeys.AttackRange, 100f));

                default:
                    Log.Warn($"No summon visual factory exists for def_id '{state.DefId}'.");
                    return null;
            }
        }

        /// <summary>Destroy a local visual instance without deleting durable summon state.</summary>
        /// <param name="key">The summon key.</param>
        private static void DestroyLocalVisual(SummonKey key)
        {
            if (!LocalVisuals.TryGetValue(key, out LocalSummonInstance instance))
                return;

            instance.Effect?.CleanUp();
            LocalVisuals.Remove(key);
        }

        /// <summary>Destroy all local visual instances without deleting durable summon state.</summary>
        private static void ClearLocalVisuals()
        {
            foreach (SummonKey key in LocalVisuals.Keys.ToList())
                DestroyLocalVisual(key);
        }

        /// <summary>Get whether a summon state should have a local visual in a location.</summary>
        /// <param name="state">The summon state.</param>
        /// <param name="location">The location to check.</param>
        private static bool ShouldStateBeVisible(SummonState state, GameLocation location)
        {
            if (state == null || location == null)
                return false;

            // Summon state location is the source of truth.
            if (!string.IsNullOrWhiteSpace(state.LocationName))
                return state.LocationName == GetLocationName(location);

            // Fallback only for old/incomplete state.
            Farmer owner = GetPlayer(state.OwnerId);
            return owner?.currentLocation != null && IsSameLocation(owner.currentLocation, location);
        }


        /*********
        ** Private state helpers
        *********/
        /// <summary>Choose the slot to use for a summon.</summary>
        /// <param name="ownerId">The owner player's unique multiplayer ID.</param>
        /// <param name="defId">The summon definition ID.</param>
        /// <param name="preferredSlotIndex">The preferred slot index, or null to choose one automatically.</param>
        private static int GetSlotForSummon(long ownerId, string defId, int? preferredSlotIndex)
        {
            if (preferredSlotIndex.HasValue && IsValidSlotIndex(preferredSlotIndex.Value))
                return preferredSlotIndex.Value;

            foreach (var pair in Summons)
            {
                if (pair.Key.OwnerId == ownerId && pair.Value.DefId == defId)
                    return pair.Key.SlotIndex;
            }

            for (int slotIndex = 0; slotIndex < MaxSummonSlotsPerPlayer; slotIndex++)
            {
                if (!Summons.ContainsKey(new SummonKey(ownerId, slotIndex)))
                    return slotIndex;
            }

            return 0;
        }

        /// <summary>Broadcast all owned summon states for a specific location.</summary>
        /// <param name="ownerId">The owner player's unique multiplayer ID.</param>
        /// <param name="locationName">The location to broadcast states for.</param>
        private static void BroadcastOwnedSummonStatesForLocation(long ownerId, string locationName)
        {
            List<SummonState> ownedStates = Summons.Values.Where(state => state.OwnerId == ownerId && state.LocationName == locationName).ToList();

            if (ownedStates.Count == 0)
            {
                BroadcastSummonClear(SummonClearPacket.ForOwner(ownerId));
                return;
            }

            foreach (SummonState state in ownedStates)
                BroadcastSummonState(state);
        }

        /// <summary>Remove remote summon states which are not in the local player's current location.</summary>
        /// <param name="locationName">The current location name.</param>
        private static void PruneRemoteSummonsOutsideLocation(string locationName)
        {
            long localOwnerId = Game1.player?.UniqueMultiplayerID ?? 0;

            foreach (SummonKey key in Summons.Keys.ToList())
            {
                if (!Summons.TryGetValue(key, out SummonState state))
                    continue;

                if (state.OwnerId == localOwnerId)
                    continue;

                if (state.LocationName == locationName)
                    continue;

                DestroyLocalVisual(key);
                Summons.Remove(key);
            }
        }

        /// <summary>Get the default duration for a summon type.</summary>
        /// <param name="defId">The summon definition ID.</param>
        private static int GetDefaultTicksLeft(string defId)
        {
            return defId switch
            {
                SummonDefs.Lantern => DefaultLanternTicksLeft,
                _ => DefaultSummonTicksLeft
            };
        }

        /// <summary>Get whether a summon definition ID is supported by the visual factory.</summary>
        /// <param name="defId">The summon definition ID.</param>
        private static bool IsKnownSummonDef(string defId)
        {
            return defId == SummonDefs.Lantern || defId == SummonDefs.Spirit || defId == SummonDefs.BatArtifact || defId == SummonDefs.BatMonster;
        }

        /// <summary>Get whether a slot index is valid.</summary>
        /// <param name="slotIndex">The slot index.</param>
        private static bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxSummonSlotsPerPlayer;
        }

        /// <summary>Normalize a slot index into the valid range.</summary>
        /// <param name="slotIndex">The slot index.</param>
        private static int NormalizeSlotIndex(int slotIndex)
        {
            if (slotIndex < 0)
                return 0;

            if (slotIndex >= MaxSummonSlotsPerPlayer)
                return MaxSummonSlotsPerPlayer - 1;

            return slotIndex;
        }

        /// <summary>Get whether the local player owns a summon state.</summary>
        /// <param name="ownerId">The owner player's unique multiplayer ID.</param>
        private static bool IsLocalOwner(long ownerId)
        {
            return Game1.player?.UniqueMultiplayerID == ownerId;
        }

        /// <summary>Get a player by unique multiplayer ID.</summary>
        /// <param name="ownerId">The player's unique multiplayer ID.</param>
        private static Farmer GetPlayer(long ownerId)
        {
            return Game1.GetPlayer(ownerId) ?? Game1.getOnlineFarmers().FirstOrDefault(farmer => farmer?.UniqueMultiplayerID == ownerId);
        }

        /// <summary>Get a stable location name for summon visibility checks.</summary>
        /// <param name="location">The location to read.</param>
        private static string GetLocationName(GameLocation location)
        {
            return location?.Name ?? "";
        }

        /// <summary>Get whether two locations represent the same place.</summary>
        /// <param name="left">The first location.</param>
        /// <param name="right">The second location.</param>
        private static bool IsSameLocation(GameLocation left, GameLocation right)
        {
            if (ReferenceEquals(left, right))
                return true;

            return GetLocationName(left) == GetLocationName(right);
        }

        /// <summary>Copy summon data into a case-insensitive dictionary.</summary>
        /// <param name="data">The data to copy.</param>
        private static Dictionary<string, string> CopyData(IDictionary<string, string> data)
        {
            return data != null
                ? new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Read a float from summon data.</summary>
        /// <param name="state">The summon state.</param>
        /// <param name="key">The data key.</param>
        /// <param name="defaultValue">The default value.</param>
        private static float GetDataFloat(SummonState state, string key, float defaultValue)
        {
            if (state?.Data != null && state.Data.TryGetValue(key, out string raw) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return value;

            return defaultValue;
        }

        /// <summary>Get the world-facing direction vector for a Stardew facing direction.</summary>
        /// <param name="facingDirection">The Stardew facing direction. 0 up, 1 right, 2 down, 3 left.</param>
        private static Vector2 GetFacingVector(int facingDirection)
        {
            return facingDirection switch
            {
                0 => new Vector2(0f, -1f), // north/up
                1 => new Vector2(1f, 0f),  // east/right
                2 => new Vector2(0f, 1f),  // south/down
                3 => new Vector2(-1f, 0f), // west/left
                _ => new Vector2(0f, 1f)
            };
        }

        /// <summary>Get the left vector relative to a forward vector.</summary>
        /// <param name="forward">The forward direction vector.</param>
        private static Vector2 GetLeftVector(Vector2 forward)
        {
            return new Vector2(forward.Y, -forward.X);
        }


        /*********
        ** Summon definitions
        *********/
        /// <summary>Known summon definition keys.</summary>
        internal static class SummonDefs
        {
            public const string Lantern = "lantern";
            public const string Spirit = "spirit";
            public const string BatArtifact = "bat_artifact";
            public const string BatMonster = "bat_monster";
        }

        /// <summary>Known summon data keys.</summary>
        internal static class SummonDataKeys
        {
            public const string AttackRange = "attack_range";
        }


        /*********
        ** Packet models
        *********/
        /// <summary>Network packet for one durable summon state.</summary>
        internal sealed class SummonStatePacket
        {
            public long OwnerId { get; set; }
            public int SlotIndex { get; set; }
            public string DefId { get; set; } = "";
            public int Level { get; set; }
            public int TicksLeft { get; set; }
            public string LocationName { get; set; } = "";
            public Dictionary<string, string> Data { get; set; } = new();
        }

        /// <summary>Network packet for clearing summon state.</summary>
        internal sealed class SummonClearPacket
        {
            public long OwnerId { get; set; }
            public int SlotIndex { get; set; } = -1;
            public bool ClearOwner { get; set; }
            public bool ClearAll { get; set; }

            public static SummonClearPacket ForSlot(long ownerId, int slotIndex)
            {
                return new SummonClearPacket
                {
                    OwnerId = ownerId,
                    SlotIndex = slotIndex
                };
            }

            public static SummonClearPacket ForOwner(long ownerId)
            {
                return new SummonClearPacket
                {
                    OwnerId = ownerId,
                    ClearOwner = true
                };
            }

            public static SummonClearPacket ForAll()
            {
                return new SummonClearPacket
                {
                    ClearAll = true
                };
            }
        }

        /// <summary>Network packet for requesting peer-owned summon states in a location.</summary>
        internal sealed class SummonStateRequestPacket
        {
            public long RequesterId { get; set; }
            public string LocationName { get; set; } = "";
        }

        /// <summary>Network packet containing one owner's summon states for one location.</summary>
        internal sealed class SummonStateSnapshotPacket
        {
            public long OwnerId { get; set; }
            public string LocationName { get; set; } = "";
            public List<SummonStatePacket> Summons { get; set; } = new();
        }


        /*********
        ** State models
        *********/
        /// <summary>Durable summon state which can be networked and used to recreate local visual instances.</summary>
        internal sealed class SummonState
        {
            public long OwnerId { get; set; }
            public int SlotIndex { get; set; }
            public string DefId { get; set; } = "";
            public int Level { get; set; }
            public int TicksLeft { get; set; }
            public string LocationName { get; set; } = "";
            public Dictionary<string, string> Data { get; set; } = new();

            public SummonState Clone()
            {
                return new SummonState
                {
                    OwnerId = this.OwnerId,
                    SlotIndex = this.SlotIndex,
                    DefId = this.DefId,
                    Level = this.Level,
                    TicksLeft = this.TicksLeft,
                    LocationName = this.LocationName,
                    Data = CopyData(this.Data)
                };
            }

            public SummonStatePacket ToPacket()
            {
                return new SummonStatePacket
                {
                    OwnerId = this.OwnerId,
                    SlotIndex = this.SlotIndex,
                    DefId = this.DefId,
                    Level = this.Level,
                    TicksLeft = this.TicksLeft,
                    LocationName = this.LocationName,
                    Data = CopyData(this.Data)
                };
            }

            public static SummonState FromPacket(SummonStatePacket packet)
            {
                return new SummonState
                {
                    OwnerId = packet.OwnerId,
                    SlotIndex = packet.SlotIndex,
                    DefId = packet.DefId ?? "",
                    Level = packet.Level,
                    TicksLeft = packet.TicksLeft,
                    LocationName = packet.LocationName ?? "",
                    Data = CopyData(packet.Data)
                };
            }
        }

        /// <summary>A local visual/effect instance created from durable summon state.</summary>
        private sealed class LocalSummonInstance
        {
            public string DefId { get; }
            public int Level { get; }
            public IActiveEffect Effect { get; }

            public LocalSummonInstance(string defId, int level, IActiveEffect effect)
            {
                this.DefId = defId;
                this.Level = level;
                this.Effect = effect;
            }
        }

        /// <summary>Dictionary key for a player's summon slot.</summary>
        private readonly struct SummonKey : IEquatable<SummonKey>
        {
            public long OwnerId { get; }
            public int SlotIndex { get; }

            public SummonKey(long ownerId, int slotIndex)
            {
                this.OwnerId = ownerId;
                this.SlotIndex = slotIndex;
            }

            public bool Equals(SummonKey other)
            {
                return this.OwnerId == other.OwnerId && this.SlotIndex == other.SlotIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is SummonKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.OwnerId, this.SlotIndex);
            }
        }
    }
}
