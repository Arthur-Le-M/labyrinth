using Labyrinth;
using Labyrinth.ApiClient;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Labyrinth.Sys;
using Labyrinth.Exploration;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Exploration.Strategies.Implementations;
using Labyrinth.Map;
using Dto=ApiTypes;
using System.Text.Json;

char DirToChar(Direction dir) =>
    "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];

var TileToChar = new Dictionary<Type, char>
{
    [typeof(Room   )] = ' ',
    [typeof(Wall   )] = '#',
    [typeof(Door   )] = '/'
};

// Explorer display metadata
var explorerColors = new[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Magenta };
var explorerMeta = new Dictionary<StrategyExplorer, (ConsoleColor Color, string StrategyName)>();
var explorerPrevPos = new Dictionary<StrategyExplorer, (int x, int y)?>();
// console lock for synchronized drawing
var consoleLock = new object();

// Shared map reference (will be set later)
SharedMap? sharedMap = null;

void DrawExplorerForRand(object? sender, CrawlingEventArgs e)
{
    var crawler = ((RandExplorer)sender!).Crawler;
    var facingTileType = crawler.FacingTileType.Result;

    lock (consoleLock)
    {
        UpdateVisibleMap(e.X, e.Y, e.Direction, facingTileType);
        DrawSharedMapCells(e.X, e.Y, e.Direction, facingTileType);
        DrawFullMapCrawler(e.X, e.Y, e.Direction);

        SafeSetCursorPosition(0, 0);
        if (crawler is ClientCrawler cc)
        {
            Console.WriteLine($"Bag : { cc.Bag.ItemTypes.Count() } item(s)");
        }
    }
    Thread.Sleep(100);
}

void DrawExplorerForStrategy(object? sender, CrawlingEventArgs e)
{
    var explorer = sender as StrategyExplorer;
    if (explorer is null) return;
    var crawlerObj = explorer.Crawler;
    var facingTileType = crawlerObj.FacingTileType.Result;

    // ALL console writes and shared-state access under one lock
    lock (consoleLock)
    {
        // restore previous cell for this explorer if it moved
        if (explorerPrevPos.TryGetValue(explorer, out var prevPos) && prevPos.HasValue)
        {
            var (px, py) = prevPos.Value;
            if (px != e.X || py != e.Y)
            {
                var occupied = explorerPrevPos.Keys.Any(other => !ReferenceEquals(other, explorer)
                    && other.Crawler.X == px && other.Crawler.Y == py);
                if (!occupied)
                {
                    RestoreCellAt(px, py);
                }
            }
        }

        UpdateVisibleMapFromSharedMap(e.X, e.Y, e.Direction, facingTileType);

        // pick color for this explorer
        var color = Console.ForegroundColor;
        if (explorerMeta.TryGetValue(explorer, out var meta))
        {
            color = meta.Color;
        }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            DrawSharedMapCells(e.X, e.Y, e.Direction, facingTileType);
            DrawFullMapCrawler(e.X, e.Y, e.Direction);
            RenderLegend();
        }
        finally
        {
            Console.ForegroundColor = prev;
        }

        explorerPrevPos[explorer] = (e.X, e.Y);
    }

    Thread.Sleep(100);
}

Labyrinth.Labyrinth labyrinth;
Inventory? bag = null;
ContestSession? contest = null;

if (args.Length < 2)
{
    Console.WriteLine(
        "Command line usage: https://apiserver.example appKeyGuid [settings.json]"
    );
    Console.WriteLine("Running in multi-mode with 3 Random strategies");
    labyrinth = new Labyrinth.Labyrinth(new AsciiParser("""
        +--+--------+
        |  /        |
        |  +--+--+  |
        |     |k    |
        +--+  |  +--+
           |k  x    |
        +  +-------/|
        |           |
        +-----------+
        """));
}
else
{
    Dto.Settings? settings = null;

    if (args.Length > 2)
    {
        settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args[2]));
    }
    contest = await ContestSession.Open(new Uri(args[0]), Guid.Parse(args[1]), settings);
    labyrinth = new (contest.Builder);
    bag = contest.Bags.First();
}

// Create shared map for strategy-based exploration
sharedMap = new SharedMap();

// Create exploration strategy based on CLI argument
IExplorationStrategy CreateStrategy(string name) => name switch
{
    "dfs" => new DfsStrategy(),
    "astar" or "a*" => new AStarStrategy(),
    "random" or _ => new RandomStrategy(new BasicEnumRandomizer<RandExplorer.Actions>())
};

