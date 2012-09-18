/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

enum EDisplayType { yell, say };

namespace PRoConEvents
{
	
	public class CZombieMode : PRoConPluginAPI, IPRoConPluginInterface
	{
		#region Constants

		const string HUMAN_TEAM = "1";

		const string ZOMBIE_TEAM = "2";

		const string BLANK_SQUAD = "0";

		const string FORCE_MOVE = "true";

		#endregion

		#region PluginSettings

		private string CommandPrefix = "!";

		private int AnnounceDisplayLength = 3;

		private bool MakeTeamsRequested = false;

		private EDisplayType AnnounceDisplayType = EDisplayType.yell;

		private int WarningDisplayLength = 10;

		private List<String> AdminUsers = new List<String>();

		private List<String> PlayerKickQueue = new List<String>();

		#endregion


		#region GamePlayVars

		private List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();

		private bool ZombieModeEnabled = false;

		private int MaxPlayers = 12;

		private int MinimumHumans = 1;

		private int MinimumZombies = 1;

		private int ZombiesKilledToSurvive = 50;

		private bool ZombieKillLimitEnabled = true;

		private int HumanBulletDamage = 0;

		private int ZombieBulletDamage = 0;

		private bool InfectSuicides = true;

		#endregion



		public int ZombiesKilled = 0;

		public int HumansKilled = 0;

		private string[] ZombieWeapons = 
	    {
	        "Melee",
	        "Defib",
	        "Knife_RazorBlade",
	        "Knife",
	        "Repair Tool"
	    };

		#region WeaponList
		private List<String> WeaponList = new List<String>(new string[] {
            "870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            "Weapons/Gadgets/C4/C4",
            "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR"
        });
		#endregion

		#region ZombieWeaponList
		private List<String> ZombieWeaponsEnabled = new List<String>(new string[] {
			"Repair Tool",
			"Defib",
			"Melee",
			"Knife_RazorBlade"
		});
		#endregion

		#region HumanWeaponList
		private List<String> HumanWeaponsEnabled = new List<String>(new string[] {
			"870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            "Weapons/Gadgets/C4/C4",
            "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR"
		});
		#endregion

		#region EventHandlers

		/** EVENT HANDLERS **/
		public override void OnPlayerKickedByAdmin(string SoldierName, string reason) 
		{
			if (this.ZombieModeEnabled == false)
				return;

			for(int i = 0; i < this.PlayerKickQueue.Count;i++)
			{
				CPlayerInfo Player = this.PlayerList[i];
				if (Player.SoldierName.Equals(SoldierName))
				{
					this.PlayerKickQueue.RemoveAt(i);
				}
			}
		}

