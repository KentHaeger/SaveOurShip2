using System.Collections;
using RimWorld.Planet;
using Verse;

namespace SaveOurShip2
{
    public class WorldDrawLayer_DevGrid : WorldDrawLayer_RaycastableGrid
    {
        private bool wasActivated;

        private static bool ShouldActivate => Prefs.DevMode;

        public override bool ShouldRegenerate
        {
            get
            {
                // We should always regenerate the mesh if activate state changes
                if (wasActivated != ShouldActivate)
                {
                    return true;
                }
                return base.ShouldRegenerate;
            }
        }

        public override IEnumerable Regenerate()
        {
            wasActivated = ShouldActivate;
            if (wasActivated)
            {
                material = WorldMaterials.MouseTile;
                foreach (object item in base.Regenerate())
                {
                    yield return item;
                }
            }
            else
            {
                // If deactivated, we should remove the grid
                base.Dispose();
                base.RegenerateWorldMeshColliders();
            }
        }
    }
}