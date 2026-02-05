using Labyrinth.Crawl;
using Labyrinth.Items;
using System.Diagnostics.CodeAnalysis;
using Dto = ApiTypes;
using System.Net.Http.Json;

namespace Labyrinth.ApiClient
{
    public class ClientCrawler : ICrawler
    {
        public ClientCrawler(Uri serverUrl, Guid appKey, Dto.Crawler createdCrawler, out Inventory bag)
        {
            _appKey = appKey;
            _httpClient = new HttpClient() { BaseAddress = serverUrl };
            bag = _bag   = new RemoteInventory(this, "bag");
            _items = new RemoteInventory(this, "items");
            UpdateCache(createdCrawler);
        }

        public int X => _cache.X;

        public int Y => _cache.Y;

        public Direction Direction => _direction;

        public Task<Type> FacingTileType => GetFacingTileTypeAsync();

        public async Task<Type> GetFacingTileTypeAsync(CancellationToken cancellationToken = default)
        {
            if (_cacheDirection != _direction && !await UpdateRemote(cancellationToken))
            {
                while(_direction != _cacheDirection)
                {
                    _direction.TurnLeft();
                }
            }
            return _cache.FacingTile.GetCrawlerFacingTileType();
        }

        public async Task<Inventory?> TryWalk(Inventory myInventory, CancellationToken cancellationToken = default)
        {
            if(myInventory != _bag)
            {
                throw new ArgumentException("Can only walk with own bag inventory (returned by labyrinth).", nameof(myInventory));
            }
            _cache.Walking = true;
            return await UpdateRemote(cancellationToken)
                ? _items
                : null;
        }

        public async Task<bool> Delete() =>
            (await _httpClient.DeleteAsync(
                $"/crawlers/{_cache.Id}?appKey={_appKey}"
            )).IsSuccessStatusCode;
        
        public event EventHandler? Changed;

        public Inventory Bag => _bag;

        private async Task<bool> UpdateRemote(CancellationToken cancellationToken = default)
        {
            _cache.Dir = _direction.GetApiDirection();
            var response = await _httpClient.PatchAsJsonAsync($"/crawlers/{_cache.Id}?appKey={_appKey}", _cache, cancellationToken);

            if (response.IsSuccessStatusCode &&
                await response.Content.ReadFromJsonAsync<Dto.Crawler>(cancellationToken: cancellationToken) is Dto.Crawler lastState)
            {
                UpdateCache(lastState);
                Changed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                _cache.Walking = false;
                return false;
            }
        }

        [MemberNotNull(nameof(_cacheDirection), nameof(_direction), nameof(_cache))]
        private void UpdateCache(Dto.Crawler updatedCrawler)
        {
            _cache = updatedCrawler;
            _cacheDirection = _cache.Dir.GetCrawlerDirection();
            _direction = (Direction)_cacheDirection.Clone();
            if (_cache.Bag   is not null) _bag  .UpdateList(_cache.Bag.Length);
            if (_cache.Items is not null) _items.UpdateList(_cache.Items.Length);
        }

        private async Task<bool> UpdateRemoteInventories(RemoteInventory from, RemoteInventory to, IList<bool> movesRequired)
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/crawlers/{_cache.Id}/{from.TypeName}?appKey={_appKey}",
                movesRequired.Select(moveReq => new Dto.InventoryItem { 
                    Type = Dto.ItemType.Key, 
                    MoveRequired = moveReq 
                })
            );
            if (response.IsSuccessStatusCode)
            {
                var newCount = (await response.Content.ReadFromJsonAsync<Dto.InventoryItem[]>())?.Length ?? 0;

                from.UpdateList(newCount);
                to.UpdateList(to.ItemTypes.Count() + movesRequired.Count - newCount);
                return true;
            }
            return false;
        }

        private Dto.Crawler _cache;
        private Direction _cacheDirection, _direction;
        private readonly RemoteInventory _bag, _items;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Guid _appKey;

        private class RemoteInventory(ClientCrawler parent, string type) : Inventory
        {
            public string TypeName { get; init; } = type;
  
            public override async Task<bool> TryMoveItemsFrom(Inventory from, IList<bool> movesRequired)
            {
                if(from != _parent._bag && from != _parent._items)
                {
                    throw new ArgumentException("Can only move items between the inventories of the crawler", nameof(from));
                }
                return await _parent.UpdateRemoteInventories((RemoteInventory)from, this, movesRequired);
            }

            public void UpdateList(int itemCount)
            {
                while(_items.Count > itemCount)
                    _items.RemoveAt(_items.Count - 1);
                while(_items.Count < itemCount)
                    _items.Add(new Key()); 
            }

            private ClientCrawler _parent = parent;
        }
    }
}