		public override void OnPlayerAuthenticated(string SoldierName, string guid)
		{
			if (this.ZombieModeEnabled == false)
				return;

			if (this.PlayerList.Count <= this.MaxPlayers)
				return;

			base.OnPlayerAuthenticated(SoldierName, guid);
			
			this.PlayerKickQueue.Add(SoldierName);

			ThreadStart kickPlayer = delegate
			{
				try
				{
					Thread.Sleep(10000);
					this.ExecuteCommand("procon.protected.tasks.add", "CZombieMode", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Sorry, zombie mode is enabled and all slots are full :( Please join when there are less than ", this.MaxPlayers.ToString(), " players"));
					while (true)
					{
						if (!this.PlayerKickQueue.Contains(SoldierName))
							break;

						this.ExecuteCommand("procon.protected.tasks.add", "CZombieMode", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Sorry, zombie mode is enabled and all slots are full :( Please join when there are less than ", this.MaxPlayers.ToString(), " players"));
						Thread.Sleep(500);
					}
				}
				catch (System.Exception e)
				{

				}
			};

			Thread t = new Thread(kickPlayer);

			t.Start();
		}
		public override void OnPlayerJoin(string SoldierName)
		{
			if (this.ZombieModeEnabled)
				this.MakeHuman(SoldierName);
		}
		
		public override void OnPlayerKilled(Kill info)
		{
			if (this.ZombieModeEnabled == false)
				return;

			if (info.DamageType == "Death") return;

			if (info.Killer.SoldierName == info.Victim.SoldierName)
			{
				if (this.InfectSuicides)
				{
					this.Infect("Suicide ", info.Victim.SoldierName);
					return;
				}
			}

			if (info.Killer.SoldierName == "")
			{
				if (this.InfectSuicides)
				{
					this.Infect("Misfortune ", info.Victim.SoldierName);
					return;
				}
			}

			if (info.Killer.TeamID.ToString() == HUMAN_TEAM)
			{
				this.ZombiesKilled += 1;

				this.ConsoleLog(String.Concat("Human ", info.Killer.SoldierName, " just killed zombie ", info.Victim.SoldierName));

				return;
			}


			this.ConsoleLog(String.Concat("damage type: ", info.DamageType));

			if (this.ValidateWeapon(info.DamageType,info.Killer.TeamID.ToString()) == false)
			{
				this.ConsoleLog(String.Concat(info.Killer.SoldierName, " invalid kill with ", info.DamageType, "!"));

				this.KillPlayer(info.Killer.SoldierName, "Bad weapon choice!");

				return;
			}

			this.HumansKilled += 1;

			this.Infect(info.Killer.SoldierName, info.Victim.SoldierName);

			this.ConsoleLog(String.Concat(info.Killer.SoldierName, " valid zombie kill with ", info.DamageType));

		}

		public override void OnListPlayers(List<CPlayerInfo> Players, CPlayerSubset Subset)
		{
			this.PlayerList = Players;
		}

		public override void OnSquadChat(string PlayerName, String Message, int TeamId, int SquadId)
		{
			if (!this.IsAdmin(PlayerName))
				return;

			List<string> MessagePieces = new List<string>(Message.Split(' '));

			String Command = MessagePieces[0];

			if (!Command.StartsWith(this.CommandPrefix))
				return;

			switch (Command.TrimStart(this.CommandPrefix.ToCharArray()))
			{
				case "infect":
					if (MessagePieces.Count != 2) return;
					this.Infect("Admin", MessagePieces[1]);
					break;
				case "heal":
					if (MessagePieces.Count != 2) return;
					this.MakeHuman(MessagePieces[1]);
					break;
				case "teams":
					this.MakeTeamsRequest();
					break;
				case "restart":
					this.RestartRound();
					break;
				case "next":
					this.NextRound();
					break;
				case "zombie":
					if (MessagePieces.Count < 2) return;
					if (MessagePieces[1] == "on")
						this.ZombieModeEnabled = true;
					else if (MessagePieces[1] == "off")
						this.ZombieModeEnabled = false;
					break;
				case "rules":
					break;
				case "warn":
					if (MessagePieces.Count < 3) return;
					string WarningMessage = String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray());

					this.ConsoleLog(WarningMessage);
					this.Warn(MessagePieces[1], WarningMessage);
					break;

				case "kill":
					if (MessagePieces.Count < 2) return;
					string KillMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";

					this.ConsoleLog(KillMessage);
					this.KillPlayer(MessagePieces[1], KillMessage);
					break;

				case "kick":
					if (MessagePieces.Count < 2) return;
					string KickMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";

					this.KickPlayer(MessagePieces[1], KickMessage);
					break;
				case "test":
					this.ConsoleLog("loopz");
					this.ConsoleLog(this.FrostbitePlayerInfoList.Values.Count.ToString());
					foreach (CPlayerInfo Player in this.FrostbitePlayerInfoList.Values)
					{
						this.ConsoleLog("looping");
						String testmessage = Player.SoldierName;
						this.ConsoleLog(testmessage);
					}
					break;
			}

		}

		#endregion


		#region PluginMethods
		/** PLUGIN RELATED SHIT **/
		// Compile and init events
		#region PluginEventHandlers
		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			this.RegisterEvents(this.GetType().Name, "OnPlayerKilled", "OnListPlayers", "OnSquadChat", "OnPlayerAuthenticated", "OnPlayerKickedByAdmin");
		}

		public void OnPluginEnable()
		{
			//System.Diagnostics.Debugger.Break();
			this.ConsoleLog(String.Concat("^b", this.GetPluginName(), " ^2Enabled... It's Game Time!"));
		}

		public void OnPluginDisable()
		{
			this.ConsoleLog(String.Concat("^b", this.GetPluginName(), " ^2Disabled :("));
			this.Reset();
		}
		#endregion

		// Plugin details
		public string GetPluginName()
		{
			return "Zombie Mode";
		}

		public string GetPluginVersion()
		{
			return "0.1.0";
		}

		public string GetPluginAuthor()
		{
			return "m4xxd3v";
		}

		public string GetPluginWebsite()
		{
			return "http://google.com";
		}

		public string GetPluginDescription()
		{
			return "This plugin enables a zombie infection mode type game play";
		}


		// Plugin variables
		public List<CPluginVariable> GetDisplayPluginVariables()
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Mode Enabled", typeof(enumBoolYesNo), this.ZombieModeEnabled ? enumBoolYesNo.Yes : enumBoolYesNo.No));

			lstReturn.Add(new CPluginVariable("Admin Settings|Command Prefix", this.CommandPrefix.GetType(), this.CommandPrefix));

			lstReturn.Add(new CPluginVariable("Admin Settings|Announce Display Length", this.AnnounceDisplayLength.GetType(), this.AnnounceDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Warning Display Length", this.WarningDisplayLength.GetType(), this.WarningDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Admin Users", typeof(string[]), this.AdminUsers.ToArray()));

			lstReturn.Add(new CPluginVariable("Game Settings|Max Players", this.MaxPlayers.GetType(), this.MaxPlayers));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Zombies", this.MinimumZombies.GetType(), this.MinimumZombies));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Humans", this.MinimumHumans.GetType(), this.MinimumHumans));

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Kill Limit Enabled", typeof(enumBoolOnOff), this.ZombieKillLimitEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

