using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace LoadoutKeeper.Utils
{
    public static class Players
    {
        public static void ResetBuyMenuLoadout(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value?.ActionTrackingServices == null)
            {
                return;
            }
            player.PlayerPawn.Value.ActionTrackingServices.WeaponPurchasesThisRound.WeaponPurchases.RemoveAll();
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_pActionTrackingServices");
        }

        public static void SetMoney(CCSPlayerController player, int amount)
        {
            if (player?.IsValid != true || player.PlayerPawn?.Value == null)
            {
                return;
            }

            player.InGameMoneyServices!.Account = amount;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        public static void ChangeMoney(CCSPlayerController player, int amount)
        {
            if (player?.IsValid != true || player.PlayerPawn?.Value == null)
            {
                return;
            }

            player.InGameMoneyServices!.Account += amount;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}