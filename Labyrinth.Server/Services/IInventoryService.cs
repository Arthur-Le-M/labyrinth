namespace Labyrinth.Server.Services;

using ApiTypes;

/// <summary>
/// Service interface for managing inventory operations following the Single Responsibility Principle.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Gets the items in the crawler's bag (inventory).
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <returns>The array of items in the bag, or null if the crawler doesn't exist.</returns>
    InventoryItem[]? GetBag(Guid crawlerId);
    
    /// <summary>
    /// Gets the items available in the room where the crawler is located.
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <returns>The array of items in the room, or null if the crawler doesn't exist.</returns>
    InventoryItem[]? GetRoomItems(Guid crawlerId);
    
    /// <summary>
    /// Moves items between the bag and room based on the move requirements.
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <param name="moveRequests">Array of items with their move requirements.</param>
    /// <returns>The updated bag contents after the move, or null if the crawler doesn't exist.</returns>
    InventoryItem[]? MoveItems(Guid crawlerId, InventoryItem[] moveRequests);
    
    /// <summary>
    /// Moves items from the room to the bag based on the move requirements.
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <param name="moveRequests">Array of items with their move requirements. Set MoveRequired to true to move an item.</param>
    /// <returns>The updated bag contents after the move, or null if the crawler doesn't exist.</returns>
    InventoryItem[]? MoveRoomItemsToBag(Guid crawlerId, InventoryItem[] moveRequests);
}
