using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace LlamaDialogue
{
    public class Util
    {
        public static IEnumerable<NPC> GetNearbyNpcs(NPC npc)
        {
            // Check for any other NPCs within 3 squares
            var speakerLocation = npc.Tile;
            var speakerName = npc.Name;
            var npcs = Game1.currentLocation.characters.Where(x => x.CanReceiveGifts() && x.Name != speakerName);
            List<NPC> nearbyNpcs = new List<NPC>();
            foreach (var otherNpc in npcs)
            {
                var npcLocation = otherNpc.Tile;
                if (Microsoft.Xna.Framework.Vector2.Distance(speakerLocation, npcLocation) < 4.5)
                {
                    nearbyNpcs.Add(otherNpc);
                }
            }
            return nearbyNpcs;
        }

    }
}