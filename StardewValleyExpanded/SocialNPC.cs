using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewValleyExpanded
{
    // Implementing the NPC interface which is what lets us override or create new NPCs
    class SocialNPC : NPC
    {
        //Variable for the originalNpc
        public NPC OriginalNpc { get; }
        //Variable for if the npc can socialize or not.
        //We default it to true so that any npc we create with this class will be able to socialize.
        public override bool CanSocialize { get; } = true;

        //Empty constructor
        public SocialNPC() { }

        /* This method is what creates the overridden npc. 
           Using the base keyword sets what constructor should be used when creating 
           new instances of this class.
        */
        public SocialNPC(NPC npc, Vector2 tilePos) 
            : base(npc.Sprite, new Vector2(tilePos.X * Game1.tileSize, tilePos.Y * Game1.tileSize), npc.DefaultMap, npc.FacingDirection, npc.Name, npc.datable.Value, null, npc.Portrait)
        {
            this.OriginalNpc = npc;
        }

        //This method forces the npc data to be reloaded
        public void ForceReload()
        {
            bool newDay = Game1.newDay;
            try
            {
                Game1.newDay = true;
                this.reloadSprite();
            }
            finally
            {
                Game1.newDay = newDay;
            }

            this.checkSchedule(600);
        }
    }
}
