using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Sys;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public class RandExplorer(ICrawler crawler, IEnumRandomizer<RandExplorer.Actions> rnd)
    {
        private readonly ICrawler _crawler = crawler;
        private readonly IEnumRandomizer<Actions> _rnd = rnd;
        
        public enum Actions
        {
            TurnLeft,
            Walk
        }

        public ICrawler Crawler => _crawler;

        public async Task<int> GetOut(int n, Inventory? bag = null, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");

            bag ??= new MyInventory();
            while (n > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var facingTileType = await _crawler.GetFacingTileTypeAsync(cancellationToken);
                if (facingTileType == typeof(Outside))
                {
                    break;
                }

                EventHandler<CrawlingEventArgs>? changeEvent;

                if (facingTileType != typeof(Wall)
                    && _rnd.Next() == Actions.Walk
                    && await _crawler.TryWalk(bag, cancellationToken) is Inventory roomContent)
                {
                    await bag.TryMoveItemsFrom(
                        roomContent,
                        roomContent.ItemTypes.Select(_ => true).ToList()
                    );
                    changeEvent = PositionChanged;
                }
                else
                {
                    _crawler.Direction.TurnLeft();
                    changeEvent = DirectionChanged;
                }
                changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
                n--;
            }
            return n;
        }


        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }

}