			if (this.ZombieKillLimitEnabled)
				lstReturn.Add(new CPluginVariable("Game Settings|Zombies Killed To Survive", this.ZombiesKilledToSurvive.GetType(), this.ZombiesKilledToSurvive));

			lstReturn.Add(new CPluginVariable("Game Settings|Infect Suicide Players", typeof(enumBoolOnOff), this.InfectSuicides ? enumBoolOnOff.On : enumBoolOnOff.Off));


			foreach (PRoCon.Core.Players.Items.Weapon Weapon in this.WeaponDictionaryByLocalizedName.Values)
			{
				String WeaponDamage = Weapon.Damage.ToString();

				if (WeaponDamage.Equals("Nonlethal") || WeaponDamage.Equals("None") || WeaponDamage.Equals("Suicide"))
					continue;

				String WeaponName = Weapon.Name.ToString();
				lstReturn.Add(new CPluginVariable(String.Concat("Zombie Weapons|Z -", WeaponName), typeof(enumBoolOnOff), this.ZombieWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
				lstReturn.Add(new CPluginVariable(String.Concat("Human Weapons|H -", WeaponName), typeof(enumBoolOnOff), this.HumanWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
			}


			return lstReturn;
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			List<CPluginVariable> lstReturn = this.GetDisplayPluginVariables();



			return lstReturn;
		}

		public void SetPluginVariable(string Name, string Value)
		{
			ThreadStart MyThread = delegate
			{
				try
				{
					int PipeIndex = Name.IndexOf('|');
					if (PipeIndex >= 0)
					{
						PipeIndex++;
						Name = Name.Substring(PipeIndex, Name.Length - PipeIndex);
					}

					BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

					String PropertyName = Name.Replace(" ", "");

					FieldInfo Field = this.GetType().GetField(PropertyName, Flags);

					Dictionary<int, Type> EasyTypeDict = new Dictionary<int, Type>();
					EasyTypeDict.Add(0, typeof(int));
					EasyTypeDict.Add(1, typeof(Int16));
					EasyTypeDict.Add(2, typeof(Int32));
					EasyTypeDict.Add(3, typeof(Int64));
					EasyTypeDict.Add(4, typeof(float));
					EasyTypeDict.Add(5, typeof(long));
					EasyTypeDict.Add(6, typeof(String));
					EasyTypeDict.Add(7, typeof(string));

					Dictionary<int, Type> BoolDict = new Dictionary<int, Type>();
					BoolDict.Add(0, typeof(Boolean));
					BoolDict.Add(1, typeof(bool));

					Dictionary<int, Type> ListStrDict = new Dictionary<int, Type>();
					ListStrDict.Add(0, typeof(List<String>));
					ListStrDict.Add(1, typeof(List<string>));
					
					

					if (Field != null)
					{
						
						Type FieldType = Field.GetValue(this).GetType();
						if (EasyTypeDict.ContainsValue(FieldType))
							Field.SetValue(this, TypeDescriptor.GetConverter(FieldType).ConvertFromString(Value));
						else if (ListStrDict.ContainsValue(FieldType))
							Field.SetValue(this, new List<string>(CPluginVariable.DecodeStringArray(Value)));
						else if (BoolDict.ContainsValue(FieldType))
							if (Value == "Yes" || Value == "On")
								Field.SetValue(this, true);
							else
								Field.SetValue(this, false);
					}
					else
					{
						String WeaponName = Name.Substring(3, Name.Length - 3);

						if (this.WeaponList.IndexOf(WeaponName) >= 0)
						{
							String WeaponType = Name.Substring(0, 3);

							if (WeaponType == "H -")
							{
								if (Value == "On")
									this.EnableHumanWeapon(WeaponName);
								else
									this.DisableHumanWeapon(WeaponName);
							}
							else
							{
								if (Value == "On")
									this.EnableZombieWeapon(WeaponName);
								else
									this.DisableZombieWeapon(WeaponName);
							}

						}
					}
				}
				catch (System.Exception e)
				{

				}
			};

			Thread t = new Thread(MyThread);

			t.Start();

			
		}
		#endregion



		/** PRIVATE METHODS **/

		#region RoundCommands

		private void RestartRound()
		{
			this.ExecuteCommand("procon.protected.send", "mapList.restartRound");
		}

		private void NextRound()
		{
			this.ExecuteCommand("procon.protected.send", "mapList.runNextRound");
		}

		#endregion


		#region PlayerPunishmentCommands

		private void Warn(String PlayerName, String Message)
		{
			this.ExecuteCommand("procon.protected.send", "admin.yell", Message, this.WarningDisplayLength.ToString(), "all", PlayerName);
		}

		private void KillPlayer(string PlayerName, string Reason)
		{
			this.ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);

			if (Reason.Length > 0)
				this.Announce(String.Concat(PlayerName, ": ", Reason));
		}

		private void KickPlayerDelayed(string PlayerName, string Reason, int SecsToDelay)
		{
			this.ExecuteCommand("procon.protected.tasks.add", "ZombieKickUser", SecsToDelay.ToString(), "1", "1", "admin.kickPlayer", PlayerName, Reason);
		}

		private void KickPlayer(string PlayerName, string Reason)
		{
			this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", PlayerName, Reason);

			if (Reason.Length > 0)
				this.Announce(String.Concat(PlayerName, "kicked for: ", Reason));
		}

		#endregion



		private void Rules(String PlayerName)
		{

		}


		#region TeamMethods

		private void ShufflePlayersList()
		{
			List<CPlayerInfo> randomList = new List<CPlayerInfo>();

			Random r = new Random();
			int randomIndex = 0;
			while (this.PlayerList.Count > 0)
			{
				randomIndex = r.Next(0, this.PlayerList.Count); //Choose a random object in the list
				randomList.Add(this.PlayerList[randomIndex]); //add it to the new, random list
				this.PlayerList.RemoveAt(randomIndex); //remove to avoid duplicates
			}

			this.PlayerList = randomList; //return the new random list

			if (this.MakeTeamsRequested == true)
				this.MakeTeams();

		}

		private void RequestPlayersList()
		{
			this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
		}

		private void MakeTeamsRequest()
		{
			this.Announce("Teams are being generated!");

			this.ConsoleLog("Teams being generated");

			this.MakeTeamsRequested = true;

			this.RequestPlayersList();
		}

		private void MakeTeams()
		{
			this.MakeTeamsRequested = false;

			this.ShufflePlayersList();

			int ZombieCount = 0;
			foreach (CPlayerInfo Player in this.PlayerList)
			{
				if (ZombieCount < this.MinimumZombies)
				{
					ZombieCount++;
					this.ConsoleLog(String.Concat("Making ", Player, " a zombie"));
					this.MakeZombie(Player.SoldierName);

				}
				else
				{
					this.ConsoleLog(String.Concat("Making ", Player, " a human"));
					this.MakeHuman(Player.SoldierName);
				}
			}

			this.ConsoleLog("Team generation complete.");
		}

		public void Infect(string Carrier, string Victim)
		{
			this.Announce(String.Concat(Carrier, " just infected ", Victim));

			this.MakeZombie(Victim);
		}

		private void MakeHuman(string PlayerName)
		{
			this.Announce(String.Concat(PlayerName, " has join the fight for survival!"));

			this.ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, HUMAN_TEAM, BLANK_SQUAD, FORCE_MOVE);
		}

		private void MakeZombie(string PlayerName)
		{
			this.ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, ZOMBIE_TEAM, BLANK_SQUAD, FORCE_MOVE);
		}

