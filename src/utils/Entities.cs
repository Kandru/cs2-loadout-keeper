using CounterStrikeSharp.API.Core;

namespace LoadoutKeeper.Utils
{
    public static class Entities
    {
        public static string? PlayerWeaponName(CBasePlayerWeapon weapon)
        {
            if (!weapon.IsValid)
            {
                return null;
            }
            try
            {
                CCSWeaponBaseVData? vdata = weapon.GetVData<CCSWeaponBaseVData>();
                return vdata?.Name ?? null;
            }
            catch
            {
                return null;
            }
        }
    }
}