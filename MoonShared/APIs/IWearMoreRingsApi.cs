using StardewValley.Objects;

namespace MoonShared.APIs;

public interface IWearMoreRingsAPI_2
{
    int RingSlotCount();
    Ring GetRing(int slot);
}