Console.WriteLine("Exploration mode: Multi (3 Random explorers)");


var fullMap = BuildFullMap(labyrinth);
var mapWidth = fullMap.GetLength(0);
var mapHeight = fullMap.GetLength(1);
var visibleMap = CreateUnknownMap(mapWidth, mapHeight);

var legendHeight = 4; // Pour 3 explorateurs
var fullMapTitleY = legendHeight;
var fullMapOffsetY = fullMapTitleY + 1;
var sharedMapTitleY = fullMapOffsetY + mapHeight + 1;
var sharedMapOffsetY = sharedMapTitleY + 1;
var requiredBufferHeight = sharedMapOffsetY + mapHeight + 1;
var requiredBufferWidth = mapWidth + 1;
var canRenderSharedMap = EnsureConsoleBuffer(requiredBufferWidth, requiredBufferHeight);

Console.Clear();
RenderLegend();
SafeSetCursorPosition(0, fullMapTitleY);
Console.WriteLine("Full map");
SafeSetCursorPosition(0, fullMapOffsetY);
DrawMap(fullMap);
if (canRenderSharedMap)
{
    SafeSetCursorPosition(0, sharedMapTitleY);
    Console.WriteLine("Shared map");
    SafeSetCursorPosition(0, sharedMapOffsetY);
    DrawMap(visibleMap);
}

// Run in multi-mode by default: spawn 3 explorers with Random strategy sharing the same map
var explorers = new List<IExplorer>();
var multiStrategies = new[] { "astar", "dfs", "random" };

for (int i = 0; i < 3; i++)
{
    ICrawler c;
    Inventory? crawlerBag = null;
    if (contest is null)
    {
        c = labyrinth.NewCrawler();
    }
    else
    {
        c = await contest.NewCrawler();
        // ClientCrawler requires its own bag for TryWalk
        if (c is ClientCrawler cc)
        {
            crawlerBag = cc.Bag;
        }
    }

    var stratName = multiStrategies[i];
    var se = new StrategyExplorer(c, CreateStrategy(stratName), sharedMap, 3000, crawlerBag);
    se.DirectionChanged += DrawExplorerForStrategy;
    se.PositionChanged += (s, e) =>
    {
        try
        {
            DrawExplorerForStrategy(s, e);
        }
        catch (ArgumentOutOfRangeException) { /* Ignore console bounds errors */ }
    };

    explorerMeta[se] = (explorerColors[i % explorerColors.Length], stratName);
    explorers.Add(se);
}

var multi = new MultiExplorer(explorers, sharedMap);
await multi.StartAsync();

if (contest is not null)
{
    await contest.Close();
}

return;

char[,] BuildFullMap(Labyrinth.Labyrinth lab)
{
    var lines = lab.ToString()
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    var height = lines.Length;
    var width = lines.Length == 0 ? 0 : lines.Max(l => l.Length);
    var map = new char[width, height];

    for (int y = 0; y < height; y++)
    {
        var line = lines[y];
        for (int x = 0; x < width; x++)
        {
            map[x, y] = x < line.Length ? line[x] : '?';
        }
    }

    return map;
}

char[,] CreateUnknownMap(int width, int height)
{
    var map = new char[width, height];
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            map[x, y] = '?';
        }
    }
    return map;
}

void DrawMap(char[,] map)
{
    for (int y = 0; y < map.GetLength(1); y++)
    {
        for (int x = 0; x < map.GetLength(0); x++)
        {
            Console.Write(map[x, y]);
        }
        Console.WriteLine();
    }
}

void UpdateVisibleMap(int x, int y, Direction dir, Type facingTileType)
{
    if (IsInBounds(x, y))
    {
        visibleMap[x, y] = ' ';
    }

    var facingX = x + dir.DeltaX;
    var facingY = y + dir.DeltaY;
    if (facingTileType != typeof(Outside) && IsInBounds(facingX, facingY))
    {
        visibleMap[facingX, facingY] = GetCharForTileType(facingTileType);
    }
}

