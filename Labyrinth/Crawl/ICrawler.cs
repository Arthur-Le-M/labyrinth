using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Crawl
{
    /// <summary>
    /// Labyrinth crawler interface.
    /// </summary>
    public interface ICrawler
    {
        /// <summary>
        /// Gets the current X position.
        /// </summary>
        int X { get; }

        /// <summary>
        /// Gets the current Y position.
        /// </summary>
        int Y { get; }

        /// <summary>
        /// Gets the current direction.
        /// </summary>
        Direction Direction { get; }

        /// <summary>
        /// Gets the tile in front of the crawler.
        /// </summary>
        Task<Type> FacingTileType { get; }

        /// <summary>
        /// Gets the tile in front of the crawler with cancellation support.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        Task<Type> GetFacingTileTypeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Pass the tile in front of the crawler and move into it.
        /// </summary>
        /// <param name="myInventory">The inventory of the crawler</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The inventory of the place reached, null if cannot walk in.</returns>
        Task<Inventory?> TryWalk(Inventory myInventory, CancellationToken cancellationToken = default);
    }
}
