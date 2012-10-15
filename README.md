BF3 Zombie Mode
===============

##STILL IN DEVELOPMENT, DO NOT USE YET

<h2>Description</h2>

<p>BF3 Zombie Mode is a ProCon 1.0 plugin that turns Team Deathmatch into the _Infected_ or _Zombie_ variant play.</p>

<p>When there are a minimum number of players spawned, all of the players are moved to the human team (US), except for one zombie (RU). With default settings, Zombies can use knife/defib/repair tool <i>only</i> for weapons and Humans can use any weapon <i>except</i> explosives (grenades, C4, Claymores) or missiles; the allowed/forbidden weapon settings are configurable. Zombies are hard to kill. Every time a zombie kills a human, the human becomes infected and is moved to the zombie team. Humans win by killing a minimum number of zombies (configurable) or when all the zombies leave the server. Zombies win by infecting all the humans or when all the humans leave the server.</p>

<p>The maximum number of players is half your server slots, so if you have a 64-slot server, you can have a max of 32 players.</p>

Recommended server settings are here: <a href="http://www.phogue.net/forumvb/forum.php">TBD</a>

<h2>Settings</h2>
<p>There are a large number of configurable setttings, divided into sections.</p>

<h3>Admin Settings</h3>
<p><b>Zombie Mode Enabled</b>: <i>On/Off</i>, default is <i>On</i>.</p>

<p><b>Command Prefix</b>: Chat text that represents a command, default is <i>!zombie</i>. May be set to a single character, for example <i>@</i>, so that instead of a command being <i>!zombie rules</i>, the command would just be <i>@rules</i>.</p>

<p><b>Announce Display Length</b>: Time in seconds that announcements are shown as yells, default is <i>10</i>.</p>

<p><b>Warning Display Length</b>: Time in seconds that warnings are shown as yells, default is <i>15</i>.</p>

<p><b>Human Max Idle Seconds</b>: Time in seconds that a human is allowed to be idle (no spawns and no kills/deaths) before being kicked. This idle time applies only when a match is in progress. Since zombies can't win unless they can kill humans, the match can stall if a human remains idle and never spawns. The idle time for humans should therefore be relatively short. The default value is <i>120</i> seconds, or 2 minutes.</p>

<p><b>Max Idle Seconds</b>: Time in seconds that any player is allowed to be idle (no spawns and no kills/deaths) before being kicked. This idle time applies as long as Zombie Mode is enabled (On). The default value is <i>600</i> seconds, or 10 minutes.</p>

<p><b>Warns Before Kick For Rules Violations</b>: Number of warnings given before a player is kicked for violating the Zombie Mode rules, particularly for using a forbidden weapon type. The default value is <i>1</i>.</p>

<p><b>Debug Level</b>: A number that represents the amount of debug logging  that is sent to the plugin.log file in PRoCon. The higher the number, the more spam is logged. The default value is <i>2</i>. Note: if you have a problem using the plugin, set your <b>Debug Level</b> to <i>5</i> and save the plugin.log for posting to phogue.net.</p>

<p><b>Admin Users</b>: A table of soldier names that will permitted to use in-game admin commands (see below). The default value is <i>PapaCharlieNiner</i>.</p>

<h2>Commands</h2>
<p>TBD</p>

<h2>Development</h2>
<p>TBD</p>

<h2>Download</h2>

<p>Do links work? <a href=https://github.com/m4xxd3v/BF3ZombieMode/downloads>Download from this GitHub page!</a></p>

<h3>Changelog</h3>


