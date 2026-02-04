using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(int x, int y, Tile[,] tiles, SemaphoreSlim semaphore) : ICrawler
        {
            public int X => _x;

            public int Y => _y;

            public Task<Type> FacingTileType => ProcessFacingTileAsync((x, y, tile) => tile.GetType());

            Direction ICrawler.Direction => _direction;

            public Task<Inventory?> TryWalk(Inventory walkerInventory) => 
                ProcessFacingTileAsync((facingX, facingY, tile) =>
                {
                    Inventory? tileContent = null;

                    if (tile is Door door)
                    {
                        Open(door, walkerInventory);
                    }
                    if (tile.IsTraversable)
                    {
                        tileContent = tile.Pass();
                        _x = facingX;
                        _y = facingY;
                    }
                    return tileContent;
                });
            
            private bool Open(Door door, Inventory walkerInventory)
            {
                if (walkerInventory is not LocalInventory keyRing)
                {
                    throw new NotSupportedException("Local inventories only");
                }
                for(var maxKeys = walkerInventory.ItemTypes.Count(); maxKeys > 0; maxKeys--)
                {
                    if (door.Open(keyRing))
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool IsOut(int pos, int dimension) =>
                pos < 0 || pos >= _tiles.GetLength(dimension);

            private async Task<T> ProcessFacingTileAsync<T>(Func<int, int, Tile, T> process)
            {
                await _semaphore.WaitAsync();
                try
                {
                    int facingX = _x + _direction.DeltaX,
                        facingY = _y + _direction.DeltaY;

                    return process(
                         facingX, facingY,
                        IsOut(facingX, dimension: 0) ||
                        IsOut(facingY, dimension: 1)
                            ? Outside.Singleton
                            : _tiles[facingX, facingY]
                     );
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            private int _x = x;
            private int _y = y;

            private readonly Direction _direction = Direction.North;
            private readonly Tile[,] _tiles = tiles;
            private readonly SemaphoreSlim _semaphore = semaphore;
        }
    }
}