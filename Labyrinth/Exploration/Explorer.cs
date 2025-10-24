using Labyrinth.Crawl;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration
{
    /// <summary>
    /// Controls the exploration of the labyrinth using a crawler.
    /// </summary>
    /// <remarks>
    /// The explorer performs random moves through the labyrinth until reaching
    /// an <see cref="Outside"/> tile or performing the given maximum number of steps.
    /// The random behavior can be mocked for deterministic testing.
    /// </remarks>
    public class Explorer
    {
        private readonly ICrawler _crawler;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="Explorer"/> class with the specified crawler.
        /// </summary>
        /// <param name="crawler">The crawler used to move through the labyrinth.</param>
        /// <param name="random">Optional random number generator (used for testing).</param>
        public Explorer(ICrawler crawler, Random? random = null)
        {
            _crawler = crawler ?? throw new ArgumentNullException(nameof(crawler));
            _random = random ?? new Random();
        }

        /// <summary>
        /// Randomly walks through the labyrinth up to <paramref name="n"/> steps
        /// or stops when an <see cref="Outside"/> tile is reached.
        /// </summary>
        /// <param name="n">The maximum number of moves to perform.</param>
        public void GetOut(int n)
        {
            if (n <= 0) return;

            for (var i = 0; i < n; i++)
            {
                if (_crawler.FacingTile is Outside)
                    return;

                var action = _random.Next(3);

                switch (action)
                {
                    case 0:
                        _crawler.Walk();
                        break;
                    case 1:
                        _crawler.Direction.TurnLeft();
                        break;
                    case 2:
                        _crawler.Direction.TurnRight();
                        break;
                }
            }
        }
    }
}
