using RimWorld.Planet;
using Verse;

namespace SaveOurShip2
{
    public class WorldDrawLayer_DevSelectedTile : WorldDrawLayer_SelectedTile
    {
        protected override PlanetTile Tile
        {
            get
            {
                if (!Prefs.DevMode)
                {
                    return PlanetTile.Invalid;
                }

                return base.Tile;
            }
        }
    }
}