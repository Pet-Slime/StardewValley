using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StardewValley;
using StardewValley.Locations;
using Log = MoonShared.Attributes.Log;

namespace WizardrySkill.Core.Framework.Game.Interface
{
    /// <summary>
    /// Finds player-style routes between outdoor maps using player-facing map warps.
    /// </summary>
    /// <remarks>
    /// This is based on the useful part of vanilla's WarpPathfindingCache:
    /// build a cached table of location-to-location routes, then query the cache later.
    ///
    /// Unlike vanilla's NPC cache, this is player-facing and outdoor-only:
    /// - Farm is allowed.
    /// - Backwoods is allowed.
    /// - Indoor maps are ignored.
    /// - Door/building exploration is ignored.
    /// - Generated mine and volcano levels are ignored.
    ///
    /// The cache is split into three pieces:
    /// 1. OutdoorWarpGraph:
    ///    source location -> outdoor target locations.
    ///
    /// 2. ReverseOutdoorWarpGraph:
    ///    target location -> outdoor source locations.
    ///
    /// 3. Routes:
    ///    cached route paths between known outdoor locations.
    ///
    /// Known outdoor locations come from the player's Wizardry teleport modData keys.
    ///
    /// On spell cast:
    ///     EnsureCache(...) builds the cache if it is missing.
    ///
    /// On day start:
    ///     PopulateCache(...) fully rebuilds the graph and route table.
    ///
    /// On new outdoor location:
    ///     AddKnownOutdoorLocation(...) adds the modData key and tries to expand routes
    ///     around the new location. If the cache is missing/stale/weird, it falls back to
    ///     a full rebuild.
    /// </remarks>
    internal static class PlayerRoutePathfinder
    {
        /*********
        ** Constants
        *********/
        private const string TeleportKeyPrefix = "moonslime.Wizardry.TeleportTo.";

        /// <summary>
        /// Hard safety cap for route length to avoid pathological custom-map loops.
        /// </summary>
        private const int MaxRouteLength = 256;


        /*********
        ** Fields
        *********/
        /// <summary>
        /// Outdoor warp graph, indexed by source location name.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> OutdoorWarpGraph = new(StringComparer.Ordinal);

        /// <summary>
        /// Reverse outdoor warp graph, indexed by target location name.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> ReverseOutdoorWarpGraph = new(StringComparer.Ordinal);

        /// <summary>
        /// Cached outdoor routes, indexed by start location, then destination location.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, PlayerLocationRoute>> Routes = new(StringComparer.Ordinal);

        /// <summary>
        /// Outdoor locations the local player has discovered and Wizardry is allowed to route through.
        /// </summary>
        private static HashSet<string> KnownOutdoorLocations = new(StringComparer.Ordinal);

        /// <summary>
        /// Whether the graph and route cache have been populated.
        /// </summary>
        private static bool CacheReady;

        /// <summary>
        /// Save ID used when the cache was last populated.
        /// </summary>
        private static ulong CachedSaveId;

        /// <summary>
        /// Local player ID used when the cache was last populated.
        /// </summary>
        private static long CachedPlayerId;

        /// <summary>
        /// Signature of the known outdoor location set used when the cache was last populated.
        /// </summary>
        private static string CachedKnownLocationSignature = "";


        /*********
        ** Private models
        *********/
        private sealed class PlayerLocationRoute
        {
            /// <summary>The location names in the route, including the start and destination.</summary>
            public string[] LocationNames { get; }

            public PlayerLocationRoute(string[] locationNames)
            {
                this.LocationNames = locationNames;
            }
        }


        /*********
        ** Public methods
        *********/
        /// <summary>
        /// Clear all cached outdoor route data.
        /// </summary>
        /// <remarks>
        /// Call this when returning to title or when maps/warps are edited while the save is loaded.
        /// Normal player warps do not need to call this.
        /// </remarks>
        public static void Reset()
        {
            Log.Mute(() => "[PlayerRoutePathfinder] Resetting outdoor route cache.");

            OutdoorWarpGraph.Clear();
            ReverseOutdoorWarpGraph.Clear();
            Routes.Clear();
            KnownOutdoorLocations.Clear();

            CacheReady = false;
            CachedSaveId = 0;
            CachedPlayerId = 0;
            CachedKnownLocationSignature = "";
        }

