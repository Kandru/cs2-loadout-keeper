using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.InteropServices;

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

            CCSWeaponBaseVData vdata = weapon.GetVData<CCSWeaponBaseVData>()!;
            return Utilities.ReadStringUtf8(Marshal.ReadIntPtr(Schema.GetSchemaValue<nint>(vdata.Handle, "CCSWeaponBaseVData", "m_szAnimClass"), 0x10) + 0x10);
        }
    }
}