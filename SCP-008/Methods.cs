using System.Collections.Generic;
using EXILED;
using EXILED.Extensions;
using MEC;
using UnityEngine;

namespace SCP008
{
	public class Methods
	{
		private readonly Plugin plugin;
		public Methods(Plugin plugin) => this.plugin = plugin;

		public void InfectPlayer(ReferenceHub player)
		{
			if (plugin.InfectedPlayers.Contains(player))
			{
				Log.Debug($"{player.nicknameSync.MyNick} already infected.");
				return;
			}

			if (player.characterClassManager.IsAnyScp())
			{
				Log.Debug($"{player.nicknameSync.MyNick} is an SCP.");
				return;
			}
			plugin.InfectedPlayers.Add(player);

			Log.Debug($"{player.nicknameSync.MyNick} infected.");
			plugin.Coroutines.Add(Timing.RunCoroutine(DoInfectionTimer(player), $"{player.characterClassManager.UserId}"));
		}

		private IEnumerator<float> DoInfectionTimer(ReferenceHub player)
		{
			Log.Debug($"Infection timer for {player.nicknameSync.MyNick} started.");
			for (int i = 0; i < plugin.InfectionLength; i++)
			{
				if (!plugin.InfectedPlayers.Contains(player))
				{
					Log.Debug($"{player.nicknameSync.MyNick} is no longer on infected list, halting timer.");
					yield break;
				}

				player.gameObject.GetComponent<Broadcast>().RpcClearElements();
				player.Broadcast(1, $"You are infected with SCP-008. The infection will take over in {plugin.InfectionLength - i} seconds!", false);
				yield return Timing.WaitForSeconds(1f);
			}

			GameObject gameObject = player.gameObject;
			Vector3 pos = gameObject.transform.position;

			player.inventory.ServerDropAll();
			Timing.RunCoroutine(TurnIntoZombie(player, pos));

			yield return Timing.WaitForSeconds(0.6f);
			
			foreach (ReferenceHub hub in Player.GetHubs())
				if (Vector3.Distance(hub.gameObject.transform.position, player.gameObject.transform.position) < 10f && hub.characterClassManager.IsHuman() && hub != player)
					InfectPlayer(hub);
			CurePlayer(player);
		}
		
		public IEnumerator<float> TurnIntoZombie(ReferenceHub player, Vector3 position)
		{
			if (player.characterClassManager.CurClass == RoleType.Scp0492)
			{
				yield break;
			}
			yield return Timing.WaitForSeconds(0.3f);
			player.characterClassManager.SetClassIDAdv(RoleType.Scp0492, false);
			yield return Timing.WaitForSeconds(2.5f);
			player.playerStats.health = player.playerStats.maxHP;
			player.plyMovementSync.OverridePosition(position, player.gameObject.transform.rotation.y);
			CurePlayer(player);
		}

		public void CurePlayer(ReferenceHub player)
		{
			if (plugin.InfectedPlayers.Contains(player))
				plugin.InfectedPlayers.Remove(player);

			Timing.KillCoroutines($"{player.characterClassManager.UserId}");
		}
	}
}