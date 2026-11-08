using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class IsmailsWeaponReworks : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			Common.Net.WeaponNet.Handle(reader, whoAmI);
		}
	}
}
