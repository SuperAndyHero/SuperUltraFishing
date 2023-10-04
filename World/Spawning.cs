using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperUltraFishing.World
{
    public class Spawning
    {
        private EntitySystem entitySystem;
        private GameWorld gameworld;

        public Spawning(GameWorld gameworld)
        {
            this.gameworld = gameworld;
        }

        public void PostLoad(EntitySystem entitySystem)
        {
            this.entitySystem = entitySystem;
        }

        public void SpawnEntities()
        {

        }
    }
}
