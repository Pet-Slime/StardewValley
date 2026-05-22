using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using WizardrySkill.Core;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class HealAreaEffect : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private const int PulseIntervalTicks = 120;
        private const int DurationTicks = 120 * 60;

        private static bool Initialized;

        private readonly Farmer Summoner;
        private readonly int Level;
        private readonly Vector2 CenterTile;
        private readonly GameLocation Location;

        private int TimeLeft = DurationTicks;
        private int PulseIndex;


        /*********
        ** Public methods
        *********/
        public HealAreaEffect(Farmer summoner, int level)
        {
            this.Summoner = summoner;
            this.Level = level + 1;
            this.CenterTile = summoner.Tile;
            this.Location = summoner.currentLocation;
        }

        /// <summary>Register heal pulse packet handling.</summary>
        public static void Init()
        {
            if (Initialized)
                return;

            NetworkEvents.HealPulseReceived += OnHealPulseReceived;
            Initialized = true;
        }

        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Summoner == null || this.Location == null)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }

            // The aura is anchored to the location where it was cast.
            if (this.Summoner.currentLocation != this.Location)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }

            if (this.TimeLeft % PulseIntervalTicks == 0)
                this.Pulse();

            if (--this.TimeLeft <= 0)
            {
                this.CleanUp();
                return false;
            }

            return true;
        }

        public void Draw(SpriteBatch b)
        {
            // Nothing; visuals are broadcast through TemporaryAnimatedSprite.
        }

        public void CleanUp()
        {
            // No persistent sprite/light resources to remove.
        }


        /*********
        ** Private helpers
        *********/
        private void Pulse()
        {
            List<Vector2> affectedTiles = MakeAffectedTiles(this.CenterTile, 2 * this.Level);

            // The caster-owned effect broadcasts the aura visuals through Stardew's native sprite sync.
            this.BroadcastPulseVisuals(affectedTiles);

            // The caster's own machine heals its own local player immediately if applicable.
            ApplyLocalHealIfInsidePulse(
                locationName: this.Location.Name,
                centerTile: this.CenterTile,
                level: this.Level,
                healAmount: 1,
                playSound: this.TimeLeft % 300 == 0);

            // Other machines receive this packet and check only their own local player.
            NetworkEvents.BroadcastHealPulse(new HealPulsePacket
            {
                PulseId = $"{this.Summoner.UniqueMultiplayerID}:{Game1.ticks}:{this.PulseIndex++}",
                CasterId = this.Summoner.UniqueMultiplayerID,
                SpellId = "life:healarea",
                Level = this.Level,
                LocationName = this.Location.Name,
                CenterX = (int)(this.CenterTile.X * Game1.tileSize),
                CenterY = (int)(this.CenterTile.Y * Game1.tileSize),
                Radius = (1.5f + 2 * this.Level) * Game1.tileSize,
                HealAmount = 1
            });
        }

        private void BroadcastPulseVisuals(List<Vector2> affectedTiles)
        {
            foreach (Vector2 tile in affectedTiles)
            {
                if (this.Location.IsTileBlockedBy(new Vector2(tile.X, tile.Y), ignorePassables: CollisionMask.Farmers))
                    continue;

                Vector2 point = tile * Game1.tileSize;
                Game1.Multiplayer.broadcastSprites(this.Location,
                    new TemporaryAnimatedSprite(
                        10,
                        point,
                        Color.Red,
                        10,
                        Game1.random.NextDouble() < 0.5,
                        70f,
                        0,
                        Game1.tileSize,
                        (tile.Y + 1) * Game1.tileSize / 10000f));
            }
        }

        private static void OnHealPulseReceived(HealPulsePacket packet)
        {
            if (packet == null || Game1.player == null || Game1.player.currentLocation == null)
                return;

            Vector2 centerTile = new(packet.CenterX / Game1.tileSize, packet.CenterY / Game1.tileSize);

            ApplyLocalHealIfInsidePulse(
                locationName: packet.LocationName,
                centerTile: centerTile,
                level: packet.Level,
                healAmount: packet.HealAmount,
                playSound: true);
        }

        private static void ApplyLocalHealIfInsidePulse(string locationName, Vector2 centerTile, int level, int healAmount, bool playSound)
        {
            Farmer player = Game1.player;
            if (player == null || player.currentLocation == null)
                return;

            if (player.currentLocation.Name != locationName)
                return;

            List<Vector2> affectedTiles = MakeAffectedTiles(centerTile, 2 * level);
            if (!affectedTiles.Contains(player.Tile))
                return;

            player.health = Math.Min(player.health + healAmount, player.maxHealth);
            player.currentLocation.debris.Add(new Debris(1, new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), Color.Green, 1f, player));

            if (playSound)
                player.currentLocation.LocalSoundAtPixel("healSound", player.Position);
        }

        private static List<Vector2> MakeAffectedTiles(Vector2 tileLocation, int level, bool hollow = false)
        {
            List<Vector2> list = new();
            int centerX = (int)tileLocation.X;
            int centerY = (int)tileLocation.Y;

            float radius = 1.5f + level;
            float radiusSq = radius * radius;
            float innerRadiusSq = (radius - 0.75f) * (radius - 0.75f);

            for (int x = centerX - (int)radius; x <= centerX + (int)radius; x++)
            {
                for (int y = centerY - (int)radius; y <= centerY + (int)radius; y++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq)
                    {
                        if (!hollow || distSq >= innerRadiusSq)
                            list.Add(new Vector2(x, y));
                    }
                }
            }

            return list;
        }
    }
}