		#endregion

		#region WeaponMethods

		private void DisableZombieWeapon(String WeaponName)
		{
			int Index = this.ZombieWeaponsEnabled.IndexOf(WeaponName);
			if (Index >= 0)
				this.ZombieWeaponsEnabled.RemoveAt(Index);
		}

		private void DisableHumanWeapon(String WeaponName)
		{
			int Index = this.HumanWeaponsEnabled.IndexOf(WeaponName);
			if (Index >= 0)
				this.HumanWeaponsEnabled.RemoveAt(Index);
		}

		private void EnableZombieWeapon(String WeaponName)
		{
			int Index = this.ZombieWeaponsEnabled.IndexOf(WeaponName);
			if (Index < 0)
				this.ZombieWeaponsEnabled.Add(WeaponName);
		}

		private void EnableHumanWeapon(String WeaponName)
		{
			int Index = this.HumanWeaponsEnabled.IndexOf(WeaponName);
			if (Index < 0)
				this.HumanWeaponsEnabled.Add(WeaponName);

		}

		private bool ValidateWeapon(string Weapon, string TEAM_CONST)
		{

			if (
				(TEAM_CONST == HUMAN_TEAM && this.HumanWeaponsEnabled.IndexOf(Weapon) >= 0) || 
				(TEAM_CONST == ZOMBIE_TEAM && this.ZombieWeaponsEnabled.IndexOf(Weapon) >= 0)
				)
				return true;
			
			return false;
		}

		#endregion

		private bool IsAdmin(string PlayerName)
		{
			return this.AdminUsers.IndexOf(PlayerName) >= 0 ? true : false;
		}

		private void ConsoleLog(string str)
		{
			this.ExecuteCommand("procon.protected.pluginconsole.write", str);
		}

		private void Announce(string Message)
		{
			this.ExecuteCommand("procon.protected.send", "admin.yell", Message, this.AnnounceDisplayLength.ToString(), this.AnnounceDisplayType.ToString());
		}

		private void Reset()
		{
			this.ZombiesKilled = 0;
			this.HumansKilled = 0;
			this.PlayerList = new List<CPlayerInfo>();
		}

	}

}