// Met à jour visibleMap en utilisant la SharedMap pour voir les portes ouvertes
void UpdateVisibleMapFromSharedMap(int x, int y, Direction dir, Type facingTileType)
{
    if (sharedMap == null) return;

    // Update current position
    if (IsInBounds(x, y))
    {
        var currentTile = sharedMap.GetTile((x, y));
        visibleMap[x, y] = currentTile != null ? GetCharForTile(currentTile) : ' ';
    }

    // Update facing position using SharedMap
    var facingX = x + dir.DeltaX;
    var facingY = y + dir.DeltaY;
    if (facingTileType != typeof(Outside) && IsInBounds(facingX, facingY))
    {
        var facingTile = sharedMap.GetTile((facingX, facingY));
        if (facingTile != null)
        {
            visibleMap[facingX, facingY] = GetCharForTile(facingTile);
        }
        else
        {
            visibleMap[facingX, facingY] = GetCharForTileType(facingTileType);
        }
    }
}

void DrawSharedMapCells(int x, int y, Direction dir, Type facingTileType)
{
    if (!canRenderSharedMap)
    {
        return;
    }
    var facingX = x + dir.DeltaX;
    var facingY = y + dir.DeltaY;

    if (facingTileType != typeof(Outside) && IsInBounds(facingX, facingY))
    {
        SafeSetCursorPosition(facingX, facingY + sharedMapOffsetY);
        Console.Write(visibleMap[facingX, facingY]);
    }

    if (IsInBounds(x, y))
    {
        SafeSetCursorPosition(x, y + sharedMapOffsetY);
        Console.Write(DirToChar(dir));
    }
}

void DrawFullMapCrawler(int x, int y, Direction dir)
{
    if (IsInBounds(x, y))
    {
        SafeSetCursorPosition(x, y + fullMapOffsetY);
        Console.Write(DirToChar(dir));
    }
}


// Restore the tile at arbitrary coordinates (used by multi-explorer display)
void RestoreCellAt(int x, int y)
{
    if (!IsInBounds(x, y)) return;
    SafeSetCursorPosition(x, y + fullMapOffsetY);
    Console.Write(fullMap[x, y]);
    if (canRenderSharedMap)
    {
        SafeSetCursorPosition(x, y + sharedMapOffsetY);
        Console.Write(visibleMap[x, y]);
    }
}

void RenderLegend()
{
    // legend sits at line 0 above the full map
    lock (consoleLock)
    {
        var legendY = 0;
        // clear legend area
        for (int i = 0; i < legendHeight; i++)
        {
            SafeSetCursorPosition(0, legendY + i);
            Console.Write(new string(' ', Math.Max(40, requiredBufferWidth)));
        }

        int idx = 0;
        foreach (var kv in explorerMeta)
        {
            var expl = kv.Key;
            var meta = kv.Value;
            var steps = expl is IStepsTrackingExplorer st ? st.StepsExecuted : 0;
            SafeSetCursorPosition(0, legendY + idx);
            var prev = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = meta.Color;
                Console.Write($"[{idx+1}] ");
            }
            finally { Console.ForegroundColor = prev; }
            var text = $"Strategy={meta.StrategyName} Steps={steps}";
            var maxWrite = Math.Min(Math.Max(40, requiredBufferWidth), Math.Max(1, Console.BufferWidth - 1));
            Console.Write(text.PadRight(maxWrite).Substring(0, maxWrite));
            idx++;
        }
    }
}

void SafeSetCursorPosition(int x, int y)
{
    if (x < 0 || y < 0)
    {
        return;
    }

    if (x >= Console.BufferWidth || y >= Console.BufferHeight)
    {
        return;
    }

    Console.SetCursorPosition(x, y);
}

bool IsInBounds(int x, int y) =>
    x >= 0 && y >= 0 && x < visibleMap.GetLength(0) && y < visibleMap.GetLength(1);

char GetCharForTileType(Type tileType) =>
    TileToChar.TryGetValue(tileType, out var ch) ? ch : '?';

// Convertit une Tile en caractère (affiche les portes ouvertes comme des espaces)
char GetCharForTile(Tile tile)
{
    return tile switch
    {
        Room => ' ',
        Wall => '#',
        Door door => door.IsOpened ? ' ' : '/', // Porte ouverte = espace, fermée = /
        Outside => '?',
        _ => '?'
    };
}

bool EnsureConsoleBuffer(int width, int height)
{
    try
    {
        if (Console.BufferWidth < width)
        {
            Console.BufferWidth = width;
        }
        if (Console.BufferHeight < height)
        {
            Console.BufferHeight = height;
        }
        return Console.BufferHeight >= height && Console.BufferWidth >= width;
    }
    catch (ArgumentOutOfRangeException)
    {
        return false;
    }
    catch (IOException)
    {
        return false;
    }
}