        /// <summary>
        /// Ensure the player-facing outdoor route cache exists.
        /// </summary>
        /// <remarks>
        /// This is safe to call from the teleport spell before opening the menu.
        /// If the cache is already valid, this is cheap.
        /// </remarks>
        public static void EnsureCache(Farmer player)
        {
            if (player == null)
            {
                Log.Mute(() => "[PlayerRoutePathfinder] EnsureCache skipped: player is null.");
                return;
            }

            if (!CacheReady || !IsCacheOwner(player))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] EnsureCache rebuilding: CacheReady={CacheReady}, IsCacheOwner={IsCacheOwner(player)}, Player={player.Name}, PlayerId={player.UniqueMultiplayerID}.");
                PopulateCache(player);
                return;
            }

            HashSet<string> currentKnownLocations = GetKnownOutdoorLocationNames(player);
            string currentKnownSignature = BuildKnownLocationSignature(currentKnownLocations);

            if (CachedKnownLocationSignature == currentKnownSignature)
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] EnsureCache cache is valid. Known={KnownOutdoorLocations.Count}, GraphLocations={OutdoorWarpGraph.Count}, GraphEdges={CountGraphEdges()}, RouteStarts={Routes.Count}, Routes={CountRoutes()}.");
                return;
            }

            Log.Mute(() => $"[PlayerRoutePathfinder] EnsureCache known-location signature changed. CachedKnown={KnownOutdoorLocations.Count}, CurrentKnown={currentKnownLocations.Count}. Trying incremental update.");

            if (TryApplyKnownLocationDelta(player, currentKnownLocations))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] EnsureCache incremental update succeeded. Known={KnownOutdoorLocations.Count}, RouteStarts={Routes.Count}, Routes={CountRoutes()}.");
                return;
            }

            Log.Mute(() => "[PlayerRoutePathfinder] EnsureCache incremental update failed. Falling back to full cache rebuild.");
            PopulateCache(player);
        }

        /// <summary>
        /// Build the player-facing outdoor route cache from scratch.
        /// </summary>
        /// <remarks>
        /// Call this at day start, or when the cache is missing/stale/invalid.
        /// </remarks>
        public static void PopulateCache(Farmer player)
        {
            if (player == null)
            {
                Log.Mute(() => "[PlayerRoutePathfinder] PopulateCache called with null player. Resetting cache.");
                Reset();
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Mute(() => $"[PlayerRoutePathfinder] PopulateCache started. Player={player.Name}, PlayerId={player.UniqueMultiplayerID}, LoadedLocations={Game1.locations.Count}.");

            BuildOutdoorWarpGraph();

            Routes.Clear();
            KnownOutdoorLocations = GetKnownOutdoorLocationNames(player);

            Log.Mute(() => $"[PlayerRoutePathfinder] PopulateCache building routes. KnownOutdoorLocations={KnownOutdoorLocations.Count}, GraphLocations={OutdoorWarpGraph.Count}, GraphEdges={CountGraphEdges()}.");

            foreach (string startLocationName in KnownOutdoorLocations)
                BuildRoutesFrom(startLocationName);

            CacheReady = true;
            CachedSaveId = Game1.uniqueIDForThisGame;
            CachedPlayerId = player.UniqueMultiplayerID;
            CachedKnownLocationSignature = BuildKnownLocationSignature(KnownOutdoorLocations);

            stopwatch.Stop();

            Log.Mute(() => $"[PlayerRoutePathfinder] PopulateCache finished in {stopwatch.ElapsedMilliseconds}ms. Known={KnownOutdoorLocations.Count}, GraphLocations={OutdoorWarpGraph.Count}, GraphEdges={CountGraphEdges()}, ReverseGraphLocations={ReverseOutdoorWarpGraph.Count}, RouteStarts={Routes.Count}, Routes={CountRoutes()}.");
        }

        /// <summary>
        /// Add a known outdoor location and update the route cache if possible.
        /// </summary>
        /// <remarks>
        /// This is intended for the local player's Warped event.
        /// It adds the Wizardry modData key if needed, then tries to incrementally expand the route table.
        /// If the cache is missing, stale, or changed in a way that is more complex than one new location,
        /// it falls back to a full rebuild.
        /// </remarks>
        public static void AddKnownOutdoorLocation(Farmer player, string locationName)
        {
            if (player == null || string.IsNullOrWhiteSpace(locationName))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] AddKnownOutdoorLocation skipped: player null or location name empty. LocationName={locationName ?? "(null)"}.");
                return;
            }

            Log.Mute(() => $"[PlayerRoutePathfinder] AddKnownOutdoorLocation requested. Player={player.Name}, PlayerId={player.UniqueMultiplayerID}, Location={locationName}.");

            locationName = ResolveWarpTargetName(locationName);

            GameLocation location = Game1.getLocationFromName(locationName);
            if (!CanUseOutdoorLocation(location))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] AddKnownOutdoorLocation skipped: location is not usable outdoor location. Location={locationName}.");
                return;
            }

            string canonicalLocationName = location.Name;
            string teleportKey = TeleportKeyPrefix + canonicalLocationName;

            if (!player.modData.ContainsKey(teleportKey))
            {
                player.modData[teleportKey] = "";
                Log.Mute(() => $"[PlayerRoutePathfinder] Added Wizardry teleport discovery key: {teleportKey}.");
            }
            else
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] Wizardry teleport discovery key already exists: {teleportKey}.");
            }

            if (!CacheReady || !IsCacheOwner(player))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] AddKnownOutdoorLocation rebuilding: CacheReady={CacheReady}, IsCacheOwner={IsCacheOwner(player)}.");
                PopulateCache(player);
                return;
            }

            HashSet<string> currentKnownLocations = GetKnownOutdoorLocationNames(player);

            Log.Mute(() => $"[PlayerRoutePathfinder] AddKnownOutdoorLocation trying incremental update. CachedKnown={KnownOutdoorLocations.Count}, CurrentKnown={currentKnownLocations.Count}, NewLocation={canonicalLocationName}.");

            if (TryApplyKnownLocationDelta(player, currentKnownLocations))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] AddKnownOutdoorLocation incremental update succeeded. Known={KnownOutdoorLocations.Count}, Routes={CountRoutes()}.");
                return;
            }

            Log.Mute(() => "[PlayerRoutePathfinder] AddKnownOutdoorLocation incremental update failed. Falling back to full cache rebuild.");
            PopulateCache(player);
        }

        /// <summary>
        /// Add a known outdoor location and update the route cache if possible.
        /// </summary>
        public static void AddKnownOutdoorLocation(Farmer player, GameLocation location)
        {
            if (location == null)
            {
                Log.Mute(() => "[PlayerRoutePathfinder] AddKnownOutdoorLocation skipped: location is null.");
                return;
            }

            AddKnownOutdoorLocation(player, location.Name);
        }

        /// <summary>
        /// Get every outdoor location the player can reach from their current outdoor location.
        /// </summary>
        /// <param name="player">The local player.</param>
        public static HashSet<string> GetReachableLocations(Farmer player)
        {
            HashSet<string> reachable = new(StringComparer.Ordinal);

            if (player?.currentLocation == null)
            {
                Log.Mute(() => "[PlayerRoutePathfinder] GetReachableLocations returned empty: player or current location is null.");
                return reachable;
            }

            EnsureCache(player);

            string startLocationName = player.currentLocation.Name;
            if (string.IsNullOrWhiteSpace(startLocationName))
            {
                Log.Mute(() => "[PlayerRoutePathfinder] GetReachableLocations returned empty: start location name is empty.");
                return reachable;
            }

            if (!KnownOutdoorLocations.Contains(startLocationName))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] GetReachableLocations returned empty: start location is not known. Start={startLocationName}, Known={KnownOutdoorLocations.Count}.");
                return reachable;
            }

            reachable.Add(startLocationName);

            if (!Routes.TryGetValue(startLocationName, out Dictionary<string, PlayerLocationRoute> routesByDestination))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] GetReachableLocations found only start location: no routes from {startLocationName}.");
                return reachable;
            }

            foreach (string destinationName in routesByDestination.Keys)
                reachable.Add(destinationName);

            Log.Mute(() => $"[PlayerRoutePathfinder] GetReachableLocations finished. Start={startLocationName}, Reachable={reachable.Count}, RoutesFromStart={routesByDestination.Count}.");
            return reachable;
        }

        /// <summary>
        /// Get a cached outdoor route from the player's current location to a destination location.
        /// </summary>
        /// <param name="player">The local player.</param>
        /// <param name="endingLocationName">The destination location's internal name.</param>
        public static string[] GetLocationRoute(Farmer player, string endingLocationName)
        {
            if (player?.currentLocation == null || string.IsNullOrWhiteSpace(endingLocationName))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] GetLocationRoute returned null: player/current location null or ending location empty. Ending={endingLocationName ?? "(null)"}.");
                return null;
            }

            EnsureCache(player);

            string startingLocationName = player.currentLocation.Name;
            if (string.IsNullOrWhiteSpace(startingLocationName))
            {
                Log.Mute(() => "[PlayerRoutePathfinder] GetLocationRoute returned null: starting location name is empty.");
                return null;
            }

            endingLocationName = ResolveWarpTargetName(endingLocationName);

            GameLocation endingLocation = Game1.getLocationFromName(endingLocationName);
            if (endingLocation != null)
                endingLocationName = endingLocation.Name;

            if (startingLocationName.Equals(endingLocationName, StringComparison.Ordinal))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] GetLocationRoute returned same-location route. Location={startingLocationName}.");
                return new[] { startingLocationName };
            }

            if (!Routes.TryGetValue(startingLocationName, out Dictionary<string, PlayerLocationRoute> routesByDestination))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] GetLocationRoute returned null: no route table for start. Start={startingLocationName}, End={endingLocationName}.");
                return null;
            }

            if (!routesByDestination.TryGetValue(endingLocationName, out PlayerLocationRoute route))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] GetLocationRoute returned null: destination not reachable. Start={startingLocationName}, End={endingLocationName}, RoutesFromStart={routesByDestination.Count}.");
                return null;
            }

            Log.Mute(() => $"[PlayerRoutePathfinder] GetLocationRoute found route. Start={startingLocationName}, End={endingLocationName}, Length={route.LocationNames.Length}, Route={string.Join(" -> ", route.LocationNames)}.");
            return route.LocationNames.ToArray();
        }

        /// <summary>
        /// Check whether the player has a cached outdoor route to a destination location.
        /// </summary>
        /// <param name="player">The local player.</param>
        /// <param name="endingLocationName">The destination location's internal name.</param>
        public static bool HasLocationRoute(Farmer player, string endingLocationName)
        {
            bool result = GetLocationRoute(player, endingLocationName) != null;
            Log.Mute(() => $"[PlayerRoutePathfinder] HasLocationRoute checked. End={endingLocationName ?? "(null)"}, Result={result}.");
            return result;
        }


        /*********
        ** Graph methods
        *********/
        /// <summary>
        /// Build the outdoor-only warp graph from loaded game locations.
        /// </summary>
        private static void BuildOutdoorWarpGraph()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            OutdoorWarpGraph.Clear();
            ReverseOutdoorWarpGraph.Clear();

            int checkedLocations = 0;
            int usableOutdoorLocations = 0;
            int checkedWarps = 0;
            int acceptedWarps = 0;

            foreach (GameLocation location in Game1.locations)
            {
                checkedLocations++;

                if (!CanUseOutdoorLocation(location))
                    continue;

                usableOutdoorLocations++;
                EnsureGraphLocation(location.Name);

                foreach (Warp warp in location.warps)
                {
                    checkedWarps++;

                    if (warp == null || string.IsNullOrWhiteSpace(warp.TargetName))
                        continue;

                    string targetName = ResolveWarpTargetName(warp.TargetName);
                    GameLocation targetLocation = Game1.getLocationFromName(targetName);

                    if (!CanUseOutdoorLocation(targetLocation))
                        continue;

                    AddGraphEdge(location.Name, targetLocation.Name);
                    acceptedWarps++;
                }
            }

            stopwatch.Stop();

            Log.Mute(() => $"[PlayerRoutePathfinder] BuildOutdoorWarpGraph finished in {stopwatch.ElapsedMilliseconds}ms. CheckedLocations={checkedLocations}, UsableOutdoorLocations={usableOutdoorLocations}, CheckedWarps={checkedWarps}, AcceptedOutdoorWarps={acceptedWarps}, GraphLocations={OutdoorWarpGraph.Count}, GraphEdges={CountGraphEdges()}.");
        }

        /// <summary>
        /// Ensure a location exists in both graph dictionaries, even if it has no outgoing/incoming edges.
        /// </summary>
        private static void EnsureGraphLocation(string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName))
                return;

            if (!OutdoorWarpGraph.ContainsKey(locationName))
                OutdoorWarpGraph[locationName] = new HashSet<string>(StringComparer.Ordinal);

            if (!ReverseOutdoorWarpGraph.ContainsKey(locationName))
                ReverseOutdoorWarpGraph[locationName] = new HashSet<string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Add a directed outdoor warp edge.
        /// </summary>
        private static void AddGraphEdge(string sourceLocationName, string targetLocationName)
        {
            if (string.IsNullOrWhiteSpace(sourceLocationName) || string.IsNullOrWhiteSpace(targetLocationName))
                return;

            EnsureGraphLocation(sourceLocationName);
            EnsureGraphLocation(targetLocationName);

            OutdoorWarpGraph[sourceLocationName].Add(targetLocationName);
            ReverseOutdoorWarpGraph[targetLocationName].Add(sourceLocationName);
        }


        /*********
        ** Route cache methods
        *********/
        /// <summary>
        /// Try to update the cache when the known outdoor location set has changed.
        /// </summary>
        /// <remarks>
        /// This only handles the common case where exactly one new location was added.
        /// If locations were removed, many were added at once, or the cache is otherwise weird,
        /// this returns false so the caller can do a full rebuild.
        /// </remarks>
        private static bool TryApplyKnownLocationDelta(Farmer player, HashSet<string> currentKnownLocations)
        {
            if (player == null || currentKnownLocations == null)
            {
                Log.Mute(() => "[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: player or current known locations is null.");
                return false;
            }

            if (!CacheReady || !IsCacheOwner(player))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: cache not ready or wrong owner. CacheReady={CacheReady}, IsCacheOwner={IsCacheOwner(player)}.");
                return false;
            }

            List<string> removedLocations = KnownOutdoorLocations
                .Where(locationName => !currentKnownLocations.Contains(locationName))
                .ToList();

            if (removedLocations.Count > 0)
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: locations were removed. Removed={removedLocations.Count}, Names={string.Join(", ", removedLocations)}.");
                return false;
            }

            List<string> addedLocations = currentKnownLocations
                .Where(locationName => !KnownOutdoorLocations.Contains(locationName))
                .ToList();

            if (addedLocations.Count == 0)
            {
                CachedKnownLocationSignature = BuildKnownLocationSignature(KnownOutdoorLocations);
                Log.Mute(() => "[PlayerRoutePathfinder] TryApplyKnownLocationDelta succeeded: no added locations, signature refreshed.");
                return true;
            }

            if (addedLocations.Count != 1)
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: more than one added location. Added={addedLocations.Count}, Names={string.Join(", ", addedLocations)}.");
                return false;
            }

            string newLocationName = addedLocations[0];
            GameLocation newLocation = Game1.getLocationFromName(newLocationName);

            if (!CanUseOutdoorLocation(newLocation))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: added location is not usable outdoor location. NewLocation={newLocationName}.");
                return false;
            }

            // If the graph doesn't know about this location, the loaded-location/warp state
            // probably changed since the graph was built. Rebuild fully.
            if (!OutdoorWarpGraph.ContainsKey(newLocation.Name) && !ReverseOutdoorWarpGraph.ContainsKey(newLocation.Name))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: graph does not contain new location. NewLocation={newLocation.Name}.");
                return false;
            }

            Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta applying one-location update. NewLocation={newLocation.Name}, KnownBefore={KnownOutdoorLocations.Count}, RoutesBefore={CountRoutes()}.");

            KnownOutdoorLocations.Add(newLocation.Name);

            if (!TryExpandRoutesThroughNewLocation(newLocation.Name))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta failed: TryExpandRoutesThroughNewLocation returned false. NewLocation={newLocation.Name}.");
                return false;
            }

            CachedKnownLocationSignature = BuildKnownLocationSignature(KnownOutdoorLocations);

            Log.Mute(() => $"[PlayerRoutePathfinder] TryApplyKnownLocationDelta succeeded. NewLocation={newLocation.Name}, KnownAfter={KnownOutdoorLocations.Count}, RoutesAfter={CountRoutes()}.");
            return true;
        }

        /// <summary>
        /// Build routes from one known outdoor location to every reachable known outdoor location.
        /// </summary>
        private static void BuildRoutesFrom(string startLocationName)
        {
            if (string.IsNullOrWhiteSpace(startLocationName))
            {
                Log.Mute(() => "[PlayerRoutePathfinder] BuildRoutesFrom skipped: start location name is empty.");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            int routesBefore = CountRoutesForStart(startLocationName);
            int attemptedRoutes = 0;

            foreach (string[] route in FindRoutesFrom(startLocationName))
            {
                attemptedRoutes++;
                AddRoute(route);
            }

            stopwatch.Stop();

            int routesAfter = CountRoutesForStart(startLocationName);
            Log.Mute(() => $"[PlayerRoutePathfinder] BuildRoutesFrom finished in {stopwatch.ElapsedMilliseconds}ms. Start={startLocationName}, AttemptedRoutes={attemptedRoutes}, AddedRoutes={routesAfter - routesBefore}, TotalRoutesFromStart={routesAfter}.");
        }

        /// <summary>
        /// Try to incrementally expand the route table around a newly known location.
        /// </summary>
        /// <remarks>
        /// This adds:
        /// - new location -> old reachable locations
        /// - old reachable locations -> new location
        /// - old reachable locations -> old locations through the new location, if the new location bridges regions
        /// </remarks>
        private static bool TryExpandRoutesThroughNewLocation(string newLocationName)
        {
            if (string.IsNullOrWhiteSpace(newLocationName))
            {
                Log.Mute(() => "[PlayerRoutePathfinder] TryExpandRoutesThroughNewLocation failed: new location name is empty.");
                return false;
            }

            if (!KnownOutdoorLocations.Contains(newLocationName))
            {
                Log.Mute(() => $"[PlayerRoutePathfinder] TryExpandRoutesThroughNewLocation failed: new location is not known. NewLocation={newLocationName}.");
                return false;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            int routesBefore = CountRoutes();

            List<string[]> routesFromNewLocation = FindRoutesFrom(newLocationName).ToList();

            Log.Mute(() => $"[PlayerRoutePathfinder] TryExpandRoutesThroughNewLocation found routes from new location. NewLocation={newLocationName}, RoutesFromNewLocation={routesFromNewLocation.Count}.");

            foreach (string[] routeFromNewLocation in routesFromNewLocation)
                AddRoute(routeFromNewLocation);

            int startsChecked = 0;
            int routesToNewLocationFound = 0;
            int combinedRoutesAttempted = 0;

            foreach (string startLocationName in KnownOutdoorLocations.ToArray())
            {
                if (startLocationName.Equals(newLocationName, StringComparison.Ordinal))
                    continue;

                startsChecked++;

                string[] routeToNewLocation = FindRoute(startLocationName, newLocationName);
                if (routeToNewLocation == null)
                    continue;

                routesToNewLocationFound++;
                AddRoute(routeToNewLocation);

                foreach (string[] routeFromNewLocation in routesFromNewLocation)
                {
                    string[] combinedRoute = CombineRoutes(routeToNewLocation, routeFromNewLocation);
                    if (combinedRoute == null)
                        continue;

                    combinedRoutesAttempted++;
                    AddRoute(combinedRoute);
                }
            }

            stopwatch.Stop();

            Log.Mute(() => $"[PlayerRoutePathfinder] TryExpandRoutesThroughNewLocation finished in {stopwatch.ElapsedMilliseconds}ms. NewLocation={newLocationName}, StartsChecked={startsChecked}, RoutesToNewLocationFound={routesToNewLocationFound}, RoutesFromNewLocation={routesFromNewLocation.Count}, CombinedRoutesAttempted={combinedRoutesAttempted}, RoutesBefore={routesBefore}, RoutesAfter={CountRoutes()}.");
            return true;
        }

        /// <summary>
        /// Add a route to the route table.
        /// </summary>
        private static void AddRoute(string[] route)
        {
            if (route == null || route.Length <= 1)
                return;

            string startLocationName = route[0];
            string destinationLocationName = route[route.Length - 1];

            if (string.IsNullOrWhiteSpace(startLocationName) || string.IsNullOrWhiteSpace(destinationLocationName))
                return;

            if (startLocationName.Equals(destinationLocationName, StringComparison.Ordinal))
                return;

            if (!KnownOutdoorLocations.Contains(startLocationName) || !KnownOutdoorLocations.Contains(destinationLocationName))
                return;

            if (!Routes.TryGetValue(startLocationName, out Dictionary<string, PlayerLocationRoute> routesByDestination))
                Routes[startLocationName] = routesByDestination = new Dictionary<string, PlayerLocationRoute>(StringComparer.Ordinal);

            // Keep the first route found, like vanilla effectively does when GetLocationRoute returns
            // the first matching destination route.
            if (routesByDestination.ContainsKey(destinationLocationName))
                return;

            routesByDestination[destinationLocationName] = new PlayerLocationRoute(route.ToArray());
        }

        /// <summary>
        /// Find one route from a start location to every reachable known outdoor destination.
        /// </summary>
        private static IEnumerable<string[]> FindRoutesFrom(string startLocationName)
        {
            if (string.IsNullOrWhiteSpace(startLocationName))
                yield break;

            if (!KnownOutdoorLocations.Contains(startLocationName))
                yield break;

            if (!OutdoorWarpGraph.ContainsKey(startLocationName))
                yield break;

            Log.Mute(() => $"[PlayerRoutePathfinder] FindRoutesFrom started. Start={startLocationName}, Known={KnownOutdoorLocations.Count}.");

            Queue<string[]> open = new();
            HashSet<string> visited = new(StringComparer.Ordinal)
            {
                startLocationName
            };

            open.Enqueue(new[] { startLocationName });

            int checkedRoutes = 0;
            int yieldedRoutes = 0;
            int cappedRoutes = 0;

            while (open.Count > 0)
            {
                string[] route = open.Dequeue();
                checkedRoutes++;

                if (route.Length >= MaxRouteLength)
                {
                    cappedRoutes++;
                    continue;
                }

                string currentLocationName = route[route.Length - 1];
                if (!OutdoorWarpGraph.TryGetValue(currentLocationName, out HashSet<string> targets))
                    continue;

                foreach (string targetLocationName in GetSortedLocationNames(targets))
                {
                    if (!KnownOutdoorLocations.Contains(targetLocationName))
                        continue;

                    if (!visited.Add(targetLocationName))
                        continue;

                    string[] nextRoute = AppendRouteLocation(route, targetLocationName);

                    yieldedRoutes++;
                    yield return nextRoute;
                    open.Enqueue(nextRoute);
                }
            }

            Log.Mute(() => $"[PlayerRoutePathfinder] FindRoutesFrom finished. Start={startLocationName}, CheckedRoutes={checkedRoutes}, YieldedRoutes={yieldedRoutes}, Visited={visited.Count}, CappedRoutes={cappedRoutes}.");
        }

        /// <summary>
        /// Find one route between two known outdoor locations.
        /// </summary>
        private static string[] FindRoute(string startLocationName, string destinationLocationName)
        {
            if (string.IsNullOrWhiteSpace(startLocationName) || string.IsNullOrWhiteSpace(destinationLocationName))
                return null;

            if (!KnownOutdoorLocations.Contains(startLocationName) || !KnownOutdoorLocations.Contains(destinationLocationName))
                return null;

            if (startLocationName.Equals(destinationLocationName, StringComparison.Ordinal))
                return new[] { startLocationName };

            foreach (string[] route in FindRoutesFrom(startLocationName))
            {
                if (route[route.Length - 1].Equals(destinationLocationName, StringComparison.Ordinal))
                    return route;
            }

            return null;
        }

        /// <summary>
        /// Combine A -> B and B -> C into A -> B -> C.
        /// </summary>
        private static string[] CombineRoutes(string[] firstRoute, string[] secondRoute)
        {
            if (firstRoute == null || secondRoute == null)
                return null;

            if (firstRoute.Length == 0 || secondRoute.Length == 0)
                return null;

            if (!firstRoute[firstRoute.Length - 1].Equals(secondRoute[0], StringComparison.Ordinal))
                return null;

            List<string> combined = new(firstRoute);

            for (int i = 1; i < secondRoute.Length; i++)
            {
                string locationName = secondRoute[i];

                // Avoid loops. If a combined route would revisit an older location, it is not useful.
                if (combined.Contains(locationName))
                    return null;

                combined.Add(locationName);
            }

            return combined.Count > MaxRouteLength ? null : combined.ToArray();
        }


        /*********
        ** Known-location methods
        *********/
        /// <summary>
        /// Get every outdoor location the local player has discovered through Wizardry.
        /// </summary>
        private static HashSet<string> GetKnownOutdoorLocationNames(Farmer player)
        {
            HashSet<string> known = new(StringComparer.Ordinal);

            if (player == null)
                return known;

            foreach (string key in player.modData.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!key.StartsWith(TeleportKeyPrefix, StringComparison.Ordinal))
                    continue;

                string locationName = key.Substring(TeleportKeyPrefix.Length);
                if (string.IsNullOrWhiteSpace(locationName))
                    continue;

                locationName = ResolveWarpTargetName(locationName);

                GameLocation location = Game1.getLocationFromName(locationName);
                if (!CanUseOutdoorLocation(location))
                    continue;

                known.Add(location.Name);
            }

            Log.Mute(() => $"[PlayerRoutePathfinder] GetKnownOutdoorLocationNames found {known.Count} known outdoor locations.");
            return known;
        }

        /// <summary>
        /// Build a stable signature for the current known outdoor location set.
        /// </summary>
        private static string BuildKnownLocationSignature(HashSet<string> knownOutdoorLocations)
        {
            List<string> names = new(knownOutdoorLocations);
            names.Sort(StringComparer.Ordinal);

            return string.Join("|", names);
        }

        /// <summary>
        /// Check whether the existing cache belongs to this save and local player.
        /// </summary>
        private static bool IsCacheOwner(Farmer player)
        {
            return player != null
                && CachedSaveId == Game1.uniqueIDForThisGame
                && CachedPlayerId == player.UniqueMultiplayerID;
        }


        /*********
        ** Utility methods
        *********/
        /// <summary>
        /// Check whether this location is an outdoor location Wizardry should consider for travel routing.
        /// </summary>
        private static bool CanUseOutdoorLocation(GameLocation location)
        {
            if (location == null)
                return false;

            if (string.IsNullOrWhiteSpace(location.Name))
                return false;

            if (!location.IsOutdoors)
                return false;

            if (MineShaft.IsGeneratedLevel(location.Name) || VolcanoDungeon.IsGeneratedLevel(location.Name))
                return false;

            return true;
        }

        /// <summary>
        /// Apply outdoor-relevant vanilla-style warp target remaps.
        /// </summary>
        private static string ResolveWarpTargetName(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return targetName;

            if (targetName == "BoatTunnel")
                return "IslandSouth";

            foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
            {
                if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data)
                    && data.MapReplacements != null
                    && data.MapReplacements.TryGetValue(targetName, out string replacement))
                {
                    return replacement;
                }
            }

            return targetName;
        }

        /// <summary>
        /// Append a location to a route.
        /// </summary>
        private static string[] AppendRouteLocation(string[] route, string locationName)
        {
            string[] result = new string[route.Length + 1];

            for (int i = 0; i < route.Length; i++)
                result[i] = route[i];

            result[result.Length - 1] = locationName;
            return result;
        }

        /// <summary>
        /// Get sorted location names for deterministic route selection.
        /// </summary>
        private static IEnumerable<string> GetSortedLocationNames(HashSet<string> locationNames)
        {
            List<string> sorted = new(locationNames);
            sorted.Sort(StringComparer.Ordinal);

            return sorted;
        }

        /// <summary>
        /// Count all graph edges.
        /// </summary>
        private static int CountGraphEdges()
        {
            int count = 0;

            foreach (HashSet<string> targets in OutdoorWarpGraph.Values)
                count += targets.Count;

            return count;
        }

        /// <summary>
        /// Count all cached routes.
        /// </summary>
        private static int CountRoutes()
        {
            int count = 0;

            foreach (Dictionary<string, PlayerLocationRoute> routesByDestination in Routes.Values)
                count += routesByDestination.Count;

            return count;
        }

        /// <summary>
        /// Count cached routes from one start location.
        /// </summary>
        private static int CountRoutesForStart(string startLocationName)
        {
            if (string.IsNullOrWhiteSpace(startLocationName))
                return 0;

            if (!Routes.TryGetValue(startLocationName, out Dictionary<string, PlayerLocationRoute> routesByDestination))
                return 0;

            return routesByDestination.Count;
        }
    }
}
