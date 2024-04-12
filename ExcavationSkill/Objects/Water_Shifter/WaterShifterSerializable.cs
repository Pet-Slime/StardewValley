using Microsoft.Xna.Framework;

namespace ArchaeologySkill.Objects.Water_Shifter
{
    public class WaterShifterSerializable
    {
        public long Owner { get; set; } = 0L;

        public string Bait { get; set; } = "";

        public int BaitQuality { get; set; } = 0;

        public string ObjectName { get; set; } = "";

        public string ObjectId { get; set; } = "";

        public int ObjectStack { get; set; } = -1;

        public int ObjectQuality { get; set; } = 0;

        public bool IsJAObject { get; set; } = false;

        public bool IsDGAObject { get; set; } = false;

        public Vector2 Tile { get; set; }

        public WaterShifterSerializable() { }

        public WaterShifterSerializable(WaterShifter f)
        {
            Owner = f.Owner.Value;
            if (f.ShifterBait.Value is not null)
            {
                Bait = f.ShifterBait.Value.QualifiedItemId;
                BaitQuality = f.ShifterBait.Value.Quality;
            }
            if (f.heldObject.Value is not null)
            {
                ObjectName = f.heldObject.Value.Name;
                ObjectId = f.heldObject.Value.QualifiedItemId;
                ObjectStack = f.heldObject.Value.Stack;
                ObjectQuality = f.heldObject.Value.Quality;
                if (ModEntry.JsonAssetsLoaded)
                    IsJAObject = !string.IsNullOrWhiteSpace(ModEntry.JAAPI.GetObjectId(ObjectName));
                if (ModEntry.DynamicGameAssetsLoaded)
                    IsDGAObject = ModEntry.DGAAPI.GetDGAItemId(f.heldObject.Value) is not null;
                if (IsDGAObject)
                    ObjectName = ModEntry.DGAAPI.GetDGAItemId(f.heldObject.Value);
            }
            Tile = f.TileLocation;
        }
    }
}